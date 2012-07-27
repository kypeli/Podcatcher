using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Podcatcher.ViewModels;

namespace Podcatcher
{
    public partial class PodcastDownloadControl : UserControl
    {
        private PodcastEpisodeModel m_episodeModel;

        /************************************* Public implementations *******************************/
        public PodcastDownloadControl()
        {
            InitializeComponent();
            this.DownloadProgressText.Text = "";
            this.CancelDownloadButton.Visibility = Visibility.Collapsed;

            Loaded += new RoutedEventHandler(PodcastDownloadControl_Loaded);
        }

        void PodcastDownloadControl_Loaded(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.CancelDownloadButton.Visibility = Visibility.Visible; 
            this.DownloadProgressText.Text = String.Format("{0} %", (sender as ProgressBar).Value);
        }

        private void StopDownloadImage_Tap(object sender, GestureEventArgs e)
        {
            PodcastEpisodesDownloadManager.getInstance().cancelEpisodeDownload(m_episodeModel);            
        }
    }
}
