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
using System.ComponentModel;

namespace Podcatcher
{
    public delegate void SubscriptionManagerHandler(object source, SubscriptionManagerArgs e);

    public class SubscriptionManagerArgs
    {
        public String message;
        public PodcastSubscriptionModel addedSubscription;
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

        public void deleteSubscription(PodcastSubscriptionModel podcastSubscriptionModel)
        {
            podcastSubscriptionModel.cleanupForDeletion();
            m_podcastsSqlModel.deleteSubscription(podcastSubscriptionModel);
        }


        /************************************* Private implementation *******************************/
        #region privateImplementations
        private static PodcastSubscriptionsManager m_instance = null;
        private PodcastSqlModel m_podcastsSqlModel            = null;
        private Random m_random                               = null;

        private PodcastSubscriptionsManager()
        {
            m_podcastsSqlModel = PodcastSqlModel.getInstance();
            m_random = new Random();

            // Hook a callback method to the signal that we emit when the subscription has been added.
            // This way we can continue the synchronous execution.
            this.OnPodcastChannelFinished += new SubscriptionManagerHandler(PodcastSubscriptionsManager_OnPodcastAddedFinished);
        }

        private void wc_DownloadPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null
                || e.Cancelled)
            {
                PodcastSubscriptionFailedWithMessage("ERROR: Web request failed. Message: " + e.Error.Message);
                return;
            }

            string podcastRss = e.Result;
            PodcastSubscriptionModel podcastModel = PodcastFactory.podcastModelFromRSS(podcastRss);
            if (podcastModel == null)
            {
                PodcastSubscriptionFailedWithMessage("ERROR: Could not parse podcast subscription from that location.");
                return;
            }

            if (m_podcastsSqlModel.isPodcastInDB(podcastModel))
            {
                PodcastSubscriptionFailedWithMessage("Already subscribed to the podcast.");
                return;
            }

            podcastModel.CachedPodcastRSSFeed = podcastRss;                        
            podcastModel.PodcastLogoLocalLocation = localLogoFileName(podcastModel);
            m_podcastsSqlModel.addSubscription(podcastModel);

            podcastModel.fetchChannelLogo();

            SubscriptionManagerArgs addArgs = new SubscriptionManagerArgs();
            addArgs.addedSubscription = podcastModel;

            OnPodcastChannelFinished(this, addArgs);
        }

        private void PodcastSubscriptionsManager_OnPodcastAddedFinished(object source, SubscriptionManagerArgs e)
        {
            PodcastSubscriptionModel subscriptionModel = e.addedSubscription;
            Debug.WriteLine("Podcast added successfully. Name: " + subscriptionModel.PodcastName);

            subscriptionModel.EpisodesManager.updatePodcastEpisodes();
        }

        private void PodcastSubscriptionFailedWithMessage(string message)
        {
            Debug.WriteLine(message);
            SubscriptionManagerArgs args = new SubscriptionManagerArgs();
            args.message = message;

            OnPodcastChannelFinishedWithError(this, args);
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
