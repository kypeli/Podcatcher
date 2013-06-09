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
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;
using System.Linq;
using System.Collections.Generic;

namespace Podcatcher
{
    public partial class PodcastEpisodeControl : UserControl
    {
        /************************************* Private implementations *******************************/
        public PodcastEpisodeControl()
        {
            // Required to initialize variables
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Page_Loaded);
        }

        /************************************* Private implementations *******************************/
        #region private
        private PodcastEpisodeModel m_episodeModel;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PodcastPlaybackManager.getInstance().play(m_episodeModel);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
            PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();
            PodcastEpisodesDownloadManager.notifyUserOfDownloadRestrictions(m_episodeModel);
            downloadManager.addEpisodeToDownloadQueue(m_episodeModel);
        }


        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = (sender as MenuItem).DataContext as PodcastEpisodeModel;
            podcastEpisode.deleteDownloadedEpisode();
            PodcastSubscriptionsManager.getInstance().removedPlayableEpisode(podcastEpisode);

            using (var db = new PodcastSqlModel())
            {
                m_episodeModel = db.episodeForEpisodeId(m_episodeModel.EpisodeId);
            }

            this.DataContext = null;
            this.DataContext = m_episodeModel;
        }

        private void Episode_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = this.DataContext as PodcastEpisodeModel;
            PhoneApplicationFrame applicationFrame = Application.Current.RootVisual as PhoneApplicationFrame;

            if (podcastEpisode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Playing
                || podcastEpisode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Paused
                || podcastEpisode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Streaming)
            {
                applicationFrame.Navigate(new Uri("/Views/PodcastPlayerView.xaml", UriKind.Relative));
            }
            else
            {
                applicationFrame.Navigate(new Uri(string.Format("/Views/PodcastEpisodeDescriptionView.xaml?episodeId={0}", (this.DataContext as PodcastEpisodeModel).EpisodeId), UriKind.Relative));
            }
        }

        #endregion

        private void MenuItemAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = this.DataContext as PodcastEpisodeModel;
            PodcastPlaybackManager.getInstance().addToPlayqueue(podcastEpisode);
        }
    }
}