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
using System.Collections.Specialized;

namespace Podcatcher
{
    public partial class MainView : PhoneApplicationPage
    {
        private const int PODCAST_PLAYER_PIVOR_INDEX = 2;
        private const int TRIAL_SUBSCRIPTION_LIMIT   = 2;
        
        private PodcastSqlModel m_podcastsModel = PodcastSqlModel.getInstance();
        private PodcastEpisodesDownloadManager m_episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();
        private PodcastSubscriptionsManager m_subscriptionsManager;

        public MainView()
        {
            InitializeComponent();

            // Hook data contextes.
            DataContext = m_podcastsModel;

            // Upon startup, refresh all subscriptions so we get the latest episodes for each. 
            m_subscriptionsManager = PodcastSubscriptionsManager.getInstance();
            m_subscriptionsManager.refreshSubscriptions();

            // Post-pageinitialization event call hookup.
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            // Hook to the event when the download list changes, so we can update the pivot header text for the 
            // download page. 
            ((INotifyCollectionChanged)EpisodeDownloadList.Items).CollectionChanged += downloadListChanged;
            this.EpisodeDownloadList.ItemsSource = m_episodeDownloadManager.EpisodeDownloadQueue;

            // Hook to the event when the podcast player starts playing. 
            this.PodcastPlayer.PodcastPlayerStarted += new EventHandler(PodcastPlayer_PodcastPlayerStarted);
        }

        void PodcastPlayer_PodcastPlayerStarted(object sender, EventArgs e)
        {
            PodcastPlayerControl player = sender as PodcastPlayerControl;

            // Got event that the podcast player started playing. We now
            //  - Pop the navigation back to the main page (yes, we know that the subscription page is open).
            //  - Set the pivot index to show the player. 
            // ...I don't really like this, but seems this is the way to work with the pivot control.
            NavigationService.GoBack();
            this.NavigationPivot.SelectedIndex = PODCAST_PLAYER_PIVOR_INDEX;
            this.PodcastPlayerHeader.AltText = player.PlayingEpisode.PodcastSubscription.PodcastName;
        }

        private void downloadListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            int episodeDownloads = EpisodeDownloadList.Items.Count;
            string downloadText = "";

            switch (episodeDownloads)
            {
                case 0:
                    downloadText = "";
                    EpisodesDownloadingText.Visibility = Visibility.Visible;
                    break;
                case 1:
                    downloadText = @"1 episode downloading";
                    EpisodesDownloadingText.Visibility = Visibility.Collapsed;
                    break;
                default:
                    downloadText = String.Format("{0} episodes downloading", EpisodeDownloadList.Items.Count);
                    EpisodesDownloadingText.Visibility = Visibility.Collapsed;
                    break;
            }

            this.DownloadPivotHeader.AltText = downloadText;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AddSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            bool allowAddSubscription = true;
            if ((Application.Current as App).IsTrial)
            {
                if (m_podcastsModel.PodcastSubscriptions.Count >= TRIAL_SUBSCRIPTION_LIMIT)
                {
                    allowAddSubscription = false;
                }
            }


            if (allowAddSubscription)
            {
                NavigationService.Navigate(new Uri("/Views/AddSubscription.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show("You have reached the limit of the trial version for Podcatcher. Please purchase the full version from Marketplace.");
            }
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
