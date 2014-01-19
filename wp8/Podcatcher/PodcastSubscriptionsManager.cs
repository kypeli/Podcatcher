/**
 * Copyright (c) 2012, 2013, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */



using Coding4Fun.Toolkit.Controls;
using Microsoft.Live;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json.Linq;
using Podcatcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Podcatcher
{
    public delegate void SubscriptionManagerHandler(object source, SubscriptionManagerArgs e);
    
    public delegate void SubscriptionChangedHandler(PodcastSubscriptionModel s);

    public class SubscriptionManagerArgs
    {
        public String message;
        public PodcastSubscriptionModel subscription;
        public PodcastSubscriptionsManager.SubscriptionsState state;
        public bool isImportingFromExternalService = false;
        public Uri podcastFeedRSSUri;
    }

    internal class AddSubscriptionOptions
    {
        public String rssUrl = "";
        public bool isImportingFromExternalService = false;
        public String username = "";
        public String password = "";
    }

    public class PodcastSubscriptionsManager
    {
        /************************************* Public implementations *******************************/

        public event SubscriptionManagerHandler OnPodcastChannelAddStarted;
        public event SubscriptionManagerHandler OnPodcastChannelAddFinished;
        public event SubscriptionManagerHandler OnPodcastChannelDeleteStarted;
        public event SubscriptionManagerHandler OnPodcastChannelDeleteFinished;
        public event SubscriptionManagerHandler OnPodcastChannelAddFinishedWithError;
        public event SubscriptionManagerHandler OnPodcastChannelRequiresAuthentication;
        

        public event SubscriptionManagerHandler OnExternalServiceImportStarted;
        public event SubscriptionManagerHandler OnExternalServiceImportFinished;
        public event SubscriptionManagerHandler OnExternalServiceImportFinishedWithError;

        public event SubscriptionManagerHandler OnPodcastSubscriptionsChanged;

        public event SubscriptionManagerHandler OnOPMLExportToSkydriveChanged;

        public event EpisodesEventHandler NewPlayableEpisode;
        public event EpisodesEventHandler RemovedPlayableEpisode;

        public event SubscriptionChangedHandler OnPodcastChannelPlayedCountChanged;
        public event SubscriptionChangedHandler OnPodcastChannelAdded;
        public event SubscriptionChangedHandler OnPodcastChannelRemoved;

        public delegate void EpisodesEventHandler(PodcastEpisodeModel e);

        public enum SubscriptionsState
        {
            StartedRefreshing,
            RefreshingSubscription,
            FinishedRefreshing,
            StartedSkydriveExport,
            FinishedSkydriveExport
        }

        public static PodcastSubscriptionsManager getInstance()
        {
            if (m_subscriptionManagerInstance == null)
            {
                m_subscriptionManagerInstance = new PodcastSubscriptionsManager();
            }

            return m_subscriptionManagerInstance;
        }

        public void addSubscriptionFromURL(string podcastRss, bool importingFromExternalService = false)
        {

            if (String.IsNullOrEmpty(podcastRss))
            {
#if DEBUG
                podcastRss = "http://192.168.0.6:8000/feed.rss";
#else
                Debug.WriteLine("ERROR: Empty URL.");
                PodcastSubscriptionFailedWithMessage("Empty podcast address.");
                return; 
#endif
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
                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "Malformed URL";
                OnPodcastChannelAddFinishedWithError(this, args);
                return;
            }

            AddSubscriptionOptions options = new AddSubscriptionOptions();
            options.rssUrl = podcastRss;
            options.isImportingFromExternalService = importingFromExternalService;

            if (importingFromExternalService)
            {
                m_activeExternalImportsCount++;
            }

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadPodcastRSSCompleted);
            wc.DownloadStringAsync(podcastRssUri, options);

            if (!importingFromExternalService)
            {
                OnPodcastChannelAddStarted(this, null);
            }

            Debug.WriteLine("Fetching podcast from URL: " + podcastRss.ToString());
        }

        public void addSubscriptionFromURLWithCredentials(string podcastRss, NetworkCredential nc)
        {
            if (String.IsNullOrEmpty(podcastRss))
            {
#if DEBUG
                podcastRss = "http://192.168.0.6:8000/feed.rss";
#else
                Debug.WriteLine("ERROR: Empty URL.");
                PodcastSubscriptionFailedWithMessage("Empty podcast address.");
                return; 
#endif
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
                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "Malformed URL";
                OnPodcastChannelAddFinishedWithError(this, args);
                return;
            }

            AddSubscriptionOptions options = new AddSubscriptionOptions();
            options.rssUrl = podcastRss;
            options.username = nc.UserName;
            options.password = nc.Password;

            WebClient wc = new WebClient();
            wc.Credentials = nc;
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadPodcastRSSCompleted);
            wc.DownloadStringAsync(podcastRssUri, options);

            OnPodcastChannelAddStarted(this, null);

            Debug.WriteLine("Fetching podcast from URL: " + podcastRss.ToString());
        }

        public async void deleteSubscription(PodcastSubscriptionModel podcastSubscriptionModel)
        {
            OnPodcastChannelDeleteStarted(this, null);

            await Task.Run(() =>
            {
                using (var db = new PodcastSqlModel())
                {
                    PodcastSubscriptionModel dbSubscription = db.Subscriptions.First(s => s.PodcastId == podcastSubscriptionModel.PodcastId);
                    dbSubscription.cleanupForDeletion();
                    db.deleteSubscription(dbSubscription);
                }
            });

            OnPodcastChannelDeleteFinished(this, null);
            OnPodcastChannelRemoved(podcastSubscriptionModel);
        }

        public void refreshSubscriptions()
        {
            SubscriptionManagerArgs stateChangedArgs = new SubscriptionManagerArgs();
            if (NetworkInterface.GetIsNetworkAvailable() == false)
            {
                Debug.WriteLine("No network available. Won't refresh.");                
                stateChangedArgs.state = PodcastSubscriptionsManager.SubscriptionsState.FinishedRefreshing;
                OnPodcastSubscriptionsChanged(this, stateChangedArgs);                
                return;
            }

            stateChangedArgs.state = PodcastSubscriptionsManager.SubscriptionsState.StartedRefreshing;
            OnPodcastSubscriptionsChanged(this, stateChangedArgs);

            m_subscriptions = App.mainViewModels.PodcastSubscriptions.ToList();
            refreshNextSubscription();
        }

        public void refreshSubscription(PodcastSubscriptionModel subscription)
        {
            m_subscriptions = new List<PodcastSubscriptionModel>();
            m_subscriptions.Add(subscription);
            refreshNextSubscription();
        }

        public void importFromGpodderWithCredentials(NetworkCredential nc)
        {
            if (String.IsNullOrEmpty(nc.Password) || String.IsNullOrEmpty(nc.UserName))
            {
                Debug.WriteLine("gPodder username or password empty.");
                
                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "Please give both gPodder username and password.";
                OnExternalServiceImportFinishedWithError(this, args);
                
                return;
            }

            OnExternalServiceImportStarted(this, null);

            Uri gpodderImportUri = new Uri(string.Format("http://gpodder.net/subscriptions/{0}.xml", nc.UserName));
            WebClient wc = new WebClient();
            wc.Credentials = nc;
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_GpodderImportCompleted);
            wc.DownloadStringAsync(gpodderImportUri);

            Debug.WriteLine("Importing from gPodder for user, " + nc.UserName + ", URL: " + gpodderImportUri.ToString());
        }

        async public void exportSubscriptions()
        {
            List<PodcastSubscriptionModel> subscriptions = App.mainViewModels.PodcastSubscriptions.ToList();
            if (subscriptions.Count == 0)
            {
                MessageBox.Show("No subscriptions to export.");
                return;
            }


            using (var db = new PodcastSqlModel())
            {
                if (db.settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportToSkyDrive)
                {
                    if (liveConnect == null)
                    {
                        await loginUserToSkyDrive();
                    }
                }

            }

            DoOPMLExport();
        }

        public void newDownloadedEpisode(PodcastEpisodeModel e)
        {
            if (NewPlayableEpisode != null)
            {
                NewPlayableEpisode(e);
            }
        }

        public void removedPlayableEpisode(PodcastEpisodeModel e)
        {
            if (RemovedPlayableEpisode != null)
            {
                RemovedPlayableEpisode(e);
            }
        }

        public void podcastPlaystateChanged(PodcastSubscriptionModel s)
        {
            if (OnPodcastChannelPlayedCountChanged != null) 
            {
                OnPodcastChannelPlayedCountChanged(s);
            }
        }

        public void podcastSubscriptionRemoved(PodcastSubscriptionModel s)
        {
            if (OnPodcastChannelRemoved != null)
            {
                OnPodcastChannelRemoved(s);
            }
        }

        public static string sanitizeFilename(string podcastString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in podcastString.ToArray())
            {
                if (Char.IsLetterOrDigit(c) || c == '.')
                {
                    sb.Append(c);
                }
            }

            podcastString = null;
            return sb.ToString();
        }

        public void cleanListenedEpisodes(PodcastSubscriptionModel podcastSubscriptionModel)
        {
            using (var db = new PodcastSqlModel())
            {
                float listenedEpisodeThreshold = 0.0F;
                listenedEpisodeThreshold = (float)db.settings().ListenedThreashold / (float)100.0;

                var queryDelEpisodes = db.Episodes.Where(episode => episode.PodcastId == podcastSubscriptionModel.PodcastId).AsEnumerable()
                                                  .Where(ep => (ep.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Listened
                                                                || (ep.EpisodeFile != ""
                                                                    && ((ep.TotalLengthTicks > 0 && ep.SavedPlayPos > 0)
                                                                    && ((float)((float)ep.SavedPlayPos / (float)ep.TotalLengthTicks) > listenedEpisodeThreshold))))
                                                                ).AsEnumerable();

                foreach (var episode in queryDelEpisodes)
                {
                    episode.deleteDownloadedEpisode();
                }
            }
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations
        private static PodcastSubscriptionsManager m_subscriptionManagerInstance = null;
        private Random m_random                               = null;
        private List<PodcastSubscriptionModel> m_subscriptions = null;
        int m_activeExternalImportsCount                      = 0;
        private LiveConnectClient liveConnect                 = null;

        private PodcastSubscriptionsManager()
        {
            m_random = new Random();

            // Hook a callback method to the signal that we emit when the subscription has been added.
            // This way we can continue the synchronous execution.
            this.OnPodcastChannelAddFinished += new SubscriptionManagerHandler(PodcastSubscriptionsManager_OnPodcastAddedFinished);
        }


        private Uri createNonCachedRefreshUri(string refreshUri)
        {
            string delimitter = "&";
            if (refreshUri.Contains("?") == false)
            {
                delimitter = "?";
            }

            return new Uri(string.Format("{0}{1}nocache={2}", refreshUri,
                                                              delimitter,
                                                              Environment.TickCount));
        }

        async private void wc_DownloadPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null
                || e.Cancelled)
            {
                try
                {
                    string foo = e.Result;
                }
                catch (WebException ex)
                {
                    if (needsAuthentication(ex))
                    {
                        if (MessageBox.Show("Subscribing to this podcast requires authentication. Do you want to give username and password to continue?",
                             "Attention",
                             MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            SubscriptionManagerArgs authArgs = new SubscriptionManagerArgs();
                            authArgs.podcastFeedRSSUri = ex.Response.ResponseUri;
                            OnPodcastChannelRequiresAuthentication(this, authArgs);
                            return;
                        }
                    }
                }

                PodcastSubscriptionFailedWithMessage("Could not fetch the podcast feed.");
                return;
            }

            string podcastRss = e.Result;
            PodcastSubscriptionModel podcastModel = PodcastFactory.podcastModelFromRSS(podcastRss);
            if (podcastModel == null)
            {
                PodcastSubscriptionFailedWithMessage("Podcast feed is invalid.");
                return;
            }

            AddSubscriptionOptions options = e.UserState as AddSubscriptionOptions;
            string rssUrl = options.rssUrl;
            bool importingFromExternalService = options.isImportingFromExternalService;
            bool isPodcastInDB = false;

            using (var db = new PodcastSqlModel())
            {
                isPodcastInDB = db.isPodcastInDB(rssUrl);
            }

            if (isPodcastInDB)
            {
                if (!importingFromExternalService)
                {
                    PodcastSubscriptionFailedWithMessage("You have already subscribed to that podcast.");
                }

                if (importingFromExternalService)                    
                {
                    m_activeExternalImportsCount--;
                    if (m_activeExternalImportsCount <= 0)
                    {
                        if (OnExternalServiceImportFinished != null)
                        {
                            OnExternalServiceImportFinished(this, null);
                        }
                    }
                }
                return;
            }

            podcastModel.CachedPodcastRSSFeed = podcastRss;                        
            podcastModel.PodcastLogoLocalLocation = localLogoFileName(podcastModel);
            podcastModel.PodcastRSSUrl = rssUrl;

            if (String.IsNullOrEmpty(options.username) == false)
            {
                podcastModel.Username = options.username;
            }

            if (String.IsNullOrEmpty(options.password) == false)
            {
                podcastModel.Password = options.password;
            }

            using (var db = new PodcastSqlModel())
            {
                db.addSubscription(podcastModel);
            }

            await podcastModel.fetchChannelLogo();

            SubscriptionManagerArgs addArgs = new SubscriptionManagerArgs();
            addArgs.subscription = podcastModel;
            addArgs.isImportingFromExternalService = importingFromExternalService;

            OnPodcastChannelAddFinished(this, addArgs);
            OnPodcastChannelAdded(podcastModel);
        }

        private bool needsAuthentication(WebException e)
        {
            if (e.Response.Headers.ToString().ToLower().Contains("WWW-Authenticate".ToLower()))
            {
                return true;
            }

            return false;
        }

        private void refreshNextSubscription()
        {
            SubscriptionManagerArgs stateChangedArgs = new SubscriptionManagerArgs();

            if (m_subscriptions.Count < 1)
            {
                if (OnPodcastSubscriptionsChanged == null)
                {
                    return;
                }
                
                Debug.WriteLine("No more episodes to refresh. Done.");
                stateChangedArgs.state = PodcastSubscriptionsManager.SubscriptionsState.FinishedRefreshing;

                // Update all episodes to the latest list.
                App.mainViewModels.LatestEpisodesListProperty = new ObservableCollection<PodcastEpisodeModel>();
                
                OnPodcastSubscriptionsChanged(this, stateChangedArgs);                
                return;
            }

            PodcastSubscriptionModel subscription = m_subscriptions[0];
            m_subscriptions.RemoveAt(0);

            if (subscription.IsSubscribed == false)
            {
                Debug.WriteLine("Not subscribed to {0}, no refresh.", subscription.PodcastName);
                refreshNextSubscription();
            }

            Uri refreshUri = new Uri(subscription.PodcastRSSUrl, UriKind.Absolute);
            Debug.WriteLine("Refreshing subscriptions for '{0}', using URL: {1}", subscription.PodcastName, refreshUri);

            NetworkCredential nc = null;
            if (String.IsNullOrEmpty(subscription.Username) == false)
            { 
                nc = new NetworkCredential();
                nc.UserName = subscription.Username;
                Debug.WriteLine("Using username to refresh subscription: {0}", nc.UserName);
            }

            if (String.IsNullOrEmpty(subscription.Password) == false)
            {
                nc.Password = subscription.Password;
                Debug.WriteLine("User password to refresh subscription.");
            }

            WebClient wc = new WebClient();
            if (nc != null)
            {
                wc.Credentials = nc;
            }
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_RefreshPodcastRSSCompleted);
            wc.DownloadStringAsync(refreshUri, subscription);

            stateChangedArgs.state = SubscriptionsState.RefreshingSubscription;
            stateChangedArgs.subscription = subscription;
            OnPodcastSubscriptionsChanged(this, stateChangedArgs);                            
        }

        async void wc_RefreshPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            PodcastSubscriptionModel subscription = e.UserState as PodcastSubscriptionModel;
            
            if (e.Error != null)
            {
                Debug.WriteLine("ERROR: Got web error when refreshing subscriptions: " + e.ToString());
                ToastPrompt toast = new ToastPrompt();
                toast.Title = "Error";
                toast.Message = "Cannot refresh subscription '" + subscription.PodcastName + "'";

                toast.Show();

                refreshNextSubscription();
                return;
            }

            subscription.CachedPodcastRSSFeed = e.Result as string;
            subscription.SubscriptionStatus = "";

            Debug.WriteLine("Starting refreshing episodes for " + subscription.PodcastName);
            await Task.Run(() => subscription.EpisodesManager.updatePodcastEpisodes());
            Debug.WriteLine("Done.");

            // Ugly.
            if (App.forceReloadOfEpisodeData)
            {
                subscription.reloadUnplayedPlayedEpisodes();
                subscription.reloadPartiallyPlayedEpisodes();
            }

            refreshNextSubscription();
            
        }

        private void PodcastSubscriptionsManager_OnPodcastAddedFinished(object source, SubscriptionManagerArgs e)
        {
            PodcastSubscriptionModel subscriptionModel = e.subscription;
            Debug.WriteLine("Podcast added successfully. Name: " + subscriptionModel.PodcastName);

            subscriptionModel.EpisodesManager.updatePodcastEpisodes();
            if (e.isImportingFromExternalService)
            {
                m_activeExternalImportsCount--;
                if (m_activeExternalImportsCount <= 0)
                {
                    if (OnExternalServiceImportFinished != null)
                    {
                        OnExternalServiceImportFinished(this, null);
                    }
                }
            }
        }

        private void PodcastSubscriptionFailedWithMessage(string message)
        {
            Debug.WriteLine(message);
            SubscriptionManagerArgs args = new SubscriptionManagerArgs();
            args.message = message;

            OnPodcastChannelAddFinishedWithError(this, args);
        }

        private string localLogoFileName(PodcastSubscriptionModel podcastModel)
        {
            string podcastLogoFilename;
            if (podcastModel.PodcastLogoUrl == null 
                || String.IsNullOrEmpty(podcastModel.PodcastLogoUrl.ToString()))
            {
                return "";
            }
            else
            {
                // Parse the filename of the logo from the remote URL.
                string localPath = podcastModel.PodcastLogoUrl.LocalPath;
                podcastLogoFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);
                podcastLogoFilename = sanitizeFilename(podcastLogoFilename);
            }

            // Make podcast logo name random.
            // This is because, if for some reason, two podcasts have the same logo name and we delete
            // one of them, we don't want the other one to be affected. Just to be sure. 
            StringBuilder podcastLogoFilename_sb = new StringBuilder(podcastLogoFilename);
            podcastLogoFilename_sb.Insert(0, m_random.Next().ToString());

            string localPodcastLogoFilename = App.PODCAST_ICON_DIR + @"/" + podcastLogoFilename_sb.ToString();
            Debug.WriteLine("Found icon filename: " + localPodcastLogoFilename);

            return localPodcastLogoFilename;
        }

        private void wc_GpodderImportCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine("Error from gPodder when importing: " + e.Error + ", " + e.Error.ToString());

                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "Error importing from gPodder. Please try again.";
                OnExternalServiceImportFinishedWithError(this, args);
                return;
            }

            string xmlResponse = e.Result.ToString();
            List<Uri> subscriptions = PodcastFactory.podcastUrlFromGpodderImport(xmlResponse);
            if (subscriptions == null || subscriptions.Count == 0)
            {
                Debug.WriteLine("Got no subscriptions from gPodder.net.");

                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "No subscriptions could be imported.";
                OnExternalServiceImportFinishedWithError(this, args);
                return;
            }

            MessageBox.Show("A blank screen may appear for a longer period of time. Please wait until the import has completed and do not exit the app.");

            foreach(Uri subscription in subscriptions) 
            {
                addSubscriptionFromExterrnalService(subscription.ToString());
            }
        }

        private void addSubscriptionFromExterrnalService(string podcastRss)
        {
            addSubscriptionFromURL(podcastRss, true);
        }

        public void addSubscriptionFromOPMLFile(string opmlFileUrl)
        {
            Uri uri = null;

            bool valid = Uri.TryCreate(opmlFileUrl, System.UriKind.Absolute, out uri);
            if (!valid)
            {
                App.showErrorToast("Cannot fetch OPML from that location.");
                return;
            }

            MessageBox.Show("A blank screen may appear for a longer period of time. Please wait until the import has completed and do not exit the app.");

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadOPMLCompleted);
            wc.DownloadStringAsync(uri);

            if (OnPodcastChannelAddStarted != null)
            {
                OnPodcastChannelAddStarted(this, null);
            }
        }

        void wc_DownloadOPMLCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                App.showErrorToast("Cannot fetch OPML from that location.");
                
                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "Cannot fetch OPML from that location.";
                OnPodcastChannelAddFinishedWithError(this, args);
                return;
            }

            String opml = e.Result as String;
            List<Uri> subscriptions = PodcastFactory.podcastUrlFromOPMLImport(opml);
            if (subscriptions == null
                || subscriptions.Count < 1)
            {
                App.showNotificationToast("No subscriptions to import.");

                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "No podcasts to import.";
                OnPodcastChannelAddFinishedWithError(this, args);
                return;
            }

            foreach (Uri subscription in subscriptions)
            {
                addSubscriptionFromURL(subscription.ToString(), true);
            }
        }

        async private Task loginUserToSkyDrive()
        {
            LiveAuthClient auth = new LiveAuthClient(App.LSKEY_LIVE_CLIENT_ID);
            LiveLoginResult loginResult = await auth.LoginAsync(new string[] { "wl.signin", "wl.skydrive_update" });
            if (loginResult.Status != LiveConnectSessionStatus.Connected)
            {
                Debug.WriteLine("Could not finish SkyDrive login.");
                MessageBox.Show("Sorry. Could not log in to SkyDrive. Please try again.");
            }
            else
            {
                liveConnect = new LiveConnectClient(auth.Session);
            }
        }

        async private Task<bool> userIsLoggedToSkyDrive()
        {
            if (liveConnect == null)
            {
                return false;
            }

            var auth = new LiveAuthClient(App.LSKEY_LIVE_CLIENT_ID);
            var result = await auth.InitializeAsync(new[] { "wl.signin", "wl.skydrive_update" });
            if (result.Status == LiveConnectSessionStatus.NotConnected)
            {
                return false;
            }

            return true;
        }

        private void DoOPMLExport()
        {
            String dateCreated = DateTime.Now.ToString("r");
            String opmlExportFileName = String.Format("PodcatcherSubscriptions_{0}.opml.xml", DateTime.Now.ToString("dd_MM_yyyy"));

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(opmlExportFileName, FileMode.Create, myIsolatedStorage);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                using (XmlWriter writer = XmlWriter.Create(isoStream, settings))
                {
                    /** OPML root */
                    writer.WriteStartElement("opml");
                    writer.WriteAttributeString("version", "2.0");

                    /** Head */
                    writer.WriteStartElement("head");
                    // title
                    writer.WriteStartElement("title");
                    writer.WriteString("My subscriptions from Podcatcher");
                    writer.WriteEndElement();
                    // dateCreated
                    writer.WriteStartElement("dateCreated");
                    writer.WriteString(dateCreated);
                    writer.WriteEndElement();
                    /** End Head */
                    writer.WriteEndElement();  

                    /** Body */
                    writer.WriteStartElement("body");
                    // Each outline
                    List<PodcastSubscriptionModel> subscriptions = App.mainViewModels.PodcastSubscriptions.ToList();
                    foreach (PodcastSubscriptionModel s in subscriptions)
                    {
                        writer.WriteStartElement("outline");
                        writer.WriteAttributeString("title", s.PodcastName);
                        writer.WriteAttributeString("xmlUrl", s.PodcastRSSUrl);
                        writer.WriteAttributeString("type", "rss");
                        writer.WriteAttributeString("text", s.PodcastDescription);
                        writer.WriteEndElement();
                    }
                    /** End Body */
                    writer.WriteEndElement();

                    // Finish the document
                    writer.WriteEndDocument();
                    writer.Flush();
                }

                isoStream.Seek(0, SeekOrigin.Begin);

                using (var db = new PodcastSqlModel())
                {
                    if (db.settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportToSkyDrive)
                    {
                        SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                        args.state = SubscriptionsState.StartedSkydriveExport;
                        OnOPMLExportToSkydriveChanged(this, args);

                        exportToSkyDrive(opmlExportFileName, isoStream);
                    }
                    else if (db.settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportViaEmail)
                    {
                        exportViaEmail(isoStream);
                    }
                }
            }
        }

        async private void exportToSkyDrive(String opmlExportFileName, IsolatedStorageFileStream sourceStream)
        {
            try
            {
                await liveConnect.UploadAsync("me/skydrive", opmlExportFileName, sourceStream, OverwriteOption.Overwrite);
                MessageBox.Show("Exported to SkyDrive succesfully!");

                SubscriptionManagerArgs managerArgs = new SubscriptionManagerArgs();
                managerArgs.state = SubscriptionsState.FinishedSkydriveExport;
                OnOPMLExportToSkydriveChanged(this, managerArgs);
            }
            catch (Exception e)
            {
                MessageBox.Show("There was an error uploading to SkyDrive. Please try again.");
            }
        }

        private void exportViaEmail(IsolatedStorageFileStream isoStream)
        {
            StreamReader reader = new StreamReader(isoStream);
            String emailBody = reader.ReadToEnd();

            EmailComposeTask emailTask = new EmailComposeTask();
            emailTask.Subject = "Podcast subscriptions from Podcatcher.";
            emailTask.Body = emailBody;
            emailTask.Show();
        }

        async internal void importSubscriptionsFromSkyDrive()
        {
            if (MessageBox.Show("Podcatcher will try to find and import the latest podcasts that were exported from Podcatcher. Please login to SkyDrive to continue.",
                                "Import from SkyDrive",
                                MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            {
                return;
            }

            try
            {
                if (await userIsLoggedToSkyDrive() == false)
                {
                    await loginUserToSkyDrive();
                }
            }
            catch (LiveAuthException e)
            {
                Debug.WriteLine("User did not log in or got some other Live exception. Cannot import subscriptions.");
                MessageBox.Show("Authentication to SkyDrive was not successful. Thus, cannot import your subscriptions. Please try again.");
                return;
            }
            catch (LiveConnectException e)
            {
                Debug.WriteLine("Error communicating to SkyDrive. Cannot import subscriptions.");
                MessageBox.Show("Error communicating to SkyDrive. Thus, cannot import your subscriptions. Please try again.");
                return;
            }

            LiveOperationResult result = await liveConnect.GetAsync("me/skydrive/files");
            if (result.Result.ContainsKey("data"))
            {
                JObject json = JObject.Parse(result.RawResult);
                var podcatcherSubscriptions = from s in json["data"].Values<JObject>()
                                              where ((String)s["name"]).Contains("PodcatcherSubscriptions")
                                              select new
                                              {
                                                Id = (String)s["id"],
                                                Created = DateTime.Parse((String)s["updated_time"]),
                                                Source = (String)s["source"]
                                              };

                foreach(var item in podcatcherSubscriptions) 
                {
                    Debug.WriteLine("id: {0}, created {1}", item.Id, item.Created);
                }

                var subscription = podcatcherSubscriptions.OrderByDescending(sub => sub.Created).FirstOrDefault();
                Debug.WriteLine("Going to get file {0} from {1}: ", subscription.Id, subscription.Source);

                if (subscription != null)
                {
                    PodcastSubscriptionsManager.getInstance().addSubscriptionFromOPMLFile(subscription.Source);
                }
                else
                {
                    MessageBox.Show("No subscriptions exported by Podcatcher could be found from SkyDrive.");
                }
            }
        }

        #endregion
    }
}
