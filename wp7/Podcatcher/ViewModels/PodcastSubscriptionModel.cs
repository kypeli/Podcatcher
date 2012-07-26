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

namespace Podcatcher.ViewModels
{
    [Table(Name="PodcastSubscription")]
    public class PodcastSubscriptionModel : INotifyPropertyChanged
    {        
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

        private Uri m_PodcastLogoUrl = null;
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
                if (m_PodcastLogoLocalLocation == null)
                {
                    return null;
                }

                // This method can be called when we still don't have a local
                // image stored, so the file name can be empty.
                string isoFilename = m_PodcastLogoLocalLocation;

                // When we request for the podcast logo we will in fact fetch
                // the image from the local cache, create the BitmapImage object
                // and return that. 
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();
                if (isoStore.FileExists(isoFilename) == false)
                {
                    return null;
                }

                BitmapImage image = new BitmapImage();
                using (var stream = isoStore.OpenFile(isoFilename, System.IO.FileMode.OpenOrCreate))
                {
                    image.SetSource(stream);
                }
                return image;
            }

            private set { }
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

        private int m_newEpisodesCount;
        public String NewEpisodesCount {
            get
            {
                if (m_newEpisodesCount == 0)
                {
                    return "";
                }

                return m_newEpisodesCount.ToString();
            } 

            set 
            {
                if (m_newEpisodesCount.ToString() != value)
                {
                    m_newEpisodesCount = int.Parse(value);
                    NotifyPropertyChanged("NewEpisodesCount");
                }
            } 
        }

        public int UnplayedEpisodes
        {
            get
            {
                var query = from episode in Episodes
                            where (episode.EpisodeState == PodcastEpisodeModel.EpisodeStateEnum.Playable
                                   && episode.SavedPlayPos == 0)
                            select episode;

                return query.Count();
            }

            set
            {
                NotifyPropertyChanged("UnplayedEpisodes");
            }
        }

        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary version;

        #endregion

        /************************************* Public implementations *******************************/
        
        public PodcastSubscriptionModel()
        {
            m_podcastEpisodesManager = new PodcastEpisodesManager(this);

            m_localPodcastIconCache = IsolatedStorageFile.GetUserStoreForApplication();
            m_localPodcastIconCache.CreateDirectory(App.PODCAST_ICON_DIR);
        }

        public void cleanupForDeletion()
        {
            // TODO: Extract methods.
                        
            // Delete logo from local image cache.
            if (m_localPodcastIconCache.FileExists(m_PodcastLogoLocalLocation) == false) 
            {
                Debug.WriteLine("ERROR: Logo local cache file not found! Subscription: " + m_PodcastName 
                                + ", logo file: " + m_PodcastLogoLocalLocation);
                return;
            }

            Debug.WriteLine("Deleting local cache of logo: " + m_PodcastLogoLocalLocation);
            m_localPodcastIconCache.DeleteFile(m_PodcastLogoLocalLocation);
        }

        public void fetchChannelLogo()
        {
            Debug.WriteLine("Getting podcast icon: " + m_PodcastLogoUrl);

            // Fetch the remote podcast logo to store locally in the IsolatedStorage.
            WebClient wc = new WebClient();
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_FetchPodcastLogoCompleted);
            wc.OpenReadAsync(m_PodcastLogoUrl);
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations        
        private PodcastEpisodesManager  m_podcastEpisodesManager;
        private IsolatedStorageFile     m_localPodcastIconCache;

        private void wc_FetchPodcastLogoCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                Debug.WriteLine("ERROR: Cannot fetch channel logo.");
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
                                                                     m_localPodcastIconCache))
            {
                isoFileStream.Write(logoMemory.ToArray(), 0, (int)logoMemory.Length);
            }

            Debug.WriteLine("Stored local podcast icon as: " + PodcastLogoLocalLocation);
            
            // Local cache has been updated - notify the UI that the logo property has changed.
            // and the new logo can be fetched to the UI. 
            NotifyPropertyChanged("PodcastLogo");
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
            private BackgroundWorker m_worker = new BackgroundWorker();
            private PodcastSqlModel m_podcastsSqlModel;

            public PodcastEpisodesManager(PodcastSubscriptionModel subscriptionModel)
            {
                m_subscriptionModel = subscriptionModel;
                m_podcastsSqlModel = PodcastSqlModel.getInstance();
            }

            public void updatePodcastEpisodes()
            {
                Debug.WriteLine("Updating episodes for podcast: " + m_subscriptionModel.PodcastName);
                m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWorkUpdateEpisodes);
                m_worker.RunWorkerAsync();
                m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_UpdateEpisodesCompleted);
            }

            void m_worker_UpdateEpisodesCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                if (e.Result == null)
                {
                    return;
                }


                int newPodcastEpisodes = (int)e.Result;

                Debug.WriteLine("Got {0} new episodes.", newPodcastEpisodes);
                m_subscriptionModel.NewEpisodesCount = newPodcastEpisodes.ToString();
            }

            private void m_worker_DoWorkUpdateEpisodes(object sender, DoWorkEventArgs args)
            {
                bool subscriptionAddedNow = true;
                List<PodcastEpisodeModel> episodes = m_podcastsSqlModel.episodesForSubscription(m_subscriptionModel);
                DateTime latestEpisodePublishDate = new DateTime();

                if (episodes.Count > 0)
                {
                    // The episodes are in descending order as per publish date. 
                    // So take the first episode and we have the latest known publish date.
                    latestEpisodePublishDate = episodes[0].EpisodePublished;

                    // If we already have episodes, this subscription is not being added now.
                    subscriptionAddedNow = false;
                }

                Debug.WriteLine("\nStarting to parse episodes for podcast: " + m_subscriptionModel.PodcastName);
                List<PodcastEpisodeModel> newPodcastEpisodes = PodcastFactory.newPodcastEpisodes(m_subscriptionModel.CachedPodcastRSSFeed, latestEpisodePublishDate);
                if (newPodcastEpisodes == null)
                {
                    Debug.WriteLine("WARNING: Got null list of new episodes.");
                    return;
                }

                // Indicate new episodes to the UI only when we are not adding the feed. 
                // I.e. we want to show new episodes only when we refresh the feed at restart.
                if (subscriptionAddedNow == false)
                {
                    args.Result = newPodcastEpisodes.Count;
                }

                m_podcastsSqlModel.insertEpisodesForSubscription(m_subscriptionModel, newPodcastEpisodes);
            }
        }
        #endregion
    }
}