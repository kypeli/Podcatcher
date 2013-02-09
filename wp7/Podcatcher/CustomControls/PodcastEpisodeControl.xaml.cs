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
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using Podcatcher.ViewModels;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

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
            m_episodeModel = this.DataContext as PodcastEpisodeModel;

            // Play locally from a downloaded file.
            if (m_episodeModel.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
            {
                PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                player.playEpisode(m_episodeModel);
                m_episodeModel.PodcastSubscription.UnplayedEpisodes--;
            }

            // Stream it if not downloaded. 
            if (m_episodeModel.EpisodeDownloadState != PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
            {
                if (PodcastPlayerControl.isAudioPodcast(m_episodeModel))
                {
                    audioStreaming(m_episodeModel);
                }
                else
                {
                    PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                    player.StopPlayback();
                    videoStreaming(m_episodeModel);
                }
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
            PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();
            PodcastEpisodesDownloadManager.notifyUserOfDownloadRestrictions(m_episodeModel);
            downloadManager.addEpisodeToDownloadQueue(m_episodeModel);
        }

        private void videoStreaming(PodcastEpisodeModel podcastEpisode)
        {
            MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
            mediaPlayerLauncher.Media = new Uri(podcastEpisode.EpisodeDownloadUri, UriKind.Absolute);
            mediaPlayerLauncher.Controls = MediaPlaybackControls.All;
            mediaPlayerLauncher.Location = MediaLocationType.Data;
            mediaPlayerLauncher.Show();
        }

        private void audioStreaming(PodcastEpisodeModel podcastEpisode)
        {
            PodcastPlayerControl player = PodcastPlayerControl.getIntance();
            player.streamEpisode(podcastEpisode);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = (sender as MenuItem).DataContext as PodcastEpisodeModel;
            podcastEpisode.deleteDownloadedEpisode();
        }

        private void Episode_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri(string.Format("/Views/PodcastEpisodeDescriptionView.xaml?episodeId={0}", (this.DataContext as PodcastEpisodeModel).EpisodeId), UriKind.Relative));
        }

        #endregion
    }
}