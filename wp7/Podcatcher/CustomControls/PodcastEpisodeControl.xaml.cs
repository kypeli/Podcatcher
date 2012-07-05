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

namespace Podcatcher
{
	public partial class PodcastEpisodeControl : UserControl
	{
		public PodcastEpisodeControl()
		{
			// Required to initialize variables
			InitializeComponent();
		}

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            PhoneApplicationPage p = (Application.Current as App).RootFrame.Content as PhoneApplicationPage;
            ListBox episodeList = p.FindName("EpisodeList") as ListBox;

            Button downloadButton = sender as Button;
            PodcastEpisodeModel podcastEpisode = (episodeList.ItemContainerGenerator.ContainerFromItem(downloadButton.DataContext) as ListBoxItem)
                                                 .DataContext as PodcastEpisodeModel;

            PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();
            downloadManager.addEpisodeToDownloadQueue(podcastEpisode);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = (sender as MenuItem).DataContext as PodcastEpisodeModel;
//            podcastEpisode.deleteDownloadedEpisode();
        }

	}
}