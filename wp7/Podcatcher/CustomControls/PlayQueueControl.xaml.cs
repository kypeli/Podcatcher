using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using Podcatcher.ViewModels;

namespace Podcatcher
{
    public partial class PlayQueueControl : UserControl
    {
        public PlayQueueControl()
        {
            InitializeComponent();
        }

        private void PlayQueueItemTapped(object sender, GestureEventArgs e)
        {
            int episodeID = (int)(sender as StackPanel).Tag;

            using (var queueDb = new PlaylistDBContext())
            {
                PlaylistItem current = queueDb.Playlist.FirstOrDefault(item => item.IsCurrent == true);
                if (current != null)
                {
                    current.IsCurrent = false;
                }

                PlaylistItem next = queueDb.Playlist.First(item => item.EpisodeId == episodeID);
                next.IsCurrent = true;

                queueDb.SubmitChanges();
            }

            PodcastPlayerControl player = PodcastPlayerControl.getIntance();
            using (var db = new PodcastSqlModel())
            {
                PodcastEpisodeModel ep = db.episodeForEpisodeId(episodeID);
                player.playEpisode(ep);
                ep.setPlaying();
            }

            App.mainViewModels.PlayQueue = new System.Collections.ObjectModel.ObservableCollection<PlaylistItem>();
        }
    }
}
