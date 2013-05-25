using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;

namespace Podcatcher.ViewModels
{
    [Table]
    public class PlaylistItem
    {

#region properties
        [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
        public int ItemId
        {
            get;
            set;
        }

        [Column]
        public int OrderNumber
        {
            get;
            set;
        }

        [Column]
        public string PodcastName
        {
            get;
            set;
        }

        [Column]
        public string PodcastLogoLocation
        {
            get;
            set;
        }

        public BitmapImage PodcastLogo
        {
            get
            {
                BitmapImage logo = null;
                using (var db = new PodcastSqlModel()) {
                    PodcastEpisodeModel ep = db.Episodes.Where(e => e.EpisodeName == EpisodeName).FirstOrDefault();
                    if (ep != null) 
                    {
                        logo = ep.PodcastSubscription.PodcastLogo;
                    }
                }

                return logo;
            }
        }

        [Column]
        public string EpisodeName
        {
            get;
            set;
        }

        [Column]
        public string EpisodeLocation
        {
            get;
            set;
        }

        [Column]
        public int EpisodeId
        {
            get;
            set;
        }

        [Column(DbType = "BIT DEFAULT 0 NOT NULL")]
        public bool IsCurrent
        {
            get;
            set;
        }
#endregion

    }
}
