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
            PodcastSubscriptionModel subscription = m_podcastSqlModel.subscriptionModelForIndex(podcastId);
            episodes = new ObservableCollection<PodcastEpisodeModel>(subscription.Episodes.ToList());
            this.PodcastName.Text           = subscription.PodcastName;
            this.PodcastDescription.Text    = subscription.PodcastDescription;
            this.PodcastIcon.Source         = subscription.PodcastLogo;
            this.EpisodeList.ItemsSource    = episodes;
        }

        /************************************* Priovate implementations *******************************/
        #region private

        private PodcastSqlModel m_podcastSqlModel;
        ObservableCollection<PodcastEpisodeModel> episodes;
        
        #endregion
    }
}