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

namespace Podcatcher
{
    public class PodcastEpisodesDownloadManager
    {
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
            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Queued;
            m_episodeDownloadQueue.Enqueue(episode);

            if (m_currentEpisodeDownload == null)
            {
                startNextEpisodeDownload();
            }
        }

        public void cancelEpisodeDownload(PodcastEpisodeModel episode)
        {
            // Update new episode state.
            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Idle;

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

            findCurrentTransfer();
            if (m_currentBackgroundTransfer != null) { 
                processOngoingTransfer();
            }
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
            if (m_currentBackgroundTransfer != null
                && m_applicationSettings.Contains(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID))
            {
                Debug.WriteLine("Found ongoing episode download...");

                m_currentBackgroundTransfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);
                m_currentBackgroundTransfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);

                int downloadingEpisodeId = (int)m_applicationSettings[App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID];
                m_currentEpisodeDownload = PodcastSqlModel.getInstance().episodeForEpisodeId(downloadingEpisodeId);
                m_currentEpisodeDownload.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Downloading;
                m_episodeDownloadQueue.Enqueue(m_currentEpisodeDownload);

                ProcessTransfer(m_currentBackgroundTransfer);
            }
        }

        private void findCurrentTransfer()
        {
            IEnumerable<BackgroundTransferRequest> requests = BackgroundTransferService.Requests;
            foreach (BackgroundTransferRequest transfer in requests)
            {
                if (transfer.TransferStatus == TransferStatus.Transferring)
                {
                    m_currentBackgroundTransfer = transfer;
                    break;
                }
            }
        }

        private void removeEpisodeFromDownloadQueue(PodcastEpisodeModel episode)
        {
            m_episodeDownloadQueue.RemoveItem(episode);
        }

        private void startNextEpisodeDownload()
        {
            if (m_episodeDownloadQueue.Count > 0)
            {
                m_currentEpisodeDownload = m_episodeDownloadQueue.Peek();
                Uri downloadUri;
                try
                {
                    downloadUri = new Uri(m_currentEpisodeDownload.EpisodeDownloadUri, UriKind.Absolute);
                }
                catch (Exception)
                {
                    App.showErrorToast("Cannot download the episode.");
                    return;
                }

                string episodeFile = localEpisodeFileName(m_currentEpisodeDownload);
                if (string.IsNullOrEmpty(episodeFile))
                {
                    App.showErrorToast("Cannot download the episode.");
                    return;
                }

                // Create a new background transfer request for the podcast episode download.
                m_currentBackgroundTransfer = new BackgroundTransferRequest(downloadUri,
                                                                            new Uri(episodeFile, UriKind.Relative));
                if (canAllowCellularDownload(m_currentEpisodeDownload))
                {
                    m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
                } else {
                    m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.None;
                }
                                                                  
                m_currentBackgroundTransfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);
                m_currentBackgroundTransfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);

                // Store request to the episode.
                m_currentEpisodeDownload.DownloadRequest = m_currentBackgroundTransfer;

                m_applicationSettings.Remove(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID);
                m_applicationSettings.Add(App.LSKEY_PODCAST_EPISODE_DOWNLOADING_ID, m_currentEpisodeDownload.EpisodeId);

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

        private bool canAllowCellularDownload(PodcastEpisodeModel m_currentEpisodeDownload)
        {
            bool allowCellular = false;
            if (PodcastPlayerControl.isAudioPodcast(m_currentEpisodeDownload))      // Allow when d/l audio
            {
                if (m_currentEpisodeDownload.EpisodeDownloadSize == 0 ||            // We had an error where the d/l size was not parsed. So we have to by default allow this not to change previous behavior.
                    m_currentEpisodeDownload.EpisodeDownloadSize < 100000000)       // Allow <100MB of audio
                {
                    allowCellular = true;
                }
            }

            return allowCellular;
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
            switch (backgroundTransferRequest.TransferStatus)
            {
                case TransferStatus.WaitingForWiFi:
                    Debug.WriteLine("Transfer status: WaitingForWiFi");
                    WaitingForWiFi = true;
                    break;
                case TransferStatus.WaitingForExternalPower:
                    Debug.WriteLine("Transfer status: WaitingForExternalPower");
                    WaitingForExternalPower = true;
                    break;
                case TransferStatus.WaitingForExternalPowerDueToBatterySaverMode:
                    Debug.WriteLine("Transfer status: WaitingForExternalPowerDueToBatterySaverMode");
                    WaitingForExternalPowerDueToBatterySaverMode = true;
                    break;
                case TransferStatus.WaitingForNonVoiceBlockingNetwork:
                    Debug.WriteLine("Transfer status: WaitingForNonVoiceBlockingNetwork");
                    WaitingForNonVoiceBlockingNetwork = true;
                    break;
                case TransferStatus.Completed:
                    Debug.WriteLine("Transfer completed.");
                    completePodcastDownload(backgroundTransferRequest);
                    break;
                case TransferStatus.Transferring:
                    m_currentEpisodeDownload.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Downloading;
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
                Debug.WriteLine("Transfer request completed succesfully.");
                m_currentEpisodeDownload.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Playable;
                m_currentEpisodeDownload.EpisodeFile = localEpisodeFileName(m_currentEpisodeDownload);
                m_currentEpisodeDownload.PodcastSubscription.unplayedEpisodesChanged();
            }
            else
            {
                Debug.WriteLine("Transfer request completed with error code: " + transferRequest.StatusCode + ", " + transferRequest.TransferError);
                if (m_currentEpisodeDownload != null)
                {
                    m_currentEpisodeDownload.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Idle;
                }
            }

            cleanupEpisodeDownload(transferRequest);

            // And start a next round of downloading.
            startNextEpisodeDownload();
        }

        private void cleanupEpisodeDownload(BackgroundTransferRequest transferRequest)
        {
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

            m_currentBackgroundTransfer.TransferStatusChanged -= new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);
            m_currentBackgroundTransfer.TransferProgressChanged -= new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);
            m_currentBackgroundTransfer = null;

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

        private string localEpisodeFileName(PodcastEpisodeModel podcastEpisode)
        {
            // Parse the filename of the logo from the remote URL.
            string localPath = new Uri(podcastEpisode.EpisodeDownloadUri).LocalPath;
            string podcastEpisodeFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);

            // Remove whitespace from filename, as WP has difficulties handling that kind of names.
            podcastEpisodeFilename = podcastEpisodeFilename.Replace(' ', '_');

            string localPodcastEpisodeFilename = App.PODCAST_DL_DIR + "/" + podcastEpisodeFilename;
            Debug.WriteLine("Found episode filename: " + localPodcastEpisodeFilename);

            return localPodcastEpisodeFilename;
        }
        #endregion
    }
}
