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
using System.Linq;
using System.Data.Linq;
using Podcatcher.ViewModels;
using Microsoft.Phone.Data.Linq;

namespace Podcatcher
{
    public class PlaylistDBContext : DataContext
    {
        private const string m_connection = "Data Source=isostore:/PodcatcherPlaylists.sdf";
        private int PLAYLIST_DB_VERSION = 2;
        
        public Table<PlaylistItem> Playlist;

        public PlaylistDBContext()
            : base(m_connection)
        {
            DatabaseSchemaUpdater updater = null;
            if (!DatabaseExists())
            {
                CreateDatabase();
                updater = Microsoft.Phone.Data.Linq.Extensions.CreateDatabaseSchemaUpdater(this);
                updater.DatabaseSchemaVersion = PLAYLIST_DB_VERSION;
                updater.Execute();
            }

            if (updater == null)
            {
                updater = Microsoft.Phone.Data.Linq.Extensions.CreateDatabaseSchemaUpdater(this);

                if (updater.DatabaseSchemaVersion < 2)
                {
                    updater.AddColumn<PlaylistItem>("SavedPlayPosTick");
                    updater.AddColumn<PlaylistItem>("TotalPlayTicks");
                }

                updater.DatabaseSchemaVersion = PLAYLIST_DB_VERSION;
                updater.Execute();
            }

            Playlist = GetTable<PlaylistItem>();
        }
    }
}
