/**
 * Copyright (c) 2012, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the <organization> nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using Coding4Fun.Phone.Controls;
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
    }

    public class PodcastEpisodesDownloadManager
    {
        public event PodcastDownloadManagerHandler OnEpisodeDownloadInitiated;

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
            pushToQueueCache(episode);

            if (m_currentEpisodeDownload == null)
            {
                startNextEpisodeDownload();
            }
        }

        public void cancelEpisodeDownload(PodcastEpisodeModel episode)
        {
            // Update new episode state.
            episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;

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
        }

        public void addEpisodesToDownloadQueue(List<PodcastEpisodeModel> newPodcastEpisodes)
        {
            foreach (PodcastEpisodeModel episode in newPodcastEpisodes)
            {
                addEpisodeToDownloadQueue(episode);
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

                if (MessageBox.Show("You are about to download a file over 50 MB in size. Please " +
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
                m_currentEpisodeDownload = PodcastSqlModel.getInstance().episodeForEpisodeId(downloadingEpisodeId);

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
                    m_currentBackgroundTransfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);
                    
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading;
                    m_currentEpisodeDownload.DownloadRequest = m_currentBackgroundTransfer;
                    
                    m_episodeDownloadQueue.Enqueue(m_currentEpisodeDownload);
                    
                    ProcessTransfer(m_currentBackgroundTransfer);
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

        private void processStoredQueuedTransfers()
        {
            if (m_applicationSettings.Contains(App.LSKEY_PODCAST_DOWNLOAD_QUEUE) == false)
            {
                m_applicationSettings.Add(App.LSKEY_PODCAST_DOWNLOAD_QUEUE, "");        // Create empty setting if we don't have the key yet.
            }

            // This will return a comma separated list of episode IDs that are queued for downloading.
            string queuedSettingsString = (string)m_applicationSettings[App.LSKEY_PODCAST_DOWNLOAD_QUEUE];
            if (String.IsNullOrEmpty(queuedSettingsString))
            {
                return;
            }

            List<string> episodeIds = queuedSettingsString.Split(',').ToList();
            PodcastSqlModel sqlModel = PodcastSqlModel.getInstance();
            foreach(string episodeIdStr in episodeIds) 
            {
                if (String.IsNullOrEmpty(episodeIdStr))
                {
                    continue;
                }

                try
                {
                    int episodeId = Int16.Parse(episodeIdStr);
                    PodcastEpisodeModel episode = sqlModel.episodeForEpisodeId(episodeId);
                    if (episode == null)
                    {
                        Debug.WriteLine("Warning: Got NULL episode when processing stored queued download episodes!");
                        return;
                    }

                    episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued;
                    m_episodeDownloadQueue.Enqueue(episode);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error: Could not parse queued episode ids. Message: " + e.Message);
                }
            }

            if (m_currentBackgroundTransfer == null && m_episodeDownloadQueue.Count > 0)
            {
                startNextEpisodeDownload();
            }
        }

        private void pushToQueueCache(PodcastEpisodeModel episode)
        {
            string queuedSettingsString = (string)m_applicationSettings[App.LSKEY_PODCAST_DOWNLOAD_QUEUE];
            if (String.IsNullOrEmpty(queuedSettingsString) == false)
            {
                queuedSettingsString += ",";
            }

            queuedSettingsString += episode.EpisodeId.ToString();

            m_applicationSettings.Remove(App.LSKEY_PODCAST_DOWNLOAD_QUEUE);
            m_applicationSettings.Add(App.LSKEY_PODCAST_DOWNLOAD_QUEUE, queuedSettingsString);
            m_applicationSettings.Save();
        }

        private void popFromQueueCache()
        {
            string queuedSettingsString = (string)m_applicationSettings[App.LSKEY_PODCAST_DOWNLOAD_QUEUE];
            List<string> episodeIds = queuedSettingsString.Split(',').ToList();
            queuedSettingsString = "";
            for (int i = 1; i < episodeIds.Count; i++)
            {
                if (String.IsNullOrEmpty(queuedSettingsString) == false) 
                {
                    queuedSettingsString += ",";
                }

                queuedSettingsString += episodeIds[i];
            }

            m_applicationSettings.Remove(App.LSKEY_PODCAST_DOWNLOAD_QUEUE);
            m_applicationSettings.Add(App.LSKEY_PODCAST_DOWNLOAD_QUEUE, queuedSettingsString);
            m_applicationSettings.Save();
        }

        private void removeFromQueueCache(PodcastEpisodeModel episode) 
        {
            string queuedSettingsString = (string)m_applicationSettings[App.LSKEY_PODCAST_DOWNLOAD_QUEUE];
            List<string> episodeIds = queuedSettingsString.Split(',').ToList();
            queuedSettingsString = "";
            for (int i = 0; i < episodeIds.Count; i++)
            {
                if (Int16.Parse(episodeIds[i]) == episode.EpisodeId)
                {
                    continue;
                }

                if (String.IsNullOrEmpty(queuedSettingsString) == false)
                {
                    queuedSettingsString += ",";
                }

                queuedSettingsString += episodeIds[i];
            }

            m_applicationSettings.Remove(App.LSKEY_PODCAST_DOWNLOAD_QUEUE);
            m_applicationSettings.Add(App.LSKEY_PODCAST_DOWNLOAD_QUEUE, queuedSettingsString);
            m_applicationSettings.Save();
        }

        private void removeEpisodeFromDownloadQueue(PodcastEpisodeModel episode)
        {
            m_episodeDownloadQueue.RemoveItem(episode);
            removeFromQueueCache(episode);
        }

        private void startNextEpisodeDownload(TransferPreferences useTransferPreferences = TransferPreferences.AllowCellularAndBattery)
        {
            if (m_episodeDownloadQueue.Count > 0)
            {
                popFromQueueCache();

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
                    bool settingsAllowCellular = PodcastSqlModel.getInstance().settings().IsUseCellularData;
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
                m_currentBackgroundTransfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);

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

        void backgroundTransferProgressChanged(object sender, BackgroundTransferEventArgs e)
        {
            m_currentEpisodeDownload.DownloadPercentage = (int)(((float)e.Request.BytesReceived / (float)e.Request.TotalBytesToReceive) * 100);
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
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading;
                    break;
            }
        }

        private void completePodcastDownload(BackgroundTransferRequest transferRequest)
        {
            // If the status code of a completed transfer is 200 or 206, the
            // transfer was successful
            if (transferRequest.TransferError == null && 
               (transferRequest.StatusCode == 200 || transferRequest.StatusCode == 206))
            {
#if DEBUG
                MessageBox.Show("Downloaded bytes: " + transferRequest.BytesReceived,
                                "Completed", MessageBoxButton.OK);
#endif

                Debug.WriteLine("Transfer request completed succesfully.");
                updateEpisodeWhenDownloaded(m_currentEpisodeDownload);
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
                        App.showErrorToast("Could not download the episode from the server.");
                        break;
                }


                if (m_currentEpisodeDownload != null)
                {
                    m_currentEpisodeDownload.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Idle;
                    m_currentEpisodeDownload.deleteDownloadedEpisode();
                }
            }

            cleanupEpisodeDownload(transferRequest);

            // And start a next round of downloading.
            startNextEpisodeDownload();
        }

        private void updateEpisodeWhenDownloaded(PodcastEpisodeModel episode)
        {
            Debug.WriteLine("Updating episode information for episode when download completed: " + episode.EpisodeName);

            Debug.WriteLine(" * Downloaded file name: " + episode.EpisodeFile);
            episode.EpisodeDownloadState = PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded;
            Debug.WriteLine(" * Episode download state: " + episode.EpisodeDownloadState.ToString());
            Debug.WriteLine(" * Episode play state: " + episode.EpisodePlayState.ToString());
            episode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded;
            Debug.WriteLine(" * Subscription unplayed episodes: " + episode.PodcastSubscription.NumberOfEpisodesText);
            episode.PodcastSubscription.unplayedEpisodesChanged();

            PodcastSqlModel.getInstance().SubmitChanges();
        }

        private void cleanupEpisodeDownload(BackgroundTransferRequest transferRequest)
        {
            m_currentBackgroundTransfer.TransferStatusChanged -= new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);
            m_currentBackgroundTransfer.TransferProgressChanged -= new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);

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

            // Remove all unnecessary cruft from the filename
            string cleanFilename = "";
            foreach (Char c in podcastEpisodeFilename.ToCharArray())
            {
                if (c == '.' || Char.IsLetterOrDigit(c))
                {
                    cleanFilename += c;
                }
            }
            
            podcastEpisodeFilename = cleanFilename;
            podcastEpisodeFilename = String.Format("{0}_{1}", DateTime.Now.Millisecond, podcastEpisodeFilename);
            
            string localPodcastEpisodeFilename = App.PODCAST_DL_DIR + "/" + podcastEpisodeFilename;
            Debug.WriteLine("Found episode filename: " + localPodcastEpisodeFilename);

            return localPodcastEpisodeFilename;
        }
        #endregion
    }
}
