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
                BitmapImage logo = new BitmapImage(new Uri("/images/Podcatcher_generic_podcast_cover.png", UriKind.Relative));
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(PodcastLogoLocation))
                    {
                        stream = isoStore.OpenFile(PodcastLogoLocation, System.IO.FileMode.Open, FileAccess.Read);
                        try
                        {
                            logo.SetSource(stream);
                        }
                        catch (Exception e)
                        {
                            // Logo could not be set, using default logo.
                        }

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

        [Column(DbType = "BIGINT DEFAULT 0 NOT NULL")]
        public long SavedPlayPosTick
        {
            get;
            set;
        }

        [Column(DbType = "BIGINT DEFAULT 0 NOT NULL")]
        public long TotalPlayTicks
        {
            get;
            set;
        }

        [Column]
        public DateTime? LastPlayed
        {
            get;
            set;
        }
#endregion

    }
}
