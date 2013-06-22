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
            // TODO: Handle scheme changes.                    
        }
    }
}
