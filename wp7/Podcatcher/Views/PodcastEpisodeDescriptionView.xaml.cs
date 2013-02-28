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
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;
using System.Diagnostics;

namespace Podcatcher.Views
{
    public partial class PodcastEpisodeDescriptionView : PhoneApplicationPage
    {
        private PodcastEpisodeModel m_podcastEpisode;

        public PodcastEpisodeDescriptionView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs naviArgs)
        {
            this.DownloadButton.Visibility = System.Windows.Visibility.Collapsed;
            try
            {
                int podcastEpisodeId = int.Parse(NavigationContext.QueryString["episodeId"]);
                using (var db = new PodcastSqlModel())
                {
                    m_podcastEpisode = db.episodeForEpisodeId(podcastEpisodeId);
                    if (m_podcastEpisode != null)
                    {
                        this.DataContext = m_podcastEpisode;
                        if (m_podcastEpisode.isPlayable() && String.IsNullOrEmpty(m_podcastEpisode.EpisodeFile))
                        {
                            this.DownloadButton.Visibility = System.Windows.Visibility.Visible;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Episode model is null. Cannot show description.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Cannot get episode id. Error: " + e.Message);
             }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }

            PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();
            PodcastEpisodesDownloadManager.notifyUserOfDownloadRestrictions(m_podcastEpisode);
            downloadManager.addEpisodeToDownloadQueue(m_podcastEpisode);

        }
    }
}