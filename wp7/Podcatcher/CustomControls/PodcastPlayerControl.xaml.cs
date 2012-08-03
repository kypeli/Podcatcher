/**
 * Copyright (c) 2012, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the <organization> nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
using Podcatcher.ViewModels;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Phone.BackgroundAudio;
using System.IO.IsolatedStorage;
using System.Windows.Threading;

namespace Podcatcher
{
    public partial class PodcastPlayerControl : UserControl
    {
        public event EventHandler PodcastPlayerStarted;

        public PodcastEpisodeModel PlayingEpisode
        {
            get
            {
                return m_currentEpisode;
            }

            private set
            {

            }
        }

        public PodcastPlayerControl()
        {
            InitializeComponent();
            m_appSettings = IsolatedStorageSettings.ApplicationSettings;
            m_instance = this;

            setupPlayerUI();

            if (BackgroundAudioPlayer.Instance.Track != null)
            {
                Debug.WriteLine("Restoring UI for currently playing episode.");
                showPlayerLayout();
                restoreEpisodeToPlayerUI();
            }
            else
            {
                showNoPlayerLayout();
            }
        }

        public static PodcastPlayerControl getIntance()
        {
            return m_instance;
        }

        internal void playEpisode(PodcastEpisodeModel episodeModel)
        {
            Debug.WriteLine("Starting playback for episode: " + episodeModel);

            // Save the state for the previously playing podcast episode. 
            if (m_currentEpisode != null)
            {
                saveEpisodePlayPosition(m_currentEpisode);
                m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Playable;
            }

            m_currentEpisode = episodeModel;
            m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
            m_appSettings.Add(App.LSKEY_PODCAST_EPISODE_PLAYING_ID, m_currentEpisode.PodcastId);
            m_appSettings.Save();

            setupPlayerUIContent(m_currentEpisode);
            showPlayerLayout();

            if (m_currentEpisode.SavedPlayPos > 0)
            {
                askForContinueEpisodePlaying();
            }
            else
            {
                startPlayback();
            }

            m_currentEpisode.SavedPlayPos = 1;  // To mark that the playback has started - we update UI correctly in podcast listing.
        }

        /************************************* Private implementation *******************************/

        private static PodcastPlayerControl m_instance;
        private BitmapImage m_playButtonBitmap;
        private BitmapImage m_pauseButtonBitmap;
        private static PodcastEpisodeModel m_currentEpisode = null;
        private bool settingSliderFromPlay;
        private IsolatedStorageSettings m_appSettings;
        private DispatcherTimer m_screenUpdateTimer = new DispatcherTimer();

        private void setupPlayerUI()
        {
            m_playButtonBitmap = new BitmapImage(new Uri("/Images/play.png", UriKind.Relative));
            m_pauseButtonBitmap = new BitmapImage(new Uri("/Images/pause.png", UriKind.Relative));

            m_screenUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500); // Fire the timer every half a second.
            m_screenUpdateTimer.Tick += new EventHandler(m_screenUpdateTimer_Tick);
        }

        private void restoreEpisodeToPlayerUI()
        {
            // If we have an episodeId stored in local cache, this means we have that episode playing.
            // Hence, here we need to reload the episode data from the SQL. 
            if (m_appSettings.Contains(App.LSKEY_PODCAST_EPISODE_PLAYING_ID))
            {
                int episodeId = (int)m_appSettings[App.LSKEY_PODCAST_EPISODE_PLAYING_ID];
                m_currentEpisode = PodcastSqlModel.getInstance().episodeForEpisodeId(episodeId);
                m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Playing;

                if (m_currentEpisode == null)
                {
                    // Episode not in SQL anymore (maybe it was deleted). So clear up a bit...
                    m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
                    return;
                }

                setupPlayerUIContent(m_currentEpisode);
                setupUIForEpisodePlaying();
            }
            else
            {
                showNoPlayerLayout();
                Debug.WriteLine("WARNING: Could not find episode ID from app settings, so cannot restore player UI with episode information!");
            }
        }

        private void setupPlayerUIContent(PodcastEpisodeModel currentEpisode)
        {
            this.PodcastLogo.Source = currentEpisode.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = currentEpisode.EpisodeName;
        }

        private void showNoPlayerLayout()
        {
            this.NoPlayingLayout.Visibility = Visibility.Visible;
            this.PlayingLayout.Visibility = Visibility.Collapsed;
        }

        private void showPlayerLayout()
        {
            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
        }

        private void startPlayback()
        {
            startPlayback(TimeSpan.Zero);
        }

        private void startPlayback(TimeSpan position)
        {
            BackgroundAudioPlayer.Instance.Track = getAudioTrackForEpisode(m_currentEpisode);
            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(PlayStateChanged);

            if (position.Ticks > 0) 
            {
                BackgroundAudioPlayer.Instance.Position = new TimeSpan(position.Ticks);
            }

            BackgroundAudioPlayer.Instance.Play();
            this.PlayButtonImage.Source = m_pauseButtonBitmap;
            m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
            m_appSettings.Add(App.LSKEY_PODCAST_EPISODE_PLAYING_ID, m_currentEpisode.EpisodeId);
            m_appSettings.Save();

            PodcastPlayerStarted(this, new EventArgs());
        }

        private void StopPlayback()
        {
            playbackStopped();
            showNoPlayerLayout();
        }

        private void saveEpisodePlayPosition(PodcastEpisodeModel m_currentEpisode)
        {
            m_currentEpisode.SavedPlayPos = BackgroundAudioPlayer.Instance.Position.Ticks;
        }

        private void askForContinueEpisodePlaying()
        {
            MessageBoxButton messageButtons = MessageBoxButton.OKCancel;
            MessageBoxResult messageBoxResult = MessageBox.Show("You have previously played this episode. Do you wish to continue from the previous position?",
                                                                "Continue?",
                                                                messageButtons);
            if (messageBoxResult == MessageBoxResult.OK)
            {
                startPlayback(new TimeSpan(m_currentEpisode.SavedPlayPos));
            }
            else
            {
                startPlayback(new TimeSpan(0));
            }
        }

        void PlayStateChanged(object sender, EventArgs e)
        {
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    // Player is playing
                    Debug.WriteLine("Podcast player is playing...");
                    m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Playing;
                    setupUIForEpisodePlaying();
                    break;

                case PlayState.Paused:
                    // Player is on pause
                    Debug.WriteLine("Podcast player is paused...");
                    m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Paused;
                    saveEpisodePlayPosition(m_currentEpisode);
                    setupUIForEpisodePaused();

                    // Clear CompositionTarget.Rendering 
                    m_screenUpdateTimer.Stop();
                    break;

                case PlayState.Stopped:
                    // Player stopped
                    Debug.WriteLine("Podcast player stopped.");
                    break;

                case PlayState.Shutdown:
                    playbackStopped();
                    break;

            }
        }

        private void playbackStopped()
        {
            BackgroundAudioPlayer.Instance.PlayStateChanged -= new EventHandler(PlayStateChanged);
            
            saveEpisodePlayPosition(m_currentEpisode);
            m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateEnum.Playable;
            m_currentEpisode = null;
            m_screenUpdateTimer.Stop();
            m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
        }

        private void setupUIForEpisodePaused()
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                this.PlayButtonImage.Source = m_pauseButtonBitmap;
            }
            else
            {
                this.PlayButtonImage.Source = m_playButtonBitmap;
            }
        }

        private void setupUIForEpisodePlaying()
        {
            this.TotalDurationText.Text = BackgroundAudioPlayer.Instance.Track.Duration.ToString("hh\\:mm\\:ss");
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                this.PlayButtonImage.Source = m_pauseButtonBitmap;
            }
            else
            {
                this.PlayButtonImage.Source = m_playButtonBitmap;
            }

            m_screenUpdateTimer.Start();
        }


        private void rewButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            BackgroundAudioPlayer.Instance.Rewind();
        }

        private void playButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.Pause();
                this.PlayButtonImage.Source = m_playButtonBitmap;
            }
            else
            {
                BackgroundAudioPlayer.Instance.Play();
                this.PlayButtonImage.Source = m_pauseButtonBitmap;
            }
        }

        private void stopButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing) { 
                BackgroundAudioPlayer.Instance.Stop();
            }

            StopPlayback();
        }

        private void ffButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            BackgroundAudioPlayer.Instance.FastForward();
        }

        private AudioTrack getAudioTrackForEpisode(PodcastEpisodeModel m_currentEpisode)
        {
            return new AudioTrack(new Uri(m_currentEpisode.EpisodeFile, UriKind.Relative),
                        m_currentEpisode.EpisodeName,
                        m_currentEpisode.PodcastSubscription.PodcastName,
                        "",
                        new Uri(m_currentEpisode.PodcastSubscription.PodcastLogoLocalLocation, UriKind.Relative));
        }

        void m_screenUpdateTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan position = TimeSpan.Zero;
            TimeSpan duration = TimeSpan.Zero;

            duration = BackgroundAudioPlayer.Instance.Track.Duration;
            position = BackgroundAudioPlayer.Instance.Position;

            this.CurrentPositionText.Text = position.ToString("hh\\:mm\\:ss");

            settingSliderFromPlay = true;
            if (duration.Ticks > 0)
            {
                this.PositionSlider.Value = (double)position.Ticks / duration.Ticks;
            }
            else
            {
                this.PositionSlider.Value = 0;
            }
            settingSliderFromPlay = false;
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!settingSliderFromPlay)
            {
                if (e.NewValue <= 0.1)
                {
                    return;
                }

                AudioTrack audioTrack = null;
                TimeSpan duration = TimeSpan.Zero;

                try
                {
                    // Sometimes these property accesses will raise exceptions
                    audioTrack = BackgroundAudioPlayer.Instance.Track;

                    if (audioTrack != null)
                        duration = audioTrack.Duration;
                }
                catch
                {
                }

                if (audioTrack == null)
                    return;

                long ticks = (long)(e.NewValue * duration.Ticks);
                BackgroundAudioPlayer.Instance.Position = new TimeSpan(ticks);
            }
        }
    }
}
