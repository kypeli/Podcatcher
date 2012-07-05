﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.ComponentModel;

namespace Podcatcher
{
    [Table(Name="Episodes")]
    public class PodcastEpisodeModel : INotifyPropertyChanged
    {
        public delegate void PodcastEpisodesHandler(object source, PodcastEpisodesArgs e);

        public class PodcastEpisodesArgs
        {
        }

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
            get { return @"Running time: " + m_episodeRunningTime; }
            set { m_episodeRunningTime = value; }
        }

        public enum EpisodeStateVal
        {
            Idle,
            Queued,
            Downloading
        };

        private EpisodeStateVal m_episodeState;
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
            }
        }
        
        #endregion

        public event PodcastEpisodesHandler OnPodcastEpisodeStartedDownloading;
        public event PodcastEpisodesHandler OnPodcastEpisodeFinishedDownloading;
        
        public PodcastEpisodeModel()
        {
            EpisodeState = EpisodeStateVal.Idle;
        }

        public void downloadEpisode()
        {
            EpisodeState = EpisodeStateVal.Downloading;
        }

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
