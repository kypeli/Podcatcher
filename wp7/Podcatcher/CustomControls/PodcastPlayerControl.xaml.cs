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

namespace Podcatcher
{
    public partial class PodcastPlayerControl : UserControl
    {
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
            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
            this.PodcastLogo.Source = m_episodeModel.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = m_episodeModel.EpisodeName;
        }
    }
}
