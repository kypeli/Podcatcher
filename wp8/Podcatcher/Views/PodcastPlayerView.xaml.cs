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
using Microsoft.Phone.BackgroundAudio;
using System.Diagnostics;
using Podcatcher.ViewModels;
using Microsoft.Phone.Shell;

namespace Podcatcher.Views
{
    public partial class PodcastPlayerView : PhoneApplicationPage
    {
        public PodcastPlayerView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            this.PodcastPlayer.initializePlayerUI();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            this.PodcastPlayer.showNoPlayerLayout();
        }

        private void rewButtonClicked(object sender, EventArgs e)
        {
            BackgroundAudioPlayer player = BackgroundAudioPlayer.Instance;
            if (player != null && player.PlayerState == PlayState.Playing)
            {
                player.Position = (player.Position.TotalSeconds - 30 >= 0) ?
                                    TimeSpan.FromSeconds(player.Position.TotalSeconds - 30) :
                                    TimeSpan.FromSeconds(0);
            }
        }

        private void playButtonClicked(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                // Paused
                BackgroundAudioPlayer.Instance.Pause();
                PodcastPlayer.setupUIForEpisodePaused();
                //       PlayButtonImage.Source = m_playButtonBitmap;
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = new Uri("/Images/Light/play.png", UriKind.Relative);
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).Text = "Play";
            }
            else if (BackgroundAudioPlayer.Instance.Track != null)
            {
                // Playing
                BackgroundAudioPlayer.Instance.Play();
                PodcastPlayer.setupUIForEpisodePlaying();
                //            PlayButtonImage.Source = m_pauseButtonBitmap;
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = new Uri("/Images/Light/pause.png", UriKind.Relative);
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).Text = "Pause";
            }
            else
            {
                Debug.WriteLine("No track currently set. Trying to setup currently playing episode as track...");
                PodcastEpisodeModel ep = PodcastPlaybackManager.getInstance().CurrentlyPlayingEpisode;
                if (ep != null)
                {
                    PodcastPlaybackManager.getInstance().play(ep);
                }
                else
                {
                    Debug.WriteLine("Error: No currently playing track either! Giving up...");
                    App.showErrorToast("Something went wrong. Cannot play the track.");
                    PodcastPlayer.showNoPlayerLayout();
                }
            }
        }

        private void stopButtonClicked(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Stopped)
            {
                // We are already stopped (playback ended or something). Let's update the episode state.
                PodcastPlaybackManager.getInstance().CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded;
            }
            else
            {
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing
                    || BackgroundAudioPlayer.Instance.PlayerState == PlayState.Paused)
                {
                    BackgroundAudioPlayer.Instance.Stop();
                }
            }

            PodcastPlayer.PlaybackStopped();
        }

        private void ffButtonClicked(object sender, EventArgs e)
        {
            BackgroundAudioPlayer player = BackgroundAudioPlayer.Instance;
            if (player != null && player.PlayerState == PlayState.Playing)
            {
                player.Position = (player.Position.TotalSeconds + 30 < player.Track.Duration.TotalSeconds) ? TimeSpan.FromSeconds(player.Position.TotalSeconds + 30) :
                                                                                                             TimeSpan.FromSeconds(player.Track.Duration.TotalSeconds);
            }
        }
    }
}