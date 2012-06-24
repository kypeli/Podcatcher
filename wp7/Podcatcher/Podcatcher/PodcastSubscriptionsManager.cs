using System;
using System.Net;
using System.Diagnostics;

namespace Podcatcher
{
    public class PodcastSubscriptionsManager
    {
        private static PodcastSubscriptionsManager m_instance = null;
        private PodcastSubscriptionsModel m_subscriptions = new PodcastSubscriptionsModel();

        private PodcastSubscriptionsManager()
        {
        }

        public static PodcastSubscriptionsManager getInstance()
        {
            if (m_instance == null) {
                m_instance = new PodcastSubscriptionsManager();
            }

            return m_instance;
        }

        public PodcastSubscriptionsModel PodcastSubscriptions {
            get { return m_subscriptions; }
            private set { } 
        }

        public void addSubscriptionFromURL(string podcastRss)
        {
            if (podcastRss.StartsWith("http://") == false)
            {
                podcastRss = podcastRss.Insert(0, "http://");
            }

            Uri podcastRssUri = new Uri(podcastRss);
            
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadPodcastRSSCompleted);
            wc.DownloadStringAsync(podcastRssUri);

            Debug.WriteLine("Fetching podcast from URL: " + podcastRss.ToString());
        }

        void wc_DownloadPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Debug.WriteLine("Got XML: " + e.Result.ToString());
        }
    }
}
