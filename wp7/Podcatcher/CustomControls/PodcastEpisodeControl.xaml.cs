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
using Podcatcher.ViewModels;

namespace Podcatcher
{
    public partial class PodcastEpisodeControl : UserControl
    {
        /************************************* Private implementations *******************************/
        public PodcastEpisodeControl()
        {
            // Required to initialize variables
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Page_Loaded);
        }

        /************************************* Private implementations *******************************/
        #region private
        private PodcastEpisodeModel m_episodeModel;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
            this.EpisodeRunningTime.Text = String.Format("Running time: {0}", m_episodeModel.EpisodeRunningTime);
        }

        private void EpisodeButton_Click(object sender, RoutedEventArgs e)
        {
            switch (m_episodeModel.EpisodeState)
            {
                // Episode is idle => start downloading. 
                case PodcastEpisodeModel.EpisodeStateVal.Idle:
                    PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();
                    downloadManager.addEpisodeToDownloadQueue(m_episodeModel);
                    break;

                case PodcastEpisodeModel.EpisodeStateVal.Playable:
                    PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                    m_episodeModel.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Playing;
                    player.play(m_episodeModel);
                    break;
            }
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = (sender as MenuItem).DataContext as PodcastEpisodeModel;
            podcastEpisode.deleteDownloadedEpisode();
        }
        #endregion
    }
}