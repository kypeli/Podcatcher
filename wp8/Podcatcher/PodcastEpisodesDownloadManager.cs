﻿/**
 * Copyright (c) 2012, 2013, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.BackgroundTransfer;
using Podcatcher.CustomControls;
using Podcatcher.ViewModels;
using Microsoft.Phone.Info;

namespace Podcatcher
{
    public delegate void PodcastDownloadManagerHandler(object source, PodcastDownloadManagerArgs args);
    
    public class PodcastDownloadManagerArgs : EventArgs
    {
        public int episodeId;
        public PodcastEpisodeModel.EpisodeDownloadStateEnum downloadState;
    }

    public class PodcastEpisodesDownloadManager
    {
        public event PodcastDownloadManagerHandler OnPodcastEpisodeDownloadStateChanged;
    
        public ObservableQueue<PodcastEpisodeModel> EpisodeDownloadQueue
        {
            get
            {
                return m_episodeDownloadQueue;
            }
        }

        public static PodcastEpisodesDownloadManager getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastEpisodesDownloadManager();
            }

            return m_instance;
        }

        public void addEpisodeToDownloadQueue(PodcastEpisodeModel episode)
        {
            episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued;
            episode.DownloadPercentage = 0;
            m_episodeDownloadQueue.Enqueue(episode);
            saveEpisodeInfoToDB(episode);
            PodcastSubscriptionsManager.getInstance().newDownloadedEpisode(episode);

            if (m_currentEpisodeDownload == null)
            {
                startNextEpisodeDownload();
            }
        }

        public void cancelEpisodeDownload(PodcastEpisodeModel episode)
        {
            // Update new episode state.
            episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;
            saveEpisodeInfoToDB(episode);

            // Get the transfer request that we should cancel. 
            BackgroundTransferRequest thisRequest = episode.DownloadRequest;

            // We canceled a queued episode that wasn't downloading yet.
            if (thisRequest == null)
            {
                removeEpisodeFromDownloadQueue(episode);
                return;
            }
            else
            {
                // We canceled current download.
                RemoveTransferRequest(thisRequest);
            }

            episode.DownloadRequest = null;
            PodcastSubscriptionsManager.getInstance().removedPlayableEpisode(episode);

        }

        public void addEpisodesToDownloadQueue(List<PodcastEpisodeModel> newPodcastEpisodes)
        {
            foreach (PodcastEpisodeModel episode in newPodcastEpisodes)
            {
                if (episode.isPlayable())
                {
                    addEpisodeToDownloadQueue(episode);
                }
            }
        }

        public static void notifyUserOfDownloadRestrictions(PodcastEpisodeModel episode)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            // Notify about >100 MB downloads
            if (!settings.Contains(App.LSKEY_NOTIFY_DOWNLOADING_WITH_WIFI)
                && episode.EpisodeDownloadSize > App.MAX_SIZE_FOR_WIFI_DOWNLOAD_NO_POWER)
            {

                if (MessageBox.Show("You are about to download a file over 100 MB in size. " +
                                    "Please note that Windows Phone allows downloading this kind of files only if " +
                                    "you are connected to a WiFi network and connected to an external power source.",
                    "Attention",
                    MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    settings.Add(App.LSKEY_NOTIFY_DOWNLOADING_WITH_WIFI, true);
                    return;
                }
            }

            // Notify about >20 MB downloads
            if (!settings.Contains(App.LSKEY_NOTIFY_DOWNLOADING_WITH_CELLULAR)
                && episode.EpisodeDownloadSize > App.MAX_SIZE_FOR_CELLULAR_DOWNLOAD)
            {

                if (MessageBox.Show("You are about to download a file over 20 MB in size. Please " +
                                    "note that Windows Phone allows downloading this kind of files only if you are " +
                                    "connected to a WiFi network.",
                    "Attention",
                    MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    settings.Add(App.LSKEY_NOTIFY_DOWNLOADING_WITH_CELLULAR, true);
                    return;
                }
            }

        }

        #region private
        private static PodcastEpisodesDownloadManager m_instance            = null;
        private ObservableQueue<PodcastEpisodeModel> m_episodeDownloadQueue = new ObservableQueue<PodcastEpisodeModel>();
        private PodcastEpisodeModel m_currentEpisodeDownload                = null;
        private BackgroundTransferRequest m_currentBackgroundTransfer       = null;
        private IsolatedStorageSettings m_applicationSettings               = null;

        // Booleans for tracking if any transfers are waiting for user action. 
        bool WaitingForExternalPower                        = false;
        bool WaitingForExternalPowerDueToBatterySaverMode   = false;
        bool WaitingForNonVoiceBlockingNetwork              = false;
        bool WaitingForWiFi                                 = false;

        private PodcastEpisodesDownloadManager()
        {
            createEpisodeDownloadDir();

            m_applicationSettings = IsolatedStorageSettings.ApplicationSettings;

            if (BackgroundTransferService.Requests.Count() == 0)
            {
                m_applicationSettings.Remove(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID);
                m_currentBackgroundTransfer = null;
            }

            processOngoingTransfer();
            processStoredQueuedTransfers();
        }

        private void createEpisodeDownloadDir()
        {
            IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
            if (iso.DirectoryExists(App.PODCAST_DL_DIR) == false)
            {
                Debug.WriteLine("Download dir doesn't exist. Creating dir: " + App.PODCAST_DL_DIR);
                iso.CreateDirectory(App.PODCAST_DL_DIR);
            }
        }

        private void processOngoingTransfer()
        {
            // If key exists, we know we have need to process a download request.
            if (m_applicationSettings.Contains(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID))
            {
                Debug.WriteLine("Found a episode download that we need to process.");
                int downloadingEpisodeId = (int)m_applicationSettings[App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID];
                m_currentEpisodeDownload = null;
                using (var db = new PodcastSqlModel())
                {
                    m_currentEpisodeDownload = db.episodeForEpisodeId(downloadingEpisodeId);
                }

                if (m_currentEpisodeDownload == null)
                {
                    Debug.WriteLine("Something went wrong. Got NULL episode when asking for episode id " + downloadingEpisodeId);

                    m_applicationSettings.Remove(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID);
                    m_applicationSettings.Save();
                    return;
                }

                if (BackgroundTransferService.Requests.Count() > 0)
                {
                    // Found an ongoing request.
                    Debug.WriteLine("Found an ongoing transfer...");
                    m_currentBackgroundTransfer = BackgroundTransferService.Requests.ElementAt(0);
                    m_currentBackgroundTransfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);

                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading;
                    m_currentEpisodeDownload.DownloadRequest = m_currentBackgroundTransfer;
                    
                    m_episodeDownloadQueue.Enqueue(m_currentEpisodeDownload);
                    
                    ProcessTransfer(m_currentBackgroundTransfer);

                    saveEpisodeInfoToDB(m_currentEpisodeDownload);
                }
                else
                {
                    // No ongoing requests found. Then we need to process a finished request.
                    // Probably happened in the background while we were suspended.
                    Debug.WriteLine("Found a completed request.");
                    updateEpisodeWhenDownloaded(m_currentEpisodeDownload);
                    m_applicationSettings.Remove(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID);
                    m_applicationSettings.Save();
                }
            }
        }

        private void saveEpisodeInfoToDB(PodcastEpisodeModel m_currentEpisodeDownload)
        {
            if (m_currentEpisodeDownload == null)             
            {
                return;
            }

            m_currentEpisodeDownload.StoreProperty<PodcastEpisodeModel.EpisodeDownloadStateEnum>("EpisodeDownloadState", m_currentEpisodeDownload.EpisodeDownloadState);
            m_currentEpisodeDownload.StoreProperty<String>("EpisodeFile", m_currentEpisodeDownload.EpisodeFile);
        }

        private void processStoredQueuedTransfers()
        {
            List<PodcastEpisodeModel> queuedEpisodes = new List<PodcastEpisodeModel>();
            using (var db = new PodcastSqlModel())
            {
                queuedEpisodes = db.Episodes.Where(ep => (ep.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued
                                                          || ep.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWiFi
                                                          || ep.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWifiAndPower)
                                                  ).ToList();
                
                foreach (PodcastEpisodeModel episode in queuedEpisodes)
                {
                    episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued;
                    m_episodeDownloadQueue.Enqueue(episode);
                }

                db.SubmitChanges();
            }


            if (m_currentBackgroundTransfer == null && m_episodeDownloadQueue.Count > 0)
            {
                startNextEpisodeDownload();
            }
        }

        private void removeEpisodeFromDownloadQueue(PodcastEpisodeModel episode)
        {
            m_episodeDownloadQueue.RemoveItem(episode);
            episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;
            saveEpisodeInfoToDB(episode);
        }

        private void startNextEpisodeDownload(TransferPreferences useTransferPreferences = TransferPreferences.AllowCellularAndBattery)
        {
            if (BackgroundTransferService.Requests.Count() > 0)
            {
                // For some reason there are still old requests in the background transfer service. 
                // Let's clean everything and start over.
                foreach (BackgroundTransferRequest t in BackgroundTransferService.Requests.AsEnumerable())
                {
                    BackgroundTransferService.Remove(t);
                }
            }
            
            if (m_episodeDownloadQueue.Count > 0)
            {
                m_currentEpisodeDownload = m_episodeDownloadQueue.Peek();
                Uri downloadUri;
                try
                {
                    downloadUri = new Uri(m_currentEpisodeDownload.EpisodeDownloadUri, UriKind.Absolute);
                }
                catch (Exception e)
                {
                    App.showErrorToast("Cannot download the episode.");
                    Debug.WriteLine("Cannot download the episode. URI exception: " + e.Message);
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;
                    m_episodeDownloadQueue.Dequeue();
                    saveEpisodeInfoToDB(m_currentEpisodeDownload);
                    startNextEpisodeDownload();
                    return;
                }

                m_currentEpisodeDownload.EpisodeFile = generateLocalEpisodeFileName(m_currentEpisodeDownload);
                if (string.IsNullOrEmpty(m_currentEpisodeDownload.EpisodeFile))
                {
                    App.showErrorToast("Cannot download the episode.");
                    Debug.WriteLine("Cannot download the episode. Episode file name is null or empty.");
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;
                    m_episodeDownloadQueue.Dequeue();
                    saveEpisodeInfoToDB(m_currentEpisodeDownload);
                    startNextEpisodeDownload();
                    return;
                }

                // Create a new background transfer request for the podcast episode download.
                m_currentBackgroundTransfer = new BackgroundTransferRequest(downloadUri,
                                                                            new Uri(m_currentEpisodeDownload.EpisodeFile, UriKind.Relative));
                if (useTransferPreferences == TransferPreferences.None)
                {
                    m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.None;
                } 
                else if (canAllowCellularDownload(m_currentEpisodeDownload))
                {
                    bool settingsAllowCellular = false;
                    using (var db = new PodcastSqlModel())
                    {
                        settingsAllowCellular =  db.settings().IsUseCellularData;
                    }

                    Debug.WriteLine("Settings: Allow cellular download: " + settingsAllowCellular);
                    
                    if (settingsAllowCellular && canDownloadOverCellular())
                    {
                        m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
                    }
                    else
                    {
                        m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.AllowBattery;
                    }
                } else {
                    m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.None;
                }
                                                     
                Debug.WriteLine("m_currentBackgroundTransfer.TransferPreferences = " + m_currentBackgroundTransfer.TransferPreferences.ToString());

                m_currentBackgroundTransfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);

                // Store request to the episode.
                m_currentEpisodeDownload.DownloadRequest = m_currentBackgroundTransfer;

                m_applicationSettings.Remove(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID);
                m_applicationSettings.Add(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID, m_currentEpisodeDownload.EpisodeId);
                m_applicationSettings.Save();

                try
                {
                    BackgroundTransferService.Add(m_currentBackgroundTransfer);
                }
                catch (InvalidOperationException)
                {
                    foreach (BackgroundTransferRequest r in BackgroundTransferService.Requests)
                    {
                        BackgroundTransferService.Remove(r);
                    }

                    BackgroundTransferService.Add(m_currentBackgroundTransfer);
                }
            }
        }

        private bool canDownloadOverCellular()
        {
            if (m_currentEpisodeDownload.EpisodeDownloadSize == 0)
            {
                return true;
            }

            long downloadSizeLimit = App.MAX_SIZE_FOR_CELLULAR_DOWNLOAD;                                     
            long episodeDownloadSize = m_currentEpisodeDownload.EpisodeDownloadSize;
            if (episodeDownloadSize < downloadSizeLimit)
            {
                return true;
            }

            return false;
        }

        private bool canAllowCellularDownload(PodcastEpisodeModel m_currentEpisodeDownload)
        {
            if (m_currentEpisodeDownload.EpisodeDownloadSize == 0)
            {
                return true;
            }

            long downloadSizeLimit = App.MAX_SIZE_FOR_WIFI_DOWNLOAD_NO_POWER;
            long episodeDownloadSize = m_currentEpisodeDownload.EpisodeDownloadSize;

            if (episodeDownloadSize < downloadSizeLimit)
            {
                return true;
            }

            return false;
        }

        void backgroundTransferStatusChanged(object sender, BackgroundTransferEventArgs e)
        {
            ResetStatusFlags();
            ProcessTransfer(e.Request);
            UpdateUI(e.Request);
        }

        private void ResetStatusFlags()
        {
            WaitingForExternalPower = false;
            WaitingForExternalPowerDueToBatterySaverMode = false;
            WaitingForNonVoiceBlockingNetwork = false;
            WaitingForWiFi = false;
        }

        private void ProcessTransfer(BackgroundTransferRequest backgroundTransferRequest)
        {
            if (m_currentEpisodeDownload == null)
            {
                return;
            }

            switch (backgroundTransferRequest.TransferStatus)
            {
                case TransferStatus.WaitingForWiFi:
                    Debug.WriteLine("Transfer status: WaitingForWiFi");
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWiFi;
                    WaitingForWiFi = true;
                    break;
                case TransferStatus.WaitingForExternalPower:
                    Debug.WriteLine("Transfer status: WaitingForExternalPower");
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWifiAndPower;
                    WaitingForExternalPower = true;
                    break;
                case TransferStatus.WaitingForExternalPowerDueToBatterySaverMode:
                    Debug.WriteLine("Transfer status: WaitingForExternalPowerDueToBatterySaverMode");
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWifiAndPower;
                    WaitingForExternalPowerDueToBatterySaverMode = true;
                    break;
                case TransferStatus.WaitingForNonVoiceBlockingNetwork:
                    Debug.WriteLine("Transfer status: WaitingForNonVoiceBlockingNetwork");
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWiFi;
                    WaitingForNonVoiceBlockingNetwork = true;
                    break;
                case TransferStatus.Completed:
                    Debug.WriteLine("Transfer completed.");
                    completePodcastDownload(backgroundTransferRequest);
                    break;
                case TransferStatus.Transferring:
                    Debug.WriteLine("Transferring...");
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading;
                    break;
            }

            if (m_currentEpisodeDownload != null)
            {
                sendDownloadStateChangedEvent(m_currentEpisodeDownload, m_currentEpisodeDownload.EpisodeDownloadState);
                saveEpisodeInfoToDB(m_currentEpisodeDownload);
            }
        }

        private void completePodcastDownload(BackgroundTransferRequest transferRequest)
        {
            // If the status code of a completed transfer is 200 or 206, the
            // transfer was successful
            if (transferRequest.TransferError == null && 
               (transferRequest.StatusCode == 200 || transferRequest.StatusCode == 206))
            {
                Debug.WriteLine("Transfer request completed succesfully.");
                updateEpisodeWhenDownloaded(m_currentEpisodeDownload);

                // Add downloaded episode to play queue, if set.
                using (var db = new PodcastSqlModel())
                {
                    if (db.settings().IsAddDownloadsToPlayQueue)
                    {
                        PodcastPlaybackManager.getInstance().addSilentlyToPlayqueue(m_currentEpisodeDownload);
                    }
                }
            }
            else
            {
                Debug.WriteLine("Transfer request completed with error code: " + transferRequest.StatusCode + ", " + transferRequest.TransferError);
                switch (transferRequest.StatusCode)
                {
                    case 0:
                        Debug.WriteLine("Request canceled.");
                        break;

                    // If error code is 200 but we still got an error, this means the max. transfer size exceeded.
                    // This is because the podcast feed announced a different download size than what the file actually is.
                    // If user wants, we can try again with larger file download size policy.
                    case 200:
                        Debug.WriteLine("Maxiumum download size exceeded. Shall we try again?");

                        if (MessageBox.Show("Podcast feed announced wrong file size. Do you want to download again with larger file download settings?",
                                            "Podcast download failed",
                                            MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            if (MessageBox.Show("Please connect your phone to an external power source and to a WiFi network.",
                                                "Attention",
                                MessageBoxButton.OK) == MessageBoxResult.OK)
                            {
                                Debug.WriteLine("Download the same episode again, with preferences None.");

                                // We download the same file again, but this time we force the TransferPrefernces to be None.
                                startNextEpisodeDownload(TransferPreferences.None);
                                return;
                            }
                        }
                        break;

                    case 301:
                        App.showErrorToast("WP8 cannot download from this location.");
                        break;

                    default:
                        App.showErrorToast("Could not download the episode\nfrom the server.");
                        break;
                }


                if (m_currentEpisodeDownload != null)
                {
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;
                    m_currentEpisodeDownload.deleteDownloadedEpisode();
                }
            }

            saveEpisodeInfoToDB(m_currentEpisodeDownload);
            cleanupEpisodeDownload(transferRequest);
            // And start a next round of downloading.
            startNextEpisodeDownload();
        }

        private void sendDownloadStateChangedEvent(PodcastEpisodeModel episode, PodcastEpisodeModel.EpisodeDownloadStateEnum state)
        {
            if (OnPodcastEpisodeDownloadStateChanged != null)
            {
                this.OnPodcastEpisodeDownloadStateChanged(this, new PodcastDownloadManagerArgs() { downloadState = state, episodeId = episode.EpisodeId });
            }
        }

        private void updateEpisodeWhenDownloaded(PodcastEpisodeModel episode)
        {
            Debug.WriteLine("Updating episode information for episode when download completed: " + episode.EpisodeName);
            episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded;

            using (var db = new PodcastSqlModel())
            {
                Debug.WriteLine(" * Downloaded file name: " + episode.EpisodeFile);

                PodcastEpisodeModel e = db.episodeForEpisodeId(episode.EpisodeId);
                e.EpisodeFile = episode.EpisodeFile;
                e.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded;
                e.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded;

                db.SubmitChanges();

                PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(e.PodcastSubscription);
            }
        }

        private void cleanupEpisodeDownload(BackgroundTransferRequest transferRequest)
        {
            m_currentBackgroundTransfer.TransferStatusChanged -= new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);

            // Remove the transfer request in order to make room in the 
            // queue for more transfers. Transfers are not automatically
            // removed by the system.
            RemoveTransferRequest(transferRequest);

            // Update state of the finished episode.
            //  - Remove the settings key that denoted that this episode is download (in case the app is restarted while this downloads). 
            //  - Set currently downloading episode to NULL.
            //  - Remove this episode from the download queue. 
            m_applicationSettings.Remove(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID);
            m_episodeDownloadQueue.Dequeue();

            m_currentBackgroundTransfer = null;
            transferRequest = null;

            // Clean episode data.
            m_currentEpisodeDownload.DownloadRequest = null;
            m_currentEpisodeDownload = null;

        }

        private void RemoveTransferRequest(BackgroundTransferRequest transfer)
        {
            Debug.WriteLine("Removing transfer request with id: " + transfer.RequestId);
            // Try to remove the transfer from the background transfer service.
            try
            {
                BackgroundTransferService.Remove(transfer);
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR: Cannot remove transfer request. Error: " + e.Message);
            }
        }

        private void UpdateUI(BackgroundTransferRequest backgroundTransferRequest)
        {
            if (WaitingForExternalPower)
            {
                MessageBox.Show("Podcast transfer is waiting for external power. Please connect your device to external power to continue transferring.");
            }
            if (WaitingForExternalPowerDueToBatterySaverMode)
            {
                MessageBox.Show("Podcast transfer is waiting for external power. Connect your device to external power or disable Battery Saver Mode to continue transferring.");
            }
            if (WaitingForNonVoiceBlockingNetwork)
            {
                MessageBox.Show("Podcast transfer is waiting for a mobile network that supports simultaneous voice and data.");
            }
            if (WaitingForWiFi)
            {
                MessageBox.Show("Podcast transfer is waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring.");
            }
        }

        private string generateLocalEpisodeFileName(PodcastEpisodeModel podcastEpisode)
        {
            // Parse the filename of the logo from the remote URL.
            string localPath = new Uri(podcastEpisode.EpisodeDownloadUri).LocalPath;
            string podcastEpisodeFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);

            podcastEpisodeFilename = PodcastSubscriptionsManager.sanitizeFilename(podcastEpisodeFilename);
            podcastEpisodeFilename = String.Format("{0}_{1}", DateTime.Now.Millisecond, podcastEpisodeFilename);
            
            string localPodcastEpisodeFilename = App.PODCAST_DL_DIR + "/" + podcastEpisodeFilename;
            Debug.WriteLine("Found episode filename: " + localPodcastEpisodeFilename);

            return localPodcastEpisodeFilename;
        }
        #endregion
    }
}
