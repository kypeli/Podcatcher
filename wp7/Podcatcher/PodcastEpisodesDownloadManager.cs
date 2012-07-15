using System;
using System.Windows;
using Podcatcher.CustomControls;
using Podcatcher.ViewModels;
using Microsoft.Phone.BackgroundTransfer;
using System.Diagnostics;

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
            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Queued;
            m_episodeDownloadQueue.Enqueue(episode);

            if (m_currentEpisodeDownload == null)
            {
                startNextEpisodeDownload();
            }
        }

        #region private
        private static PodcastEpisodesDownloadManager m_instance            = null;
        private ObservableQueue<PodcastEpisodeModel> m_episodeDownloadQueue = new ObservableQueue<PodcastEpisodeModel>();
        private PodcastEpisodeModel m_currentEpisodeDownload                = null;
        private BackgroundTransferRequest m_currentBackgroundTransfer       = null;

        // Booleans for tracking if any transfers are waiting for user action. 
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        private PodcastEpisodesDownloadManager()
        {
        }

        private void startNextEpisodeDownload()
        {
            if (m_episodeDownloadQueue.Count > 0)
            {
                m_currentEpisodeDownload = m_episodeDownloadQueue.Peek();
                m_currentEpisodeDownload.EpisodeFile = localEpisodeFileName(m_currentEpisodeDownload);
                // Create a new background transfer request for the podcast episode download.
                m_currentBackgroundTransfer = new BackgroundTransferRequest(new Uri(m_currentEpisodeDownload.EpisodeDownloadUri, UriKind.Absolute),
                                                                            new Uri(m_currentEpisodeDownload.EpisodeFile, UriKind.Relative));
                m_currentBackgroundTransfer.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
                m_currentBackgroundTransfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferStatusChanged);
                m_currentBackgroundTransfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(backgroundTransferProgressChanged);
            
                BackgroundTransferService.Add(m_currentBackgroundTransfer);
            }
        }

        void backgroundTransferProgressChanged(object sender, BackgroundTransferEventArgs e)
        {
            float foo = ((float)e.Request.BytesReceived / (float)e.Request.TotalBytesToReceive);
            m_currentEpisodeDownload.DownloadPercentage = (int)(foo * 100);
            Debug.WriteLine("Download progress: " + m_currentEpisodeDownload.DownloadPercentage);
        }

        void backgroundTransferStatusChanged(object sender, BackgroundTransferEventArgs e)
        {
            ProcessTransfer(e.Request);
            UpdateUI(e.Request);
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
                    m_currentEpisodeDownload.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Downloading;
                    break;
            }
        }

        private void completePodcastDownload(BackgroundTransferRequest transferRequest)
        {
            // Remove the transfer request in order to make room in the 
            // queue for more transfers. Transfers are not automatically
            // removed by the system.
            RemoveTransferRequest(transferRequest.RequestId);

            PodcastSqlModel.getInstance().setEpisodeState(m_currentEpisodeDownload, PodcastEpisodeModel.EpisodeStateVal.Playable);

            m_currentEpisodeDownload = null;
            m_episodeDownloadQueue.Dequeue();

            startNextEpisodeDownload();
        }

        private void RemoveTransferRequest(string transferID)
        {
            // Use Find to retrieve the transfer request with the specified ID.
            BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);

            // Try to remove the transfer from the background transfer service.
            try
            {
                BackgroundTransferService.Remove(transferToRemove);
            }
            catch (Exception)
            {
                // Handle the exception.
            }
        }

        private void UpdateUI(BackgroundTransferRequest backgroundTransferRequest)
        {
            if (WaitingForExternalPower)
            {
                MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power to continue transferring.");
            }
            if (WaitingForExternalPowerDueToBatterySaverMode)
            {
                MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or disable Battery Saver Mode to continue transferring.");
            }
            if (WaitingForNonVoiceBlockingNetwork)
            {
                MessageBox.Show("You have one or more file transfers waiting for a network that supports simultaneous voice and data.");
            }
            if (WaitingForWiFi)
            {
                MessageBox.Show("You have one or more file transfers waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring.");
            }
        }

        private string localEpisodeFileName(PodcastEpisodeModel podcastEpisode)
        {
            // Parse the filename of the logo from the remote URL.
            string localPath = new Uri(podcastEpisode.EpisodeDownloadUri).LocalPath;
            string podcastEpisodeFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);

            string localPodcastEpisodeFilename = App.PODCAST_DL_DIR + podcastEpisodeFilename;
            Debug.WriteLine("Found episode filename: " + localPodcastEpisodeFilename);

            return localPodcastEpisodeFilename;
        }
        #endregion
    }
}
