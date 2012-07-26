using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;

namespace Podcatcher
{
    public partial class PodcastSubscriptionControl : UserControl
    {
        public PodcastSubscriptionControl()
        {
            // Required to initialize variables
            InitializeComponent();

            Loaded += new RoutedEventHandler(PodcastSubscriptionControl_Loaded);
        }

        void PodcastSubscriptionControl_Loaded(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionModel subscription = DataContext as PodcastSubscriptionModel;
            this.NumberOfEpisodes.Text = string.Format("{0} episodes", subscription.Episodes.Count);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionModel subscriptionToDelete = (sender as MenuItem).DataContext as PodcastSubscriptionModel;
            Debug.WriteLine("Delete podcast subscription. Name: " + subscriptionToDelete.PodcastName);

            PodcastSubscriptionsManager subscriptionsManager = PodcastSubscriptionsManager.getInstance();
            subscriptionsManager.deleteSubscription(subscriptionToDelete);
        }

    }
}