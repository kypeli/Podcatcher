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
    public partial class PodcastLatestControl : UserControl
    {
        public PodcastLatestControl()
        {
            InitializeComponent();
        }

        private void LatestEpisodeTapped(object sender, GestureEventArgs e)
        {
            PodcastEpisodeModel episode = DataContext as PodcastEpisodeModel;
            if (episode == null)
            {
                App.showNotificationToast("You don't subscribe to the podcast anymore.");
                return;
            }

            PodcastPlaybackManager.getInstance().play(episode);
        }
    }
}
