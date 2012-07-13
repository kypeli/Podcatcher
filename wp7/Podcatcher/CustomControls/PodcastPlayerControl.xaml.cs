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

namespace Podcatcher
{
    public partial class PodcastPlayerControl : UserControl
    {
        public event EventHandler PodcastPlayerStarted;
        
        public PodcastPlayerControl()
        {
            InitializeComponent();
   
            this.NoPlayingLayout.Visibility = Visibility.Visible;
            this.PlayingLayout.Visibility = Visibility.Collapsed;

            m_instance = this;

            m_playButtonBitmap = new BitmapImage(new Uri("/Images/play.png", UriKind.Relative));
            m_pauseButtonBitmap = new BitmapImage(new Uri("/Images/pause.png", UriKind.Relative));

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(PlayStateChanged);

            if (BackgroundAudioPlayer.Instance.Track != null)
            {
                showPlayerLayout();
            }

            m_appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (m_appSettings.Contains("episodeId"))
            {
                int episodeId = (int)m_appSettings["episodeId"];
                m_currentEpisode = PodcastSqlModel.getInstance().episodeForEpisodeId(episodeId);
                
                setupPlayerUIContent(m_currentEpisode);
            }

        }

        public static PodcastPlayerControl getIntance()
        {
            return m_instance;
        }

        /************************************* Private implementation *******************************/

        private static PodcastPlayerControl m_instance;
        private BitmapImage m_playButtonBitmap;
        private BitmapImage m_pauseButtonBitmap;
        private static PodcastEpisodeModel m_currentEpisode = null;
        private bool settingSliderFromPlay;
        private IsolatedStorageSettings m_appSettings;

        internal void playEpisode(PodcastEpisodeModel episodeModel)
        {
            if (m_currentEpisode != null) { 
                m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Playable;
            }
            
            m_currentEpisode = episodeModel;
            m_appSettings.Remove("episodeId");
            m_appSettings.Add("episodeId", m_currentEpisode.PodcastId);

            setupPlayerUIContent(m_currentEpisode);
            showPlayerLayout();


            BackgroundAudioPlayer.Instance.Track = getAudioTrackForEpisode(m_currentEpisode);
            BackgroundAudioPlayer.Instance.Play();
            this.PlayButtonImage.Source = m_pauseButtonBitmap;

            PodcastPlayerStarted(this, new EventArgs());
        }

        private void setupPlayerUIContent(PodcastEpisodeModel currentEpisode)
        {
            this.PodcastLogo.Source = currentEpisode.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = currentEpisode.EpisodeName;
        }

        private void showPlayerLayout()
        {
            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
        }

        void PlayStateChanged(object sender, EventArgs e)
        {
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    // Player is playing
                    Debug.WriteLine("Podcast player is playing...");
                    this.TotalDurationText.Text = BackgroundAudioPlayer.Instance.Track.Duration.ToString("hh\\:mm\\:ss");
                    m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Playing;

                    // Set CompositionTarget.Rendering handler to update player position
                    CompositionTarget.Rendering += OnCompositionTargetRendering;
                    break;

                case PlayState.Paused:
                    // Player is on pause
                    Debug.WriteLine("Podcast player is paused...");
                    m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Paused;

                    // Clear CompositionTarget.Rendering 
                    CompositionTarget.Rendering -= OnCompositionTargetRendering;
                    break;
                case PlayState.Stopped:
                case PlayState.Shutdown:
                    m_appSettings.Remove("episodeId");
                    break;

            }
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

        void OnCompositionTargetRendering(object sender, EventArgs args)
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
