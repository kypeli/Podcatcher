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

namespace Podcatcher
{
    public partial class PodcastNowPlaying : UserControl
    {
        private Dictionary<int, WeakReference> m_logoCache = new Dictionary<int, WeakReference>();
        private BitmapImage m_podcastLogo = null;
        private PodcastEpisodeModel m_currentlyShowingEpisode = null;

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
            PodcastPlaybackManager pm = PodcastPlaybackManager.getInstance();

            if (pm.CurrentlyPlayingEpisode != null)
            {
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            if (m_currentlyShowingEpisode == null 
                || pm.CurrentlyPlayingEpisode.EpisodeId != m_currentlyShowingEpisode.EpisodeId)
            {
                m_currentlyShowingEpisode = pm.CurrentlyPlayingEpisode;
                using (var db = new PodcastSqlModel())
                {
                    PodcastSubscriptionModel s = db.Subscriptions.First(sub => sub.PodcastId == pm.CurrentlyPlayingEpisode.PodcastId);
                    m_podcastLogo = getLogoForSubscription(s);
                }
            }

            this.DataContext = m_currentlyShowingEpisode;
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

        private void NowPlayingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Views/PodcastPlayerView.xaml", UriKind.Relative));
        }
    }
}
