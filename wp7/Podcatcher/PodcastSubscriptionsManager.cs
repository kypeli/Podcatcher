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
using System.Collections.Generic;
using System.Xml;
using Microsoft.Phone.Tasks;
using Microsoft.Live;
using System.IO.IsolatedStorage;
using System.IO;

namespace Podcatcher
{
    public delegate void SubscriptionManagerHandler(object source, SubscriptionManagerArgs e);

    public class SubscriptionManagerArgs
    {
        public String message;
        public PodcastSubscriptionModel addedSubscription;
        public PodcastSubscriptionsManager.SubscriptionsState state;
        public bool isImportingFromGpodder = false;
        public Uri podcastFeedRSSUri;
    }

    internal class AddSubscriptionOptions
    {
        public String rssUrl = "";
        public bool isImportingFromGpodder = false;
        public String username = "";
        public String password = "";
    }

    public class PodcastSubscriptionsManager
    {
        /************************************* Public implementations *******************************/

        public event SubscriptionManagerHandler OnPodcastChannelAddStarted;
        public event SubscriptionManagerHandler OnPodcastChannelAddFinished;
        public event SubscriptionManagerHandler OnPodcastChannelAddFinishedWithError;
        public event SubscriptionManagerHandler OnPodcastChannelRequiresAuthentication;

        public event SubscriptionManagerHandler OnGPodderImportStarted;
        public event SubscriptionManagerHandler OnGPodderImportFinished;
        public event SubscriptionManagerHandler OnGPodderImportFinishedWithError;

        public event SubscriptionManagerHandler OnPodcastSubscriptionsChanged;

        public event SubscriptionManagerHandler OnOPMLExportToSkydriveChanged;

        public enum SubscriptionsState
        {
            StartedRefreshing,
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

        public void addSubscriptionFromURL(string podcastRss, bool importingFromGpodder = false)
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
                OnPodcastChannelAddFinishedWithError(this, null);
                return;
            }

            AddSubscriptionOptions options = new AddSubscriptionOptions();
            options.rssUrl = podcastRss;
            options.isImportingFromGpodder = importingFromGpodder;

            if (importingFromGpodder)
            {
                m_activeGPodderImportsCount++;
            }

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadPodcastRSSCompleted);
            wc.DownloadStringAsync(podcastRssUri, options);

            if (!importingFromGpodder)
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
                OnPodcastChannelAddFinishedWithError(this, null);
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

        public void deleteSubscription(PodcastSubscriptionModel podcastSubscriptionModel)
        {
            podcastSubscriptionModel.cleanupForDeletion();
            m_podcastsSqlModel.deleteSubscription(podcastSubscriptionModel);
        }

        public void refreshSubscriptions()
        {
            stateChangedArgs.state = PodcastSubscriptionsManager.SubscriptionsState.StartedRefreshing;
            OnPodcastSubscriptionsChanged(this, stateChangedArgs);

            m_subscriptions = m_podcastsSqlModel.PodcastSubscriptions;
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
                OnGPodderImportFinishedWithError(this, args);
                
                return;
            }

            OnGPodderImportStarted(this, null);

            Uri gpodderImportUri = new Uri(string.Format("http://gpodder.net/subscriptions/{0}.xml", nc.UserName));
            WebClient wc = new WebClient();
            wc.Credentials = nc;
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_GpodderImportCompleted);
            wc.DownloadStringAsync(gpodderImportUri);

            Debug.WriteLine("Importing from gPodder for user, " + nc.UserName + ", URL: " + gpodderImportUri.ToString());
        }

        public void exportSubscriptions()
        {
            List<PodcastSubscriptionModel> subscriptions = m_podcastsSqlModel.PodcastSubscriptions;
            if (subscriptions.Count == 0)
            {
                MessageBox.Show("No subscriptions to export.");
                return;
            }


            if (PodcastSqlModel.getInstance().settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportToSkyDrive)
            {
                if (liveConnect == null)
                {
                    loginUserToSkyDrive();
                }
                else
                {
                    DoOPMLExport();
                }
            }
            else if (PodcastSqlModel.getInstance().settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportViaEmail)
            {
                DoOPMLExport();
            }
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations
        private static PodcastSubscriptionsManager m_subscriptionManagerInstance = null;
        private PodcastSqlModel m_podcastsSqlModel            = null;
        private Random m_random                               = null;
        private SubscriptionManagerArgs stateChangedArgs      = new SubscriptionManagerArgs();
        private List<PodcastSubscriptionModel> m_subscriptions = null;
        int m_activeGPodderImportsCount                       = 0;
        private LiveConnectClient liveConnect                 = null;

        private PodcastSubscriptionsManager()
        {
            m_podcastsSqlModel = PodcastSqlModel.getInstance();
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

        private void wc_DownloadPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
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
            bool importingFromGpodder = options.isImportingFromGpodder;

            if (m_podcastsSqlModel.isPodcastInDB(rssUrl))
            {
                if (!importingFromGpodder)
                {
                    PodcastSubscriptionFailedWithMessage("You have already subscribed to that podcast.");
                }

                if (importingFromGpodder)                    
                {
                    m_activeGPodderImportsCount--;
                    if (m_activeGPodderImportsCount <= 0)
                    {
                        OnGPodderImportFinished(this, null);
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

            m_podcastsSqlModel.addSubscription(podcastModel);

            podcastModel.fetchChannelLogo();

            SubscriptionManagerArgs addArgs = new SubscriptionManagerArgs();
            addArgs.addedSubscription = podcastModel;
            addArgs.isImportingFromGpodder = importingFromGpodder;

            OnPodcastChannelAddFinished(this, addArgs);
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
            if (m_subscriptions.Count < 1)
            {
                if (OnPodcastSubscriptionsChanged == null)
                {
                    return;
                }
                
                Debug.WriteLine("No more episodes to refresh. Done.");
                stateChangedArgs.state = PodcastSubscriptionsManager.SubscriptionsState.FinishedRefreshing;
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

            Uri refreshUri = createNonCachedRefreshUri(subscription.PodcastRSSUrl);
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
        }

        void wc_RefreshPodcastRSSCompleted(object sender, DownloadStringCompletedEventArgs e)
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

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(workerUpdateEpisodes);
            worker.RunWorkerAsync(subscription);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerUpdateEpisodesCompleted);
        }

        private void workerUpdateEpisodes(object sender, DoWorkEventArgs args)
        {
            PodcastSubscriptionModel subscription = args.Argument as PodcastSubscriptionModel;
            Debug.WriteLine("Starting refreshing episodes for " + subscription.PodcastName);
            lock (subscription)
            {
                subscription.EpisodesManager.updatePodcastEpisodes();
            }

            Debug.WriteLine("Done.");
        }

        private void workerUpdateEpisodesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            refreshNextSubscription();
        }

        private void PodcastSubscriptionsManager_OnPodcastAddedFinished(object source, SubscriptionManagerArgs e)
        {
            PodcastSubscriptionModel subscriptionModel = e.addedSubscription;
            Debug.WriteLine("Podcast added successfully. Name: " + subscriptionModel.PodcastName);

            subscriptionModel.EpisodesManager.updatePodcastEpisodes();
            if (e.isImportingFromGpodder)
            {
                m_activeGPodderImportsCount--;
                if (m_activeGPodderImportsCount <= 0)
                {
                    OnGPodderImportFinished(this, null);
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
                // Podcast logo URL is empty - use default placeholder logo.
                podcastLogoFilename = @"Podcatcher_generic_podcast_cover.png";
            }
            else
            {
                // Parse the filename of the logo from the remote URL.
                string localPath = podcastModel.PodcastLogoUrl.LocalPath;
                podcastLogoFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);
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
                OnGPodderImportFinishedWithError(this, args);
                return;
            }

            string xmlResponse = e.Result.ToString();
            List<Uri> subscriptions = PodcastFactory.podcastUrlFromGpodderImport(xmlResponse);
            if (subscriptions == null || subscriptions.Count == 0)
            {
                Debug.WriteLine("Got no subscriptions from gPodder.net.");

                SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                args.message = "No subscriptions could be imported.";
                OnGPodderImportFinishedWithError(this, args);
                return;
            }

            MessageBox.Show("A blank screen may appear for a longer period of time. Please wait until the import has completed and do not exit the app.");

            foreach(Uri subscription in subscriptions) 
            {
                addSubscriptionFromGPodder(subscription.ToString());
            }
        }

        private void addSubscriptionFromGPodder(string podcastRss)
        {
            addSubscriptionFromURL(podcastRss, true);
        }

        private void loginUserToSkyDrive()
        {
            LiveAuthClient auth = new LiveAuthClient(App.LSKEY_LIVE_CLIENT_ID); 
            auth.LoginCompleted +=
                new EventHandler<LoginCompletedEventArgs>(greetUser_LoginCompleted);
            auth.LoginAsync(new string[] { "wl.skydrive_update" });
        }

        void greetUser_LoginCompleted(object sender, LoginCompletedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                liveConnect = new LiveConnectClient(e.Session);
                liveConnect.GetCompleted +=
                    new EventHandler<LiveOperationCompletedEventArgs>(greetUser_GetCompleted);
                liveConnect.GetAsync("me");
            }
            else
            {
                Debug.WriteLine("Could not finish SkyDrive login.");
                MessageBox.Show("Sorry. Could not log in to SkyDrive. Please try again.");
            }
        }

        void greetUser_GetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Debug.WriteLine("I am logged in to SkyDrive!");
                DoOPMLExport();
            }
            else
            {
                Debug.WriteLine("Error logging in to SkyDrive: " + e.Error);
                MessageBox.Show("Sorry. Could not log in to SkyDrive. Please try again.");
            }
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
                    List<PodcastSubscriptionModel> subscriptions = m_podcastsSqlModel.PodcastSubscriptions;
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

                if (PodcastSqlModel.getInstance().settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportToSkyDrive)
                {
                    SubscriptionManagerArgs args = new SubscriptionManagerArgs();
                    args.state = SubscriptionsState.StartedSkydriveExport;
                    OnOPMLExportToSkydriveChanged(this, args);

                    exportToSkyDrive(opmlExportFileName, isoStream);
                }
                else if (PodcastSqlModel.getInstance().settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportViaEmail)
                {
                    exportViaEmail(isoStream);
                }
            }
        }

        private void exportToSkyDrive(String opmlExportFileName, IsolatedStorageFileStream sourceStream)
        {
            liveConnect.UploadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(opmlLiveOperation_UploadCompleted);
            liveConnect.UploadAsync("me/skydrive", opmlExportFileName, sourceStream, OverwriteOption.Overwrite);
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

        private void opmlLiveOperation_UploadCompleted(object sender, LiveOperationCompletedEventArgs args)
        {
            SubscriptionManagerArgs managerArgs = new SubscriptionManagerArgs();
            managerArgs.state = SubscriptionsState.FinishedSkydriveExport;
            OnOPMLExportToSkydriveChanged(this, managerArgs);

            if (args.Error == null)
            {
                MessageBox.Show("Exported to SkyDrive succesfully!");

            }
            else
            {
                MessageBox.Show("There was an error uploading to SkyDrive. Please try again.");
            }
        }
        #endregion

    }
}
