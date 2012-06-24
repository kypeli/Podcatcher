using System;
using System.Net;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Navigation;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Windows.Controls;

namespace Podcatcher
{
    public class PodcastSubscriptionsManager
    {
        private static PodcastSubscriptionsManager m_instance = null;

        private PodcastSubscriptionsManager()
        {
            m_podcastsModel = new ObservableCollection<PodcastModel>();
        }

        public static PodcastSubscriptionsManager getInstance()
        {
            if (m_instance == null) {
                m_instance = new PodcastSubscriptionsManager();
            }

            return m_instance;
        }

        private ObservableCollection<PodcastModel> m_podcastsModel;
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
            PodcastModel podcastModel = PodcastFactory.podcastModelFromRSS(e.Result);            
            Debug.WriteLine("Got new podcast, name: " + podcastModel.PodcastName);

            PodcastSubscriptions.Add(podcastModel);
            NavigationService navi = (((App)Application.Current).RootFrame.Content as PhoneApplicationPage).NavigationService;
            navi.GoBack();
        }
    }
}
