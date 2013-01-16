using System.Windows;
using Microsoft.Phone.Scheduler;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System.Data.Linq;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System;
using System.Xml.Linq;

namespace PodcatcherBackgroundService
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        // Root key name for storing episode information for background service.
        private const string LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE = "bg_subscription_latest_episode";
        private int m_requestsStarted = 0;

        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            Debug.WriteLine("Starting background task.");

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            var pinnedSubscriptionTiles = ShellTile.ActiveTiles.Where(tile => tile.NavigationUri.ToString().Contains("podcastId="));
            foreach (ShellTile tile in pinnedSubscriptionTiles)
            {
                Debug.WriteLine("Found a pinned subscription: " + tile.NavigationUri);

                String subscriptionId = getSubscriptionIdForTile(tile);

                if (subscriptionId == "") 
                {
                    Debug.WriteLine("Could not parse the subscription ID from tile navigation URL!");
                    NotifyComplete();
                }

                if (settings.Contains(LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionId) == false) 
                {
                    Debug.WriteLine("Could not open subscription meta data! Key: " + LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionId);
                    NotifyComplete();
                }

                String subscriptionData = settings[LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionId] as String;
                String podcastUrl = subscriptionData.Split('|')[2];

                WebClient web = new WebClient();
                web.DownloadStringCompleted += new DownloadStringCompletedEventHandler(web_DownloadStringCompleted);
                web.DownloadStringAsync(new Uri(podcastUrl), subscriptionData);
                startedRequest();
            }
        }

        private static String getSubscriptionIdForTile(ShellTile tile)
        {
            String subscriptionId = "";
            List<String> urlParams = tile.NavigationUri.ToString().Split('=').ToList();
            for (int i = 0; i < urlParams.Count - 1; i++)
            {
                if (urlParams[i].Contains("podcastId"))
                {
                    foreach (Char c in urlParams[i + 1].ToCharArray())
                    {
                        if (Char.IsDigit(c)) 
                        {
                            subscriptionId += c;
                        }
                    }
                    break;    
                }
            }

            return subscriptionId;
        }

        void web_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine("Error: " + e.Error);
                finishedRequest();
                return;
            }

            String subscriptionData = e.UserState as String;
            String podcastRss = e.Result;

            int newEpisodes = getNumOfNewEpisodes(subscriptionData, podcastRss);
            if (newEpisodes > 0)
            {
                updatePinnedSubscription(subscriptionData, newEpisodes);
            }

            finishedRequest();
        }

        private int getNumOfNewEpisodes(String subscriptionData, String podcastRss)
        {
            XDocument podcastRssXmlDoc = null;
            try
            {
                podcastRssXmlDoc = XDocument.Parse(podcastRss);
            }
            catch (System.Xml.XmlException xmle)
            {
                Debug.WriteLine("ERROR: Parse error when parsing podcast episodes. Message: " + xmle.Message);
                return 0;
            }

            String timestamp = subscriptionData.Split('|')[1];
            DateTime latestEpisodeDateTime = DateTime.Parse(timestamp);
            var query = from episode in podcastRssXmlDoc.Descendants("item")
                        where (DateTime.Parse(episode.Element("pubDate").Value) > latestEpisodeDateTime)
                        select episode;

            Debug.WriteLine("Got new episodes: " + query.Count());
            return query.Count();
        }

        private void updatePinnedSubscription(string subscriptionData, int newEpisodes)
        {
            String subscriptionId = subscriptionData.Split('|')[0];
            ShellTile pinnedSubscriptionTile = ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains("podcastId=" + subscriptionId)) as ShellTile;
            if (pinnedSubscriptionTile == null)
            {
                Debug.WriteLine("Error: Could not get the pinned tile for subscription: " + subscriptionId);
                return;
            }

            StandardTileData tileData = new StandardTileData();
            tileData.BackTitle = String.Format("{0} new episode{1}", newEpisodes, (newEpisodes > 1 ? "s" : ""));
            tileData.Count = newEpisodes;
            pinnedSubscriptionTile.Update(tileData);
        }

        void startedRequest()
        {
            m_requestsStarted++;
        }

        void finishedRequest()
        {
            m_requestsStarted--;
            if (m_requestsStarted == 0)
            {
                NotifyComplete();
            }
        }
    }
}