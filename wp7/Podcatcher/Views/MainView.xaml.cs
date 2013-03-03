/**
 * Copyright (c) 2012, 2013, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */



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
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

namespace Podcatcher
{
    public partial class MainView : PhoneApplicationPage
    {
        private const int PODCAST_PLAYER_PIVOR_INDEX = 2;
        private const int TRIAL_SUBSCRIPTION_LIMIT   = 2;
        
        private PodcastEpisodesDownloadManager m_episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();
        private PodcastSubscriptionsManager m_subscriptionsManager;
        private PodcastPlayerControl m_playerControl;
        private IsolatedStorageSettings m_applicationSettings = null;

        public MainView()
        {
            InitializeComponent();

            // Hook to the event when the download list changes, so we can update the pivot header text for the 
            // download page. 
            ((INotifyCollectionChanged)EpisodeDownloadList.Items).CollectionChanged += downloadListChanged;

            // Upon startup, refresh all subscriptions so we get the latest episodes for each. 
            m_subscriptionsManager = PodcastSubscriptionsManager.getInstance();
            m_subscriptionsManager.OnPodcastSubscriptionsChanged += new SubscriptionManagerHandler(m_subscriptionsManager_OnPodcastSubscriptionsChanged);
            m_subscriptionsManager.refreshSubscriptions();

            // Hook to SkyDrive export events
            m_subscriptionsManager.OnOPMLExportToSkydriveChanged += new SubscriptionManagerHandler(m_subscriptionsManager_OnOPMLExportToSkydriveChanged);

            m_applicationSettings = IsolatedStorageSettings.ApplicationSettings;

            // Hook to the event when the podcast player starts playing. 
            m_playerControl = PodcastPlayerControl.getIntance();
            m_playerControl.PodcastPlayerStarted += new EventHandler(PodcastPlayer_PodcastPlayerStarted);

            PodcastSubscriptionsManager.getInstance().OnPodcastChannelDeleteStarted
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelDeleteStarted);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelDeleteFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelDeleteFinished);

            handleShowReviewPopup();
        }

        private void subscriptionManager_OnPodcastChannelDeleteStarted(object source, SubscriptionManagerArgs e)
        {
            ProgressText.Text = "Unsubscribing";
            deleteProgressOverlay.Visibility = Visibility.Visible;
        }
        
        private void subscriptionManager_OnPodcastChannelDeleteFinished(object source, SubscriptionManagerArgs e)
        {
            deleteProgressOverlay.Visibility = Visibility.Collapsed;
        }

        void m_subscriptionsManager_OnPodcastSubscriptionsChanged(object source, SubscriptionManagerArgs e)
        {
            if (e.state == PodcastSubscriptionsManager.SubscriptionsState.StartedRefreshing)
            {
                UpdatingIndicator.Visibility = Visibility.Visible;
            }

            if (e.state == PodcastSubscriptionsManager.SubscriptionsState.FinishedRefreshing)
            {
                UpdatingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        void m_subscriptionsManager_OnOPMLExportToSkydriveChanged(object source, SubscriptionManagerArgs e)
        {
            if (e.state == PodcastSubscriptionsManager.SubscriptionsState.StartedSkydriveExport)
            {
                ProgressText.Text = "Exporting to SkyDrive";
                deleteProgressOverlay.Visibility = Visibility.Visible;
            }

            if (e.state == PodcastSubscriptionsManager.SubscriptionsState.FinishedSkydriveExport)
            {
                deleteProgressOverlay.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            ObservableCollection<PodcastEpisodeModel> playHistory = App.mainViewModels.PlayHistoryListProperty;

            // Hook data contextes.
            this.DataContext = null;
            this.DataContext = App.mainViewModels;
            this.PlayHistoryList.ItemsSource = playHistory;
            this.EpisodeDownloadList.ItemsSource = m_episodeDownloadManager.EpisodeDownloadQueue;            
            this.NowPlaying.SetupNowPlayingView();


            if (playHistory.Count == 0)
            {
                this.PlayHistory.Visibility = System.Windows.Visibility.Collapsed;

                if (m_applicationSettings.Contains(App.LSKEY_PODCAST_EPISODE_PLAYING_ID))
                {
                    this.NoPlayHistoryText.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    this.NoPlayHistoryText.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                this.PlayHistory.Visibility = System.Windows.Visibility.Visible;
                this.NoPlayHistoryText.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        void PodcastPlayer_PodcastPlayerStarted(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/PodcastPlayerView.xaml", UriKind.Relative));
        }

        private void downloadListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            int episodeDownloads = EpisodeDownloadList.Items.Count;
            switch (episodeDownloads)
            {
                case 0:
                    EpisodesDownloadingText.Visibility = Visibility.Visible;
                    break;
                case 1:
                    EpisodesDownloadingText.Visibility = Visibility.Collapsed;
                    break;
                default:
                    EpisodesDownloadingText.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void AddSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            bool allowAddSubscription = true;
            if (App.IsTrial)
            {
                if (App.mainViewModels.PodcastSubscriptions.Count >= TRIAL_SUBSCRIPTION_LIMIT)
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
                MessageBox.Show("You have reached the limit of podcast subscriptions for this free trial of Podcatcher. Please purchase the full version from Windows Phone Marketplace.");
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0) { 
                PodcastSubscriptionModel tappedSubscription = e.AddedItems[0] as PodcastSubscriptionModel;
                Debug.WriteLine("Showing episodes for podcast. Name: " + tappedSubscription.PodcastName);
                NavigationService.Navigate(new Uri(string.Format("/Views/PodcastEpisodes.xaml?podcastId={0}", tappedSubscription.PodcastId), UriKind.Relative));
                this.SubscriptionsList.SelectedIndex = -1;  // Aaargh... stupid Silverlight.
                tappedSubscription.NewEpisodesCount = 0;
            }
        }

        private void NavigationPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ApplicationBar.IsVisible = this.NavigationPivot.SelectedIndex == 0 ? true : false;

            if (this.NavigationPivot.SelectedIndex == 2)
            {
                this.NowPlaying.SetupNowPlayingView();
            }
        }

        private void AboutSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AboutView.xaml", UriKind.Relative));
        }

        private void SettingsIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/SettingsView.xaml", UriKind.Relative));
        }

        private void handleShowReviewPopup()
        {
            if (m_applicationSettings.Contains(App.LSKEY_PODCATCHER_STARTS))
            {
                int podcatcherStarts = (int)m_applicationSettings[App.LSKEY_PODCATCHER_STARTS];
                if (podcatcherStarts > App.PODCATCHER_NEW_STARTS_BEFORE_SHOWING_REVIEW)
                {
                    m_applicationSettings.Remove(App.LSKEY_PODCATCHER_STARTS);

                    if (MessageBox.Show("Would you now like to review Podcatcher on Windows Phone Marketplace?",
                                        "I hope you are enjoying Podcatcher!",
                                        MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                        marketplaceReviewTask.Show();
                    }
                }
                else
                {
                    m_applicationSettings.Remove(App.LSKEY_PODCATCHER_STARTS);
                    m_applicationSettings.Add(App.LSKEY_PODCATCHER_STARTS, ++podcatcherStarts);
                }

                m_applicationSettings.Save();
            }
        }

        private void ExportSubscriptionsMenuItem_Click(object sender, EventArgs e)
        {
            String exportNotificationText = "";
            using (var db = new PodcastSqlModel())
            {
                if (db.settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportToSkyDrive)
                {
                    exportNotificationText = "This will export your podcast subscriptions information in OPML format to your SkyDrive account. Do you want to continue?";
                }
                else if (db.settings().SelectedExportIndex == (int)SettingsModel.ExportMode.ExportViaEmail)
                {
                    exportNotificationText = "This will export your podcast subscriptions information in OPML format via email. Do you want to continue?";
                }
            }

            if (MessageBox.Show(exportNotificationText,
                                "Export subscriptions in OPML format",
                                MessageBoxButton.OKCancel) == MessageBoxResult.OK) 
            {
                PodcastSubscriptionsManager.getInstance().exportSubscriptions();
            }
        }
    }
}
