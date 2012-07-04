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

namespace Podcatcher
{
    public class PodcastEpisodesDownloadManager
    {
        public static PodcastEpisodesDownloadManager getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastEpisodesDownloadManager();
            }

            return m_instance;
        }

        public void addEpisodeToDownloadQueue(PodcastEpisodeModel podcastEpisode)
        {
            podcastEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Downloading;
            m_episodeDownloadQueue.Enqueue(podcastEpisode);
        }

        #region private
        private static PodcastEpisodesDownloadManager m_instance = null;
        private Queue<PodcastEpisodeModel> m_episodeDownloadQueue = new Queue<PodcastEpisodeModel>();

        private PodcastEpisodesDownloadManager()
        {
        }
        #endregion
    }
}
