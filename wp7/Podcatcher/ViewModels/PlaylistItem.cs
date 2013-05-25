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
                Stream stream = null;
                BitmapImage logo = new BitmapImage(new Uri("images/Podcatcher_generic_podcast_cover.png", UriKind.Relative));
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(PodcastLogoLocation))
                    {
                        stream = isoStore.OpenFile(PodcastLogoLocation, System.IO.FileMode.Open, FileAccess.Read);
                        logo.SetSource(stream);
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
