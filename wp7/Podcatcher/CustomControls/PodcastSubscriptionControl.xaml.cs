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
        private PodcastSubscriptionModel m_subscription;

        public PodcastSubscriptionControl()
        {
            // Required to initialize variables
            InitializeComponent();

            Loaded += new RoutedEventHandler(PodcastSubscriptionControl_Loaded);
        }

        void PodcastSubscriptionControl_Loaded(object sender, RoutedEventArgs e)
        {
            m_subscription = DataContext as PodcastSubscriptionModel;
            m_subscription.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(m_subscription_PropertyChanged);
            updateUnplayedEpisodesText();
        }

        private void updateUnplayedEpisodesText()
        {
            int unplayedEpisodes;

            lock (this)
            {
                unplayedEpisodes = m_subscription.UnplayedEpisodes;
            }

            if (unplayedEpisodes > 0)
            {
                this.NumberOfEpisodes.Text = string.Format("{0} episodes, {1} unplayed", m_subscription.Episodes.Count, unplayedEpisodes);
            }
            else
            {
                this.NumberOfEpisodes.Text = string.Format("{0} episodes", m_subscription.Episodes.Count);
            }
        }

        void m_subscription_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UnplayedEpisodes")
            {
                updateUnplayedEpisodesText();
            }
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