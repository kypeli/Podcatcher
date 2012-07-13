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
            m_isPlaying = false;

            m_playButtonBitmap = new BitmapImage(new Uri("/Images/play.png", UriKind.Relative));
            m_pauseButtonBitmap = new BitmapImage(new Uri("/Images/pause.png", UriKind.Relative));
        }

        public static PodcastPlayerControl getIntance()
        {
            return m_instance;
        }

        /************************************* Private implementation *******************************/

        private static PodcastPlayerControl m_instance;
        private bool m_isPlaying;
        private BitmapImage m_playButtonBitmap;
        private BitmapImage m_pauseButtonBitmap;

        internal void play(PodcastEpisodeModel m_episodeModel)
        {
            Debug.WriteLine("Starting to play podcast episode: " + m_episodeModel.EpisodeName);

            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
            this.PodcastLogo.Source = m_episodeModel.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = m_episodeModel.EpisodeName;
            m_isPlaying = true;

            PodcastPlayerStarted(this, new EventArgs());
        }

        private void rewButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void playButtonClicked(object sender, RoutedEventArgs e)
        {
            if (m_isPlaying)
            {
                // Player is playing
                this.PlayButtonImage.Source = m_pauseButtonBitmap;
            }
            else
            {
                // Player is on pause
                this.PlayButtonImage.Source = m_playButtonBitmap;
            }

            // Revert the play state.
            m_isPlaying = !m_isPlaying; 

        }

        private void ffButtonClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
