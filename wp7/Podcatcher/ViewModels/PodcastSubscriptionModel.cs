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
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using Podcatcher.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace Podcatcher.ViewModels
{
    [Table]
    public class PodcastSubscriptionModel : INotifyPropertyChanged
    {
        private enum SubscriptionSettingDeleteEpisodes
        {
            Unset,
            True,
            False
        }

        /************************************* Public properties *******************************/
        #region properties
        private int m_podcastId;
        [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
        public int PodcastId 
        {
            get { return m_podcastId; }
            set { m_podcastId = value; }
        }

        private string m_PodcastName;
        [Column]
        public string PodcastName
        {
            get
            {
                return m_PodcastName;
            }
            
            set
            {
                if (value != m_PodcastName)
                {
                    m_PodcastName = value;
                    NotifyPropertyChanged("PodcastName");
                }
            }
        }

        private string m_PodcastDescription;
        [Column]
        public string PodcastDescription
        {
            get
            {
                return m_PodcastDescription;
            }

            set
            {
                if (value != m_PodcastDescription)
                {
                    m_PodcastDescription = value;
                    NotifyPropertyChanged("PodcastDescription");
                }
            }
        }

        private string m_PodcastLogoLocalLocation;
        [Column]
        public string PodcastLogoLocalLocation
        {
            get
            {
                return m_PodcastLogoLocalLocation;
            }

            set
            {
                m_PodcastLogoLocalLocation = value;
                NotifyPropertyChanged("PodcastLogoLocalLocation");
            }
        }

        private Uri m_PodcastLogoUrl; // = new Uri("Images/Podcatcher_generic_podcast_cover.png", UriKind.Relative);
        public Uri PodcastLogoUrl
        {
            get
            {
                return m_PodcastLogoUrl;
            }

            set
            {
                if (value != m_PodcastLogoUrl)
                {
                    m_PodcastLogoUrl = value;
                }
            }
        }

        public BitmapImage PodcastLogo
        {
            get
            {
                BitmapImage logoImage = null;
                string isoFilename;

                if (String.IsNullOrEmpty(m_PodcastLogoLocalLocation))
                {
                    isoFilename = "/images/Podcatcher_generic_podcast_cover.png";
                }
                else
                {
                    isoFilename = m_PodcastLogoLocalLocation;
                }


                // This method can be called when we still don't have a local
                // image stored, so the file name can be empty.

                // When we request for the podcast logo we will in fact fetch
                // the image from the local cache, create the BitmapImage object
                // and return that. 
                try
                {
                    IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();
                    if (isoStore.FileExists(isoFilename) != false)
                    {
                        logoImage = new BitmapImage();
                        using (var stream = isoStore.OpenFile(isoFilename, System.IO.FileMode.OpenOrCreate))
                        {
                            //logoImage = createMemorySafeThumbnail(stream, 150); // Thumbnail is 150 pixel in width
                            logoImage.SetSource(stream);
                        }
                    }
                }
                catch (IsolatedStorageException isoEx)
                {
                    Debug.WriteLine("Issue opening isolated storage! Error: " + isoEx.Message);
                }

                if (logoImage == null)
                {
                    try
                    {
                        Uri uri = new Uri("/images/Podcatcher_generic_podcast_cover.png", UriKind.Relative);
                        logoImage = new BitmapImage();
                        logoImage.UriSource = uri;
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("ERROR: Cannot set default podcast cover.");
                    }
                }

                return logoImage;
            }

            private set { }
        }

        private Image createMemorySafeThumbnail(IsolatedStorageFileStream stream, int width)
        {
            BitmapImage bi = new BitmapImage();
            bi.SetSource(stream);

            double cx = width;
            double cy = bi.PixelHeight * (cx / bi.PixelWidth);

            Image image = new Image();
            image.Source = bi;

            WriteableBitmap wb1 = new WriteableBitmap((int)cx, (int)cy);
            ScaleTransform transform = new ScaleTransform();
            transform.ScaleX = cx / bi.PixelWidth;
            transform.ScaleY = cy / bi.PixelHeight;
            wb1.Render(image, transform);
            wb1.Invalidate();

            WriteableBitmap wb2 = new WriteableBitmap((int)cx, (int)cy);
            for (int i = 0; i < wb2.Pixels.Length; i++)
                wb2.Pixels[i] = wb1.Pixels[i];
            wb2.Invalidate();

            Image thumbnail = new Image();
            thumbnail.Width = cx;
            thumbnail.Height = cy;

            thumbnail.Source = wb2;

            return thumbnail;
        }

        [Column]
        public String PodcastRSSUrl
        {
            get;
            set;
        }

        [Column]
        public String PodcastShowLink
        {
            get;
            set;
        }

        [Column]
        private int m_newEpisodesCount;
        public int NewEpisodesCount
        {
            get
            {
                if (m_newEpisodesCount == 0)
                {
                    return 0;
                }

                return m_newEpisodesCount;
            }

            set
            {
                if (m_newEpisodesCount != value)
                {
                    m_newEpisodesCount = value;
                    NotifyPropertyChanged("NewEpisodesText");
                }
            }
        }

        [Column]
        private bool m_isSubscribed = true;
        public Boolean IsSubscribed
        {
            get
            {
                return m_isSubscribed;
            }

            set
            {
                m_isSubscribed = value;
            }
        }

        [Column]
        private bool m_isAutoDL = false;
        public Boolean IsAutoDownload
        {
            get
            {
                return m_isAutoDL;
            }

            set
            {
                m_isAutoDL = value;
            }
        }

        private string m_username;
        [Column]
        public String Username
        {
            get
            {
                return m_username;
            }

            set
            {
                m_username = value;
            }
        }

        private string m_password;
        [Column]
        public String Password
        {
            get
            {
                return m_password;
            }

            set
            {
                m_password = value;
            }
        }


        private int m_episodesCount = 0;
        public String EpisodesText
        {
            get
            {
                using (var db = new PodcastSqlModel())
                {
                    m_episodesCount = db.episodesForSubscription(this).Count;
                }

                return String.Format("{0} episodes", m_episodesCount);
            }

            set
            {
                NotifyPropertyChanged("NumberOfEpisodesText");
                NotifyPropertyChanged("EpisodesText");
            }

        }

        private EntitySet<PodcastEpisodeModel> m_podcastEpisodes = new EntitySet<PodcastEpisodeModel>();
        [Association(Storage = "m_podcastEpisodes", ThisKey = "PodcastId", OtherKey = "PodcastId")]
        public EntitySet<PodcastEpisodeModel> Episodes
        {
            get { return m_podcastEpisodes; }

            set {
                NotifyPropertyChanging();
                m_podcastEpisodes.Assign(value);
                NotifyPropertyChanged("Episodes");
            }
        }

        private int m_SelectedKeepNumEpisodesIndex = 0;
        [Column(DbType = "SMALLINT DEFAULT 0 NOT NULL")]
        public int SubscriptionSelectedKeepNumEpisodesIndex
        {
            get
            {
                    return m_SelectedKeepNumEpisodesIndex;
            }

            set
            {
                if (m_SelectedKeepNumEpisodesIndex != value)
                {
                    m_SelectedKeepNumEpisodesIndex = value;
                }
            }
        }

        private bool m_isContinuousPlayback = false;
        [Column(DbType = "BIT DEFAULT 0 NOT NULL")]
        public bool IsContinuousPlayback
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        private int m_IsDeleteEpisodes = (int)SubscriptionSettingDeleteEpisodes.Unset;
        [Column(DbType = "BIT DEFAULT 0 NOT NULL")]
        public bool SubscriptionIsDeleteEpisodes
        {
            get
            {
                switch (m_IsDeleteEpisodes)
                {
                    case (int)SubscriptionSettingDeleteEpisodes.True:
                        return true;
                    case (int)SubscriptionSettingDeleteEpisodes.False:
                        return false;
                    default:
                        return false;
                }
            }

            set
            {
                if (value == true)
                {
                    m_IsDeleteEpisodes = (int)SubscriptionSettingDeleteEpisodes.True;
                }
                else
                {
                    m_IsDeleteEpisodes = (int)SubscriptionSettingDeleteEpisodes.False;
                }
            }
        }

        private ObservableCollection<PodcastEpisodeModel> m_episodes = null;
        public ObservableCollection<PodcastEpisodeModel> EpisodesPublishedDescending
        {
            get
            {
                if (m_episodes == null)
                {
                    using (var db = new PodcastSqlModel())
                    {
                        var query = from PodcastEpisodeModel episode in db.Episodes
                                    where episode.PodcastId == PodcastId
                                    orderby episode.EpisodePublished descending
                                    select episode;

                        m_episodes = new ObservableCollection<PodcastEpisodeModel>(query);

                        for (int i = 0; i < m_episodes.Count && i < NewEpisodesCount; i++)
                        {
                            m_episodes[i].NewEpisodeVisibility = Visibility.Visible;
                        }

                        if (App.CurrentlyPlayingEpisode != null)
                        {
                            PodcastEpisodeModel playingEpisode = db.Episodes.Where(ep => ep.EpisodeId == App.CurrentlyPlayingEpisode.EpisodeId).FirstOrDefault();
                            if (playingEpisode != null)
                            {
                                playingEpisode.initializeState(App.CurrentlyPlayingEpisode);
                            }
                        }
                    }
                }

                return m_episodes;
            }

            set
            {
                m_episodes = null;
                NotifyPropertyChanged("EpisodesPublishedDescending");
                NotifyPropertyChanged("EpisodesText");
            }
        }

        public string CachedPodcastRSSFeed
        {
            get;
            set;
        }

        public PodcastEpisodesManager EpisodesManager
        {
            get
            {
                return m_podcastEpisodesManager;
            }
        }

        private int m_unplayedEpisodes = -1;
        public int UnplayedEpisodes
        {
            get
            {
                if (m_unplayedEpisodes < 0)
                {
                    m_unplayedEpisodes = unplayedEpisodesCount(PodcastId);
                }

                return m_unplayedEpisodes;
            }

            set
            {
                m_unplayedEpisodes = unplayedEpisodesCount(PodcastId);
                NotifyPropertyChanged("NumberOfEpisodesText");
            }
        }

        private int m_partiallyPlayedEpisodes = -1;
        public int PartiallyPlayedEpisodes
        {
            get
            {
                if (m_partiallyPlayedEpisodes < 0)
                {
                    m_partiallyPlayedEpisodes = partiallyPlayedEpisodesCount(PodcastId);
                }

                return m_partiallyPlayedEpisodes;
            }

            set
            {
                m_partiallyPlayedEpisodes = partiallyPlayedEpisodesCount(PodcastId);
                NotifyPropertyChanged("NumberOfEpisodesText");
            }
        }

        private static int unplayedEpisodesCount(int subscriptionId)
        {
            using (var db = new PodcastSqlModel())
            {
                try
                {
                    var query = from episode in db.Episodes
                                where (episode.PodcastId == subscriptionId
                                       && episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded
                                       && episode.SavedPlayPos == 0)
                                select episode;

                    int count = query.Count();
                    return count;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        private static int partiallyPlayedEpisodesCount(int subscriptionId)
        {
            float listenedEpisodeThreshold = 0.0F;
            using (var db = new PodcastSqlModel())
            {
                try
                {
                    listenedEpisodeThreshold = (float)db.settings().ListenedThreashold / (float)100.0;
                    var query = from episode in db.Episodes
                                where (episode.PodcastId == subscriptionId
                                       && episode.EpisodePlayState != PodcastEpisodeModel.EpisodePlayStateEnum.Listened
                                       && episode.SavedPlayPos > 0
                                       && ((float)((float)episode.SavedPlayPos / (float)episode.TotalLengthTicks) < listenedEpisodeThreshold))
                                select episode;

                    return query.Count();
                }
                catch (Exception)
                {
                    // Something happened.
                    return 0;
                }
            }

        }

        public String NumberOfEpisodesText
        {
            get
            {
                bool and    = (UnplayedEpisodes > 0 && PartiallyPlayedEpisodes > 0) ? true : false;

                return String.Format("{0}{1}{2}",
                    UnplayedEpisodes > 0 ? UnplayedEpisodes + " unplayed" : "",
                    and ? "\n" : "",
                    PartiallyPlayedEpisodes > 0 ? PartiallyPlayedEpisodes + " partially played" : "");
            }

            set
            {
                NotifyPropertyChanged("NumberOfEpisodesText");
            }

        }

        public String NewEpisodesText 
        {
            get
            {
                if (NewEpisodesCount > 0)
                {
                    return String.Format("{0} new", NewEpisodesCount);
                }
                else
                {
                    return "";
                }
            }
        }

        private ObservableCollection<PodcastEpisodeModel> m_playableEpisodes = null;
        public ObservableCollection<PodcastEpisodeModel> PlayableEpisodes
        {
            get
            {
                if (m_playableEpisodes == null)
                {
                    using (var db = new PodcastSqlModel())
                    {
                        m_playableEpisodes = new ObservableCollection<PodcastEpisodeModel>(db.playableEpisodesForSubscription(this));
                    }
                }
                return m_playableEpisodes;
            }

            set
            {
                if (value != m_playableEpisodes)
                {
                    m_playableEpisodes = value;
                }

                NotifyPropertyChanged("PlayableEpisodes");
            }
        }

        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary version;

        #endregion

        /************************************* Public implementations *******************************/

        public event SubscriptionModelHandler PodcastCleanStarted;
        public event SubscriptionModelHandler PodcastCleanFinished;
        public delegate void SubscriptionModelHandler();

        public PodcastSubscriptionModel()
        {
            m_podcastEpisodesManager = new PodcastEpisodesManager(this);
            m_isolatedFileStorage = IsolatedStorageFile.GetUserStoreForApplication();
            createLogoCacheDirs();
        }

        public void cleanupForDeletion()
        {
            ShellTile tile = getSubscriptionsLiveTile();
            if (tile != null)
            {
                Debug.WriteLine("Deleted subscription's live tile.");
                tile.Delete();
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(backgroundCleanupWork);
            worker.RunWorkerAsync();
        }

        void backgroundCleanupWork(object sender, DoWorkEventArgs e)
        {
            // TODO: Extract methods.

            // Delete logo from local image cache.
            if (m_isolatedFileStorage.FileExists(m_PodcastLogoLocalLocation) == false)
            {
                Debug.WriteLine("ERROR: Logo local cache file not found! Subscription: " + m_PodcastName
                                + ", logo file: " + m_PodcastLogoLocalLocation);
                return;
            }

            Debug.WriteLine("Deleting local cache of logo: " + m_PodcastLogoLocalLocation);
            m_isolatedFileStorage.DeleteFile(m_PodcastLogoLocalLocation);
        }

        public void fetchChannelLogo()
        {
            if (m_PodcastLogoUrl == null)
            {
                Debug.WriteLine("Logo is null. Using default cover.");
                return;
            }

            Debug.WriteLine("Getting podcast icon: " + m_PodcastLogoUrl);

            // Fetch the remote podcast logo to store locally in the IsolatedStorage.
            WebClient wc = new WebClient();
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_FetchPodcastLogoCompleted);
            wc.OpenReadAsync(m_PodcastLogoUrl);
        }

        public void addNumOfNewEpisodes(int newPodcastEpisodes)
        {
            NewEpisodesCount = m_newEpisodesCount + newPodcastEpisodes;
        }

        public ShellTile getSubscriptionsLiveTile()
        {
            return ShellTile.ActiveTiles.FirstOrDefault(tile => tile.NavigationUri.ToString().Contains("podcastId=" + m_podcastId)) as ShellTile;
        }

        public void cleanOldEpisodes(int keepEpisodes, bool deleteUnplayed = false)
        {
            List<String> episodeFiles = new List<String>();
            IEnumerable<PodcastEpisodeModel> query = null;
            bool deleteDownloads = SubscriptionIsDeleteEpisodes;
            int keepDownloads = 0;

            using (var db = new PodcastSqlModel())
            {
                PodcastSubscriptionModel e = db.subscriptionModelForIndex(PodcastId);

                if (!deleteDownloads)
                {
                    keepDownloads = (from episode in e.Episodes
                                     where (episode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
                                     select episode).ToList().Count;
                }                

                if (keepEpisodes + keepDownloads >= e.Episodes.Count)
                {
                    return;
                }

                PodcastCleanStarted();

                if (deleteDownloads)
                {
                    query = (from episode in e.Episodes
                             orderby episode.EpisodePublished descending
                             select episode).Skip(keepEpisodes);
                }
                else
                {
                    query = (from episode in e.Episodes
                             orderby episode.EpisodePublished descending
                             where (episode.EpisodeDownloadState != PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
                             select episode).Skip(keepEpisodes);
                }

                foreach (PodcastEpisodeModel de in query)
                {
                    if (!String.IsNullOrEmpty(de.EpisodeFile)) 
                    {
                        episodeFiles.Add(de.EpisodeFile);
                    }
                }

                db.deleteEpisodesPerQuery(query);
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(workerCleanSubscriptions);
            worker.RunWorkerAsync(episodeFiles);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerCleanSubscriptionsCompleted);
 
        }

        private void workerCleanSubscriptions(object sender, DoWorkEventArgs args)
        {
            List<String> filesToDelete = args.Argument as List<String>;
            using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (String filename in filesToDelete)
                {
                    try
                    {
                        Debug.WriteLine("Deleting downloaded episode: " + filename);
                        if (episodeStore.FileExists(filename))
                        {
                            episodeStore.DeleteFile(filename);
                        }
                        else
                        {
                            Debug.WriteLine("Warning: Cannot delete episode with file: " + filename);
                        }
                    }
                    catch (IsolatedStorageException e)
                    {
                        Debug.WriteLine("Error: Exception occured while deleting episodes: " + e.Message);
                    }
                }
            }

        }

        private void workerCleanSubscriptionsCompleted(object sender, RunWorkerCompletedEventArgs e) 
        {
            PodcastCleanFinished();
            NotifyPropertyChanged("EpisodesText");
            NotifyPropertyChanged("EpisodesPublishedDescending");
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations        
        private PodcastEpisodesManager  m_podcastEpisodesManager;
        private IsolatedStorageFile     m_isolatedFileStorage;

        private void createLogoCacheDirs()
        {
            if (m_isolatedFileStorage.DirectoryExists(App.PODCAST_ICON_DIR) == false)
            {
                Debug.WriteLine("Icon cache dir does not exist. Creating dir: " + App.PODCAST_ICON_DIR);
                m_isolatedFileStorage.CreateDirectory(App.PODCAST_ICON_DIR);
            }
        }

        private void wc_FetchPodcastLogoCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                Debug.WriteLine("ERROR: Cannot fetch channel logo.");
                PodcastLogoLocalLocation = null;
                return;
            }

            Debug.WriteLine("Storing podcast icon locally...");

            Stream logoInStream = null;
            try
            {
                logoInStream = (Stream)e.Result;
            }
            catch (WebException webEx)
            {
                if (webEx.Status != WebExceptionStatus.Success)
                {
                    Debug.WriteLine("ERROR: Web error occured. Cannot load image!");
                    return;
                }
            }

            // Store the downloaded podcast logo to isolated storage for local cache.
            MemoryStream logoMemory = new MemoryStream();
            logoInStream.CopyTo(logoMemory);
            using (var isoFileStream = new IsolatedStorageFileStream(m_PodcastLogoLocalLocation, 
                                                                     FileMode.OpenOrCreate, 
                                                                     m_isolatedFileStorage))
            {
                isoFileStream.Write(logoMemory.ToArray(), 0, (int)logoMemory.Length);
            }

            Debug.WriteLine("Stored local podcast icon as: " + PodcastLogoLocalLocation);
            
            // Local cache has been updated - notify the UI that the logo property has changed.
            // and the new logo can be fetched to the UI. 
            NotifyPropertyChanged("PodcastLogo");
            App.mainViewModels.PodcastSubscriptions = null;
        }

        #region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;
        
        private void NotifyPropertyChanging()
        {
            if ((this.PropertyChanging != null))
            {
                this.PropertyChanging(this, null);
            }
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #endregion

        #region PodcastEpisodesManager
        public class PodcastEpisodesManager
        {
            private PodcastSubscriptionModel m_subscriptionModel;

            public PodcastEpisodesManager(PodcastSubscriptionModel subscriptionModel)
            {
                m_subscriptionModel = subscriptionModel;
            }

            public void updatePodcastEpisodes()
            {
                Debug.WriteLine("Updating episodes for podcast: " + m_subscriptionModel.PodcastName);

                bool subscriptionAddedNow = true;

                List<PodcastEpisodeModel> episodes = null;
                using (var db = new PodcastSqlModel())
                {
                    episodes = db.episodesForSubscription(m_subscriptionModel);
                }

                DateTime latestEpisodePublishDate = new DateTime();

                if (episodes.Count > 0)
                {
                    // The episodes are in descending order as per publish date. 
                    // So take the first episode and we have the latest known publish date.
                    latestEpisodePublishDate = episodes[0].EpisodePublished;

                    // If we already have episodes, this subscription is not being added now.
                    subscriptionAddedNow = false;
                }

                episodes = null;

                Debug.WriteLine("\nStarting to parse episodes for podcast: " + m_subscriptionModel.PodcastName);
                List<PodcastEpisodeModel> newPodcastEpisodes = PodcastFactory.newPodcastEpisodes(m_subscriptionModel.CachedPodcastRSSFeed, latestEpisodePublishDate);
                m_subscriptionModel.CachedPodcastRSSFeed = "";

                if (newPodcastEpisodes == null)
                {
                    Debug.WriteLine("WARNING: Got null list of new episodes.");
                    return;
                }

                using (var db = new PodcastSqlModel())
                {
                    PodcastSubscriptionModel sub = db.Subscriptions.FirstOrDefault(s => s.PodcastId == m_subscriptionModel.PodcastId);
                    if (sub == null) 
                    {
                        Debug.WriteLine("Subscription NULL. Probably already deleted.");
                        return;
                    }

                    db.insertEpisodesForSubscription(m_subscriptionModel, newPodcastEpisodes);

                    // Indicate new episodes to the UI only when we are not adding the feed. 
                    // I.e. we want to show new episodes only when we refresh the feed at restart.
                    if (subscriptionAddedNow == false)
                    {
                        int numOfNewPodcasts = newPodcastEpisodes.Count;

                        Debug.WriteLine("Got {0} new episodes.", numOfNewPodcasts);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            m_subscriptionModel.addNumOfNewEpisodes(numOfNewPodcasts);
                        });

                        sub.addNumOfNewEpisodes(numOfNewPodcasts);
                        
                        db.SubmitChanges();
                    }
                }

                if (m_subscriptionModel.IsAutoDownload
                    && newPodcastEpisodes.Count > 0)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PodcastEpisodesDownloadManager.getInstance().addEpisodesToDownloadQueue(newPodcastEpisodes);
                    });
                }

                if (newPodcastEpisodes.Count > 0)
                {
                    // Update subscription's information if it's pinned to home screen.
                    updatePinnedInformation();

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        // This will call the setter for episode model in the UI that will notify the UI that the content has changed.
                        m_subscriptionModel.EpisodesPublishedDescending = new ObservableCollection<PodcastEpisodeModel>();
                    });
                }

            }

            public void updatePinnedInformation()
            {
                // Store information about the subscription for the background agent.
                IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                String subscriptionLatestEpisodeKey = App.LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + m_subscriptionModel.PodcastId;
                if (settings.Contains(subscriptionLatestEpisodeKey) == false)
                {
                    return;
                }
                else
                {
                    settings.Remove(subscriptionLatestEpisodeKey);
                }

                DateTime newestEpisodeTimestamp;
                using (var db = new PodcastSqlModel())
                {
                    newestEpisodeTimestamp = db.episodesForSubscription(m_subscriptionModel)[0].EpisodePublished;
                } 
                String subscriptionData = String.Format("{0}|{1}|{2}",
                                                        m_subscriptionModel.PodcastId,
                                                        newestEpisodeTimestamp.ToString("r"),
                                                        m_subscriptionModel.PodcastRSSUrl);
                lock (typeof(IsolatedStorageSettings)) {
                    settings.Add(subscriptionLatestEpisodeKey, subscriptionData);
                    settings.Save();
                }

                Debug.WriteLine("Storing latest episode publish date for subscription as: " + newestEpisodeTimestamp.ToString("r"));
            }
        }
        #endregion
    }
}