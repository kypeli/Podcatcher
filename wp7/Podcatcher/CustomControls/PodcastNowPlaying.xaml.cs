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
using System.Windows;
using System.Windows.Controls;
using System.IO.IsolatedStorage;
using Podcatcher.ViewModels;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;

namespace Podcatcher
{
    public partial class PodcastNowPlaying : UserControl
    {
        private static PodcastEpisodeModel currentlyPlayingEpisodeInPlayhistory = null;
        private BitmapImage m_podcastLogo;

        public PodcastNowPlaying()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                return;
            }
        }

        internal void SetupNowPlayingView()
        {
            if (App.currentlyPlayingEpisodeId > 0)
            {
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            if (currentlyPlayingEpisodeInPlayhistory == null
                || App.currentlyPlayingEpisodeId != currentlyPlayingEpisodeInPlayhistory.EpisodeId)
            {
                using (var db = new PodcastSqlModel())
                {
                    currentlyPlayingEpisodeInPlayhistory = db.episodeForEpisodeId(App.currentlyPlayingEpisodeId);
                    m_podcastLogo = currentlyPlayingEpisodeInPlayhistory.PodcastSubscription.PodcastLogo;
                }
            }

            if (currentlyPlayingEpisodeInPlayhistory != null)
            {
                this.Visibility = Visibility.Visible;
                this.DataContext = currentlyPlayingEpisodeInPlayhistory;
                this.PodcastLogo.Source = m_podcastLogo;
            }
            
        }

        private void NowPlayingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Views/PodcastPlayerView.xaml", UriKind.Relative));
        }
    }
}
