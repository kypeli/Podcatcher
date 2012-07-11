using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using Podcatcher.CustomControls;

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
            episode.OnPodcastEpisodeStartedDownloading += new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeStartedDownloading);
            episode.OnPodcastEpisodeFinishedDownloading += new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeFinishedDownloading);

            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Queued;
            m_episodeDownloadQueue.Enqueue(episode);

            if (m_currentEpisodeDownload == null)
            {
                startNextEpisodeDownload();
            }
        }

        #region private
        private static PodcastEpisodesDownloadManager m_instance                = null;
        private ObservableQueue<PodcastEpisodeModel> m_episodeDownloadQueue     = new ObservableQueue<PodcastEpisodeModel>();
        private PodcastEpisodeModel m_currentEpisodeDownload                    = null;
        private IsolatedStorageFile m_localPodcastDownloadDir                   = null;

        private PodcastEpisodesDownloadManager()
        {
            m_localPodcastDownloadDir = IsolatedStorageFile.GetUserStoreForApplication();
            m_localPodcastDownloadDir.CreateDirectory(App.PODCAST_DL_DIR);
        }

        private void startNextEpisodeDownload()
        {
            if (m_episodeDownloadQueue.Count > 0)
            {
                m_currentEpisodeDownload = m_episodeDownloadQueue.Dequeue();
                m_currentEpisodeDownload.downloadEpisode();
            }
        }

        private void podcastEpisode_OnPodcastEpisodeStartedDownloading(object sender, PodcastEpisodeModel.PodcastEpisodesArgs e)
        {
            PodcastEpisodeModel episode = sender as PodcastEpisodeModel;
            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Downloading;
        }

        private void podcastEpisode_OnPodcastEpisodeFinishedDownloading(object sender, PodcastEpisodeModel.PodcastEpisodesArgs e)
        {
            PodcastEpisodeModel episode = sender as PodcastEpisodeModel;
            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Playable;

            // Disconnect model event handlers just in case.
            episode.OnPodcastEpisodeFinishedDownloading -= new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeFinishedDownloading);
            episode.OnPodcastEpisodeStartedDownloading -= new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeStartedDownloading);

            m_currentEpisodeDownload = null;

            startNextEpisodeDownload();
        }
        #endregion
    }
}
