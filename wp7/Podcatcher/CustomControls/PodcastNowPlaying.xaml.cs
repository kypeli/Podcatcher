using System;
using System.Windows;
using System.Windows.Controls;
using System.IO.IsolatedStorage;
using Podcatcher.ViewModels;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace Podcatcher
{
    public partial class PodcastNowPlaying : UserControl
    {
        private IsolatedStorageSettings m_appSettings;
        private PodcastEpisodeModel m_playingEpisode;

        public PodcastNowPlaying()
        {
            InitializeComponent();
            m_appSettings = IsolatedStorageSettings.ApplicationSettings;
        }

        internal void SetupNowPlayingView()
        {
            if (m_appSettings.Contains(App.LSKEY_PODCAST_EPISODE_PLAYING_ID))
            {
                int episodeId = (int)m_appSettings[App.LSKEY_PODCAST_EPISODE_PLAYING_ID];
                m_playingEpisode = PodcastSqlModel.getInstance().episodeForEpisodeId(episodeId);
                if (m_playingEpisode != null)
                {
                    this.DataContext = m_playingEpisode;
                }
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }

        private void NowPlayingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/Views/PodcastPlayerView.xaml", UriKind.Relative));
        }
    }
}
