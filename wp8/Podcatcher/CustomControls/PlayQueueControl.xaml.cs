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
using Microsoft.Phone.Controls;

namespace Podcatcher
{
    public partial class PlayQueueControl : UserControl
    {
        public PlayQueueControl()
        {
            InitializeComponent();
        }

        private void PlayQueueItemTapped(object sender, RoutedEventArgs e)
        {
            int playlistItemId = (int)(sender as StackPanel).Tag;
            PodcastPlaybackManager.getInstance().playPlaylistItem(playlistItemId);
        }

        private void RemoveFromPlayQueue_Click(object sender, RoutedEventArgs e)
        {
            PlaylistItem playlistItem = (sender as MenuItem).DataContext as PlaylistItem;
            PodcastPlaybackManager.getInstance().removeFromPlayqueue(playlistItem.ItemId);
        }
    }
}
