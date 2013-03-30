using System;
using System.Linq;
using System.Data.Linq;
using Podcatcher.ViewModels;

namespace Podcatcher
{
    public class PlaylistDBContext : DataContext
    {
        private const string m_connection = "Data Source=isostore:/PodcatcherPlaylists.sdf";
        
        public Table<PlaylistItem> Playlist;

        public PlaylistDBContext()
            : base(m_connection)
        {
            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            Playlist = GetTable<PlaylistItem>();
            // TODO: Handle scheme changes.                    
        }

    }
}
