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

namespace Podcatcher
{
    public partial class PodcastPlayerControl : UserControl
    {
        public event EventHandler PodcastPlayerStarted;

        private static PodcastPlayerControl m_instance;

        public PodcastPlayerControl()
        {
            InitializeComponent();
            
            this.NoPlayingLayout.Visibility = Visibility.Visible;
            this.PlayingLayout.Visibility = Visibility.Collapsed;

            m_instance = this;
        }

        public static PodcastPlayerControl getIntance()
        {
            return m_instance;
        }

        internal void play(PodcastEpisodeModel m_episodeModel)
        {
            Debug.WriteLine("Starting to play podcast episode: " + m_episodeModel.EpisodeName);

            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
            this.PodcastLogo.Source = m_episodeModel.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = m_episodeModel.EpisodeName;

            PodcastPlayerStarted(this, new EventArgs());
        }

        private void rewButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void playButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void ffButtonClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
