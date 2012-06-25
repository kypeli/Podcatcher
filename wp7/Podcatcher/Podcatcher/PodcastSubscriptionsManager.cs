using System;
using System.Net;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Navigation;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Windows.Controls;
using Coding4Fun.Phone.Controls;

namespace Podcatcher
{
    public delegate void SubscriptionManagerHandler(object source, SubscriptionManagerArgs e);

    public class SubscriptionManagerArgs
    {

    }

    public class PodcastSubscriptionsManager
    {
        // ***************** Public implementation ******************* //

        public event SubscriptionManagerHandler OnPodcastChannelFinished;
        public event SubscriptionManagerHandler OnPodcastChannelFinishedWithError;

        public static PodcastSubscriptionsManager getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastSubscriptionsManager();
            }

            return m_instance;
        }

        public ObservableCollection<PodcastModel> PodcastSubscriptions
        {
            get
            {
                return m_podcastsModel;
            }

            private set
            {
                if (m_podcastsModel != value)
                {
                    m_podcastsModel = value;
                }
            }
        }

        public void addSubscriptionFromURL(string podcastRss)
        {
            // DEBUG
            if (String.IsNullOrEmpty(podcastRss))
            {
                podcastRss = "http://leo.am/podcasts/twit";
            }

            if (podcastRss.StartsWith("http://") == false)
            {
                podcastRss = podcastRss.Insert(0, "http://");
            }

            Uri podcastRssUri;
            try
            {
                podcastRssUri = new Uri(podcastRss);
            }
            catch (UriFormatException)
            {
                Debug.WriteLine("ERROR: Cannot add podcast from that URL.");
                OnPodcastChannelFinishedWithError(this, null);
                return;
            }

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadPodcastRSSCompleted);
            wc.DownloadStringAsync(podcastRssUri);

            Debug.WriteLine("Fetching podcast from URL: " + podcastRss.ToString());
        }

        
        // ***************** Private implementation ******************* //

        private static PodcastSubscriptionsManager m_instance = null;

        private PodcastSubscriptionsManager()
        {
            m_podcastsModel = new ObservableCollection<PodcastModel>();
        }

        private ObservableCollection<PodcastModel> m_podcastsModel;

        private void wc_DownloadPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null
                || e.Cancelled)
            {
                Debug.WriteLine("ERROR: Web request failed. Message: " + e.Error.Message);
                OnPodcastChannelFinishedWithError(this, null);
                return;
            }
                
            PodcastModel podcastModel = PodcastFactory.podcastModelFromRSS(e.Result);            
            Debug.WriteLine("Got new podcast, name: " + podcastModel.PodcastName);

            OnPodcastChannelFinished(this, null);

            PodcastSubscriptions.Add(podcastModel);
        }
    }
}
