using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Podcatcher.ViewModels
{
    public class PodcastEpisodesManager
    {
        private PodcastSubscriptionModel m_subscriptionModel;
        private BackgroundWorker         m_worker = new BackgroundWorker();
        private PodcastSqlModel          m_podcastsSqlModel;

        public PodcastEpisodesManager(PodcastSubscriptionModel subscriptionModel)
        {
            m_subscriptionModel = subscriptionModel;
            m_podcastsSqlModel = PodcastSqlModel.getInstance();
        }

        public void updatePodcastEpisodes()
        {
            Debug.WriteLine("Updating episodes for podcast: " + m_subscriptionModel.PodcastName);
            m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWorkUpdateEpisodes);
            m_worker.RunWorkerAsync();
        }

        private void m_worker_DoWorkUpdateEpisodes(object sender, DoWorkEventArgs args)
        {
            List<PodcastEpisodeModel> episodes = m_podcastsSqlModel.episodesForSubscription(m_subscriptionModel);
//            List<PodcastEpisodeModel> episodes = m_subscriptionModel.Episodes.ToList();
            DateTime latestEpisodePublishDate = new DateTime();
            if (episodes.Count > 0)
            {
                // The episodes are in descending order as per publish date. 
                // So take the first episode and we have the latest known publish date.
                latestEpisodePublishDate = episodes.ElementAt(0).EpisodePublished;
            }

            List<PodcastEpisodeModel> newPodcastEpisodes = PodcastFactory.newPodcastEpisodes(m_subscriptionModel.CachedPodcastRSSFeed, latestEpisodePublishDate);
            if (newPodcastEpisodes == null)
            {
                Debug.WriteLine("WARNING: Got null list of new episodes.");
                return;
            }

            Debug.WriteLine("Got {0} new episodes. Writing to SQL...", newPodcastEpisodes.Count);

            m_podcastsSqlModel.insertEpisodesForSubscription(m_subscriptionModel, newPodcastEpisodes);
//            m_podcastsSqlModel.addPodcastEpisodes(newPodcastEpisodes);
        }

    }
}
