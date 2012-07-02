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
using System.Collections.ObjectModel;
using Podcatcher.ViewModels;
using System.Diagnostics;

namespace Podcatcher
{
    public partial class MainView : PhoneApplicationPage
    {
        private PodcastSqlModel m_podcastsModel  = PodcastSqlModel.getInstance();
        private PodcastSubscriptionsManager m_subscriptionsManager;

        public MainView()
        {
            InitializeComponent();
            DataContext = m_podcastsModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            m_subscriptionsManager = PodcastSubscriptionsManager.getInstance();
            m_subscriptionsManager.refreshSubscriptions();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AddSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AddSubscription.xaml", UriKind.Relative));
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0) { 
                PodcastSubscriptionModel tappedSubscription = e.AddedItems[0] as PodcastSubscriptionModel;
                Debug.WriteLine("Showing episodes for podcast. Name: " + tappedSubscription.PodcastName);
                NavigationService.Navigate(new Uri(string.Format("/Views/PodcastEpisodes.xaml?podcastId={0}", tappedSubscription.PodcastId), UriKind.Relative));
                this.SubscriptionsList.SelectedIndex = -1;  // Aaargh... stupid Silverlight.
            }
        }
    }
}