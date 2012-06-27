using System;
using System.Net;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Navigation;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Windows.Controls;
using Coding4Fun.Phone.Controls;
using Podcatcher.ViewModels;
using System.Text;

namespace Podcatcher
{
    public delegate void SubscriptionManagerHandler(object source, SubscriptionManagerArgs e);

    public class SubscriptionManagerArgs
    {
    }

    public class PodcastSubscriptionsManager
    {
        /************************************* Public implementations *******************************/

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

        public void deleteSubscription(PodcastSubscriptionModel podcastModel)
        {
            m_podcastsModel.deleteSubscription(podcastModel);
        }


        /************************************* Private implementation *******************************/
        #region privateImplementations
        private static PodcastSubscriptionsManager m_instance = null;
        private PodcastSqlModel m_podcastsModel               = null;
        private Random m_random                               = null;

        private PodcastSubscriptionsManager()
        {
            m_podcastsModel = PodcastSqlModel.getInstance();
            m_random = new Random();
        }

        private void wc_DownloadPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null
                || e.Cancelled)
            {
                Debug.WriteLine("ERROR: Web request failed. Message: " + e.Error.Message);
                OnPodcastChannelFinishedWithError(this, null);
                return;
            }
                
            PodcastSubscriptionModel podcastModel = PodcastFactory.podcastModelFromRSS(e.Result);            
            Debug.WriteLine("Got new podcast, name: " + podcastModel.PodcastName);

            podcastModel.PodcastLogoLocalLocation = localLogoFileName(podcastModel);

            OnPodcastChannelFinished(this, null);

            m_podcastsModel.addSubscription(podcastModel);
        }

        private string localLogoFileName(PodcastSubscriptionModel podcastModel)
        {
            // Parse the filename of the logo from the remote URL.
            string localPath = podcastModel.PodcastLogoUrl.LocalPath;
            string podcastLogoFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);

            // Make podcast logo name random.
            // This is because, if for some reason, two podcasts have the same logo name and we delete
            // one of them, we don't want the other one to be affected. Just to be sure. 
            StringBuilder podcastLogoFilename_sb = new StringBuilder(podcastLogoFilename);
            podcastLogoFilename_sb.Insert(0, m_random.Next().ToString());

            string localPodcastLogoFilename = App.PODCAST_ICON_DIR + @"/" + podcastLogoFilename_sb.ToString();
            Debug.WriteLine("Found icon filename: " + localPodcastLogoFilename);

            return localPodcastLogoFilename;
        }
        #endregion
    }
}
