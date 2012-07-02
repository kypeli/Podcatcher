using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;

namespace Podcatcher
{
    [Table(Name="Episodes")]
    public class PodcastEpisodeModel
    {
        #region properties
        private int m_episodeId;
        [Column(IsPrimaryKey = true, IsDbGenerated = true, AutoSync = AutoSync.OnInsert)]
        private int EpisodeId
        {
            get { return m_episodeId; }
            set { m_episodeId = value; }
        }

        private EntityRef<PodcastSubscriptionModel> m_podcast = new EntityRef<PodcastSubscriptionModel>();
        [Association(Storage = "m_podcast", OtherKey = "PodcastId", IsForeignKey = true)]
        public PodcastSubscriptionModel PodcastSubscription
        {
            get { return m_podcast.Entity; }
            set { m_podcast.Entity = value; }
        }

        private int m_podcastId;
        [Column(Storage="m_podcastId")]
        public int PodcastId
        {
            get { return m_podcastId; }
            set { m_podcastId = value; }
        }

        private string m_name;
        [Column]
        public String EpisodeName
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private string m_description;
        [Column]
        public String EpisodeDescription
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private DateTime m_published;
        [Column]
        public DateTime EpisodePublished 
        {
            get { return m_published; }
            set { m_published = value; }
        }

        private string m_episodeDownloadUrl;
        [Column]
        public string EpisodeDownloadUri
        {
            get { return m_episodeDownloadUrl; }
            set { m_episodeDownloadUrl = value; }
        }

        private long m_episodeDownloadSize;
        [Column]
        public long EpisodeDownloadSize
        {
            get { return m_episodeDownloadSize; }
            set { m_episodeDownloadSize = value; }
        }

        #endregion

        public PodcastEpisodeModel()
        {
        }
    }
}
