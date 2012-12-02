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
using Podcatcher.ViewModels;
using System.Diagnostics;

namespace Podcatcher.Views
{
    public partial class PodcastEpisodeDescriptionView : PhoneApplicationPage
    {
        private PodcastEpisodeModel m_podcastEpisode;

        public PodcastEpisodeDescriptionView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs naviArgs)
        {
            try
            {
                int podcastEpisodeId = int.Parse(NavigationContext.QueryString["episodeId"]);
                m_podcastEpisode = PodcastSqlModel.getInstance().episodeForEpisodeId(podcastEpisodeId);
                if (m_podcastEpisode != null)
                {
                    this.DataContext = m_podcastEpisode;
                }
                else
                {
                    Debug.WriteLine("Episode model is null. Cannot show description.");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Cannot get episode id. Error: " + e.Message);
             }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }

            PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();
            PodcastEpisodesDownloadManager.notifyUserOfDownloadRestrictions(m_podcastEpisode);
            downloadManager.addEpisodeToDownloadQueue(m_podcastEpisode);

        }
    }
}