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
using System.Collections.ObjectModel;

namespace Podcatcher.Views
{
    public partial class PodcastEpisodes : PhoneApplicationPage
    {

        /************************************* Public implementations *******************************/
        public PodcastEpisodes()
        {
            InitializeComponent();
            m_podcastSqlModel = PodcastSqlModel.getInstance();
        }

        
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            int podcastId = int.Parse(NavigationContext.QueryString["podcastId"]);
            m_subscription = m_podcastSqlModel.subscriptionModelForIndex(podcastId);

            m_episodes = new ObservableCollection<PodcastEpisodeModel>(m_podcastSqlModel.episodesForSubscription(m_subscription));
            this.PodcastName.Text           = m_subscription.PodcastName;
            this.PodcastDescription.Text    = m_subscription.PodcastDescription;
            this.PodcastIcon.Source         = m_subscription.PodcastLogo;
            this.EpisodeList.ItemsSource    = m_episodes;
        }

        /************************************* Priovate implementations *******************************/
        #region private

        private PodcastSqlModel m_podcastSqlModel;
        private PodcastSubscriptionModel m_subscription;
        ObservableCollection<PodcastEpisodeModel> m_episodes;
        
        #endregion

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/SubscriptionSettings.xaml?podcastId={0}", m_subscription.PodcastId), UriKind.Relative));
        }
    }
}