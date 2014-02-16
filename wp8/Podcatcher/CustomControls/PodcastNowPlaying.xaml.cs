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
using Microsoft.Phone.BackgroundAudio;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Podcatcher
{
    public partial class PodcastNowPlaying : UserControl
    {
        private Dictionary<int, WeakReference> m_logoCache = new Dictionary<int, WeakReference>();
        private BitmapImage m_podcastLogo = null;
        private int m_currentlyPlayingEpisodeId = -1;
        private PodcastPlaybackManager m_playbackManager = null;

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
            this.DataContext = null;

            m_playbackManager = PodcastPlaybackManager.getInstance();

            if (m_playbackManager.CurrentlyPlayingEpisode != null)
            {
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            if (m_currentlyPlayingEpisodeId < 0
                || m_playbackManager.CurrentlyPlayingEpisode.EpisodeId != m_currentlyPlayingEpisodeId)
            {
                m_currentlyPlayingEpisodeId = m_playbackManager.CurrentlyPlayingEpisode.EpisodeId;
                using (var db = new PodcastSqlModel())
                {
                    PodcastSubscriptionModel s = db.Subscriptions.First(sub => sub.PodcastId == m_playbackManager.CurrentlyPlayingEpisode.PodcastId);
                    m_podcastLogo = getLogoForSubscription(s);
                }
            }

            this.DataContext = m_playbackManager.CurrentlyPlayingEpisode; 
            this.PodcastLogo.Source = m_podcastLogo;
        }

        private BitmapImage getLogoForSubscription(PodcastSubscriptionModel subscription)
        {
            bool fillCache = false;
            BitmapImage logo = null;

            if (m_logoCache.ContainsKey(subscription.PodcastId) == false)
            {
                fillCache = true;
            }
            else
            {
                logo = m_logoCache[subscription.PodcastId].Target as BitmapImage;
                if (logo == null)
                {
                    fillCache = true;
                }
            }

            if (fillCache)
            {
                logo = subscription.PodcastLogo;
                WeakReference cachedLogo = new WeakReference(logo);
                m_logoCache[subscription.PodcastId] = cachedLogo;
            }

            return logo;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan position = m_playbackManager.getTrackPlayPosition();
            TimeSpan duration = m_playbackManager.getTrackPlayDuration();

            CurrentPlayTime.Text = position.ToString("hh\\:mm\\:ss");
            TotalPlayTime.Text = duration.ToString("hh\\:mm\\:ss");
        }
    }
}
