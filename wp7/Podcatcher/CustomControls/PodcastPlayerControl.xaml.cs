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
        }

        public static PodcastPlayerControl getIntance()
        {
            return m_instance;
        }

        /************************************* Private implementation *******************************/

        private static PodcastPlayerControl m_instance;
        private BitmapImage m_playButtonBitmap;
        private BitmapImage m_pauseButtonBitmap;
        private static PodcastEpisodeModel m_currentEpisode;

        internal void playEpisode(PodcastEpisodeModel episodeModel)
        {
            m_currentEpisode = episodeModel;

            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
            this.PodcastLogo.Source = m_currentEpisode.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = m_currentEpisode.EpisodeName;

            BackgroundAudioPlayer.Instance.Track = getAudioTrackForEpisode(m_currentEpisode);
            BackgroundAudioPlayer.Instance.Play();

            PodcastPlayerStarted(this, new EventArgs());
        }

        void PlayStateChanged(object sender, EventArgs e)
        {
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    // Player is playing
                    this.PlayButtonImage.Source = m_pauseButtonBitmap;
                    break;

                case PlayState.Paused:
                case PlayState.Stopped:
                    // Player is on pause
                    this.PlayButtonImage.Source = m_playButtonBitmap;
                    break;
            }
        }


        private void rewButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private void playButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
        }

        private void ffButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private AudioTrack getAudioTrackForEpisode(PodcastEpisodeModel m_currentEpisode)
        {
            return new AudioTrack(new Uri(m_currentEpisode.EpisodeFile, UriKind.Relative),
                        m_currentEpisode.EpisodeName,
                        m_currentEpisode.PodcastSubscription.PodcastName,
                        "",
                        new Uri(m_currentEpisode.PodcastSubscription.PodcastLogoLocalLocation, UriKind.Relative));
        }
    }
}
