using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.ComponentModel;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;

namespace Podcatcher.ViewModels
{
    [Table(Name="Episodes")]
    public class PodcastEpisodeModel : INotifyPropertyChanged
    {
        /****************************** PodcastEpisodeHandler definitions ***************************/

        public delegate void PodcastEpisodesHandler(object source, PodcastEpisodesArgs e);

        public class PodcastEpisodesArgs
        {
        }

        /************************************* Public properties *******************************/
        #region properties

        private int m_episodeId;
        [Column(Storage = "m_episodeId", IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
        private int EpisodeId
        {
            get { return m_episodeId; }
            set { m_episodeId = value; }
        }

        private EntityRef<PodcastSubscriptionModel> m_podcast = new EntityRef<PodcastSubscriptionModel>();
        [Association(Storage = "m_podcast", ThisKey="PodcastId", OtherKey = "PodcastId", IsForeignKey = true)]
        public PodcastSubscriptionModel PodcastSubscription
        {
            get { return m_podcast.Entity; }
            set { m_podcast.Entity = value; }
        }

        private int m_podcastId;
        [Column(Storage = "m_podcastId", UpdateCheck = UpdateCheck.Never)]
        public int PodcastId
        {
            get { return m_podcastId; }
            set { m_podcastId = value; }
        }

        private string m_name;
        [Column(UpdateCheck=UpdateCheck.Never)]
        public String EpisodeName
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private string m_description;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeDescription
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private DateTime m_published;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public DateTime EpisodePublished 
        {
            get { return m_published; }
            set { m_published = value; }
        }

        public String EpisodePublishedString
        {
            get 
            {
                string format = "dd MMM yyyy";
                return m_published.ToString(format); 
            }
        }
        
        private string m_episodeDownloadUrl;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public string EpisodeDownloadUri
        {
            get { return m_episodeDownloadUrl; }
            set { m_episodeDownloadUrl = value; }
        }

        private long m_episodeDownloadSize;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public long EpisodeDownloadSize
        {
            get { return m_episodeDownloadSize; }
            set { m_episodeDownloadSize = value; }
        }

        private String m_episodeRunningTime;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeRunningTime
        {
            get { return m_episodeRunningTime; }
            set { m_episodeRunningTime = value; }
        }

        private String m_episodeFile;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeFile
        {
            get { return m_episodeFile; }
            set { m_episodeFile = value; }
        }

        private long m_episodePlayBookmark;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public long EpisodePlayBookmark
        {
            get { return m_episodePlayBookmark; }
            set { m_episodePlayBookmark = value; }
        }

        public enum EpisodeStateVal
        {
            Idle,
            Queued,
            Downloading,
            Playable
        };

        private EpisodeStateVal m_episodeState;
        [Column(Storage = "m_episodeState", DbType = "int", CanBeNull = true)]
        public EpisodeStateVal EpisodeState
        {
            get 
            { 
                return m_episodeState; 
            }

            set
            {
                m_episodeState = value;
                NotifyPropertyChanged("EpisodeState");
                NotifyPropertyChanged("EpisodeStatusText");
            }
        }

        public String EpisodeStatusText
        {
            get 
            {
                String statusText = "";
                switch (m_episodeState)
                {
                    case EpisodeStateVal.Downloading:
                        statusText = "Downloading...";
                        break;
                    case EpisodeStateVal.Idle:
                        statusText = "";
                        break;
                    case EpisodeStateVal.Playable:
                        statusText = "Play";
                        break;
                    case EpisodeStateVal.Queued:
                        statusText = "Queued";
                        break;
                }

                return statusText;
            }
        }

        private int m_downloadPercentage;
        public int DownloadPercentage 
        { 
            get 
            {
                return m_downloadPercentage;
            }

            set
            {
                if (m_downloadPercentage != value)
                {
                    m_downloadPercentage = value;
                    NotifyPropertyChanged("DownloadPercentage");
                }
            }
        }
        
        #endregion

        public event PodcastEpisodesHandler OnPodcastEpisodeStartedDownloading;
        public event PodcastEpisodesHandler OnPodcastEpisodeFinishedDownloading;

        /************************************* Public implementations *******************************/
        public PodcastEpisodeModel()
        {
//            EpisodeState = EpisodeStateVal.Idle;

        }

        public void downloadEpisode()
        {
            OnPodcastEpisodeStartedDownloading(this, m_eventArgs);

            Debug.WriteLine("Starting download episode ({0}): {1}...", PodcastSubscription.PodcastName, EpisodeName);

            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc_DownloadProgressChanged);
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_PodcastDownloadCompleted);
            wc.OpenReadAsync(new Uri(m_episodeDownloadUrl));
        }

        public void deleteDownloadedEpisode()
        {
            using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (episodeStore.FileExists(EpisodeFile) == false)
                {
                    Debug.WriteLine("WARNING: Could not find downloaded episode to download. Name: " + EpisodeFile);
                    return;
                }

                Debug.WriteLine("Deleting downloaded episode: " + EpisodeFile);
                episodeStore.DeleteFile(EpisodeFile);                
            }
            
            EpisodeFile = null;
            PodcastSqlModel.getInstance().setEpisodeState(this, EpisodeStateVal.Idle);
        }


        /************************************* Private implementations *******************************/
        #region private
        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadPercentage = e.ProgressPercentage;
        }

        private void wc_PodcastDownloadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            Debug.WriteLine("Finished downloading episode ({0}): {1}", PodcastSubscription.PodcastName, EpisodeName);

            if (e.Error != null)
            {
                Debug.WriteLine("ERROR: Web error: " + e.ToString());
                OnPodcastEpisodeFinishedDownloading(this, m_eventArgs);
                return;
            }

            Stream downloadStream = e.Result;
            string episodeFileName = localEpisodeFileName();

            // TODO: Do this in a thread.
            using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                byte[] buffer = new byte[1024];
                using(IsolatedStorageFileStream fileStream = episodeStore.OpenFile(episodeFileName, FileMode.CreateNew)) 
                {
                    int bytesRead = 0;
                    while ((bytesRead = downloadStream.Read(buffer, 0, 1024)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            EpisodeFile = episodeFileName;
            PodcastSqlModel.getInstance().setEpisodeState(this, EpisodeStateVal.Playable);

            Debug.WriteLine("Episode written to disk. Filename: {0}", episodeFileName);
            OnPodcastEpisodeFinishedDownloading(this, m_eventArgs);
        }

        private string localEpisodeFileName()
        {
            // Parse the filename of the logo from the remote URL.
            string localPath = new Uri(m_episodeDownloadUrl).LocalPath;
            string podcastEpisodeFilename = localPath.Substring(localPath.LastIndexOf('/') + 1);

            string localPodcastEpisodeFilename = App.PODCAST_DL_DIR + @"/" + podcastEpisodeFilename;
            Debug.WriteLine("Found episode filename: " + localPodcastEpisodeFilename);

            return localPodcastEpisodeFilename;
        }

        private PodcastEpisodesArgs m_eventArgs = new PodcastEpisodesArgs();
        #endregion

        #region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
