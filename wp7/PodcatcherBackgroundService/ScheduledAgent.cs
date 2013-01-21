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
using System.Globalization;

namespace PodcatcherBackgroundService
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        // Root key name for storing episode information for background service.
        private const string LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE = "bg_subscription_latest_episode";
        private int m_requestsStarted = 0;
        private Queue<ShellTile> m_pinnedSubscriptions = null;

        private static volatile bool _classInitialized;
        IsolatedStorageSettings m_settings;

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

                m_settings = IsolatedStorageSettings.ApplicationSettings;

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

            m_pinnedSubscriptions = new Queue<ShellTile>(ShellTile.ActiveTiles.Where(tile => tile.NavigationUri.ToString().Contains("podcastId=")));
            if (m_pinnedSubscriptions.Count > 0)
            {
                refreshPinnedSubscription(m_pinnedSubscriptions);
            }
            else
            {
                NotifyComplete();
            }
        }

        private void refreshPinnedSubscription(Queue<ShellTile> tiles)
        {
            ShellTile tile = null;
            if (tiles.Count > 0) 
            {
                tile = tiles.Dequeue();
            } else 
            {
                Debug.WriteLine("All pinned subscriptions refreshed.");
                NotifyComplete();
                return;
            }

            Debug.WriteLine("Found a pinned subscription: " + tile.NavigationUri);

            String subscriptionId = getSubscriptionIdForTile(tile);
            if (subscriptionId == "")
            {
                Debug.WriteLine("Could not parse the subscription ID from tile navigation URL!");
                refreshPinnedSubscription(m_pinnedSubscriptions);
            }

            if (m_settings.Contains(LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionId) == false)
            {
                Debug.WriteLine("Could not open subscription meta data! Key: " + LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionId);
                refreshPinnedSubscription(m_pinnedSubscriptions);
            }

            String subscriptionData = m_settings[LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionId] as String;
            String podcastUrl = subscriptionData.Split('|')[2];

            WebClient web = new WebClient();
            web.DownloadStringCompleted += new DownloadStringCompletedEventHandler(web_DownloadStringCompleted);
            web.DownloadStringAsync(new Uri(podcastUrl), subscriptionData);
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

            Debug.WriteLine("Updating tile with: {0} new episode{1}, data {2} ", newEpisodes, (newEpisodes > 1 ? "s" : ""), subscriptionData);

            refreshPinnedSubscription(m_pinnedSubscriptions);
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
                        where (parsePubDate(episode.Element("pubDate").Value) > latestEpisodeDateTime)
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

        private static DateTime parsePubDate(string pubDateString)
        {
            DateTime resultDateTime = new DateTime();

            if (String.IsNullOrEmpty(pubDateString))
            {
                Debug.WriteLine("WARNING: Empty pubDate string given. Cannot parse it...");
                return resultDateTime;
            }

            // pubDateString is e.g. 'Mon, 25 Jun 2012 11:53:25 -0700'
            int indexOfComma = pubDateString.IndexOf(',');
            if (indexOfComma >= 0)
            {
                pubDateString = pubDateString.Substring(indexOfComma + 2);                      // Find the ',' and remove 'Mon, '
            }

            // Next we try to parse the date string field in various formats that 
            // can be found in podcast RSS feeds.
            resultDateTime = getDateTimeWithFormat("dd MMM yyyy HH:mm:ss", pubDateString, "dd MMM yyyy HH:mm:ss".Length);      // Parse as 25 Jun 2012 11:53:25
            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMM yyyy HH:mm:ss", pubDateString, "d MMM yyyy HH:mm:ss".Length);   // Parse as 2 Jun 2012 11:53:25
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("dd MMMM yyyy HH:mm:ss GMT", pubDateString, pubDateString.Length);   // Parse as 2 December 2012 11:53:23 GMT
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("dd MMM yyyy HH:mm", pubDateString, "dd MMM yyyy HH:mm".Length);   // Parse as 2 Jun 2012 11:53
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned again. This is for you, Hacker Public Radio and the Economist!.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("yyyy-MM-dd", pubDateString, "yyyy-MM-dd".Length);            // Parse as 2012-06-25
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Talk Radio 702 - The Week That Wasn't
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("yyyy/MM/dd HH:mm:ss", pubDateString, "yyyy/MM/dd HH:mm:ss".Length);  // Parse as 2012/12/17 03:18:16 PM
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // The Dan Patrick Show: Podcast
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMMM yyyy HH:mm:ss EST", pubDateString, pubDateString.Length);  // Parse as 11 January 2013 10:10:10 EST
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                Debug.WriteLine("ERROR: Could not parse pub date!");
            }

            return resultDateTime;
        }

        private static DateTime getDateTimeWithFormat(string dateFormat, string pubDateString, int parseLength)
        {
            DateTime result = new DateTime();
            if (parseLength > pubDateString.Length)
            {
                Debug.WriteLine("Cannot parse pub date as its length doesn't match the format length we are looking for. Returning.");
                return result;
            }

            pubDateString = pubDateString.Substring(0, parseLength);
            if (DateTime.TryParseExact(pubDateString,
                                       dateFormat,
                                       new CultureInfo("en-US"),
                // CultureInfo.InvariantCulture,
                                       DateTimeStyles.None,
                                       out result) == false)
            {
                //  Debug.WriteLine("Warning: Cannot parse feed's pubDate: '" + pubDateString + "', format: " + dateFormat);
            }

            return result;
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