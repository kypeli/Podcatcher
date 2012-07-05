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

        public void addEpisodeToDownloadQueue(PodcastEpisodeModel episode)
        {
            episode.OnPodcastEpisodeStartedDownloading += new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeStartedDownloading);
            episode.OnPodcastEpisodeFinishedDownloading += new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeFinishedDownloading);

            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Queued;
            m_episodeDownloadQueue.Enqueue(episode);
        }

        #region private
        private static PodcastEpisodesDownloadManager m_instance = null;
        private Queue<PodcastEpisodeModel> m_episodeDownloadQueue = new Queue<PodcastEpisodeModel>();

        private PodcastEpisodesDownloadManager()
        {
        }

        private void podcastEpisode_OnPodcastEpisodeStartedDownloading(object sender, PodcastEpisodeModel.PodcastEpisodesArgs e)
        {
            PodcastEpisodeModel episode = sender as PodcastEpisodeModel;
            episode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Downloading;
        }

        private void podcastEpisode_OnPodcastEpisodeFinishedDownloading(object sender, PodcastEpisodeModel.PodcastEpisodesArgs e)
        {
            PodcastEpisodeModel episode = sender as PodcastEpisodeModel;

            // Disconnect model event handlers just in case.
            episode.OnPodcastEpisodeFinishedDownloading -= new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeFinishedDownloading);
            episode.OnPodcastEpisodeStartedDownloading -= new PodcastEpisodeModel.PodcastEpisodesHandler(podcastEpisode_OnPodcastEpisodeStartedDownloading);
        }


        #endregion
    }
}
