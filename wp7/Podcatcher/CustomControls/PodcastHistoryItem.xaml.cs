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

namespace Podcatcher
{
    public partial class PodcastHistoryItem : UserControl
    {
        public PodcastHistoryItem()
        {
            InitializeComponent();
        }

        private void PlayHistoryItemTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PodcastEpisodeModel episode = DataContext as PodcastEpisodeModel;
            if (episode == null || episode.PodcastSubscription == null)
            {
                App.showNotificationToast("You don't subscribe to the podcast anymore.");
                return;
            }

            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri(string.Format("/Views/PodcastEpisodes.xaml?podcastId={0}", episode.PodcastSubscription.PodcastId), UriKind.Relative));
        }
    }
}
