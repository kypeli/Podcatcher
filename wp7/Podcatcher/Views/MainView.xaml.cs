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
using Microsoft.Phone.Shell;
using Microsoft.Phone.BackgroundAudio;

namespace Podcatcher
{
    public partial class MainView : PhoneApplicationPage
    {
        private const int PODCAST_PLAYER_PIVOR_INDEX = 2;
        private const int TRIAL_SUBSCRIPTION_LIMIT   = 2;
        
        private PodcastEpisodesDownloadManager m_episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();
        private PodcastSubscriptionsManager m_subscriptionsManager;
        private PodcastPlaybackManager m_playbackManager;
        private IsolatedStorageSettings m_applicationSettings = null;
        private ObservableCollection<PodcastSubscriptionModel> m_subscriptions = App.mainViewModels.PodcastSubscriptions;

        public MainView()
        {
            InitializeComponent();

            this.DataContext = App.mainViewModels;

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
            m_playbackManager = PodcastPlaybackManager.getInstance();
            m_playbackManager.OnOpenPodcastPlayer += new EventHandler(PodcastPlayer_PodcastPlayerStarted);

            PodcastSubscriptionsManager.getInstance().OnPodcastChannelDeleteStarted
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelDeleteStarted);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelDeleteFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelDeleteFinished);

            PodcastSubscriptionsManager.getInstance().OnPodcastChannelPlayedCountChanged
                += new SubscriptionChangedHandler(subscriptionManager_OnPodcastChannelPlayedCountChanged);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAdded
                += new SubscriptionChangedHandler(subscriptionManager_OnPodcastChannelAdded);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelRemoved
                += new SubscriptionChangedHandler(subscriptionManager_OnPodcastChannelRemoved);

            SubscriptionsList.ItemsSource = m_subscriptions;

            if (m_subscriptions.Count > 0)
            {
                NoSubscriptionsLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoSubscriptionsLabel.Visibility = Visibility.Visible;
            }

            handleShowReviewPopup();

            // This is the earliest place that we can initialize the download manager so it can show the error toast
            // which is defined in App.
            App.episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();
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

        private void subscriptionManager_OnPodcastChannelPlayedCountChanged(PodcastSubscriptionModel s) 
        {
            Debug.WriteLine("Play status changed.");
            List<PodcastSubscriptionModel> subs = m_subscriptions.ToList();
            foreach (PodcastSubscriptionModel sub in subs) 
            {
                if (sub.PodcastId == s.PodcastId)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        sub.UnplayedEpisodes--;
                        sub.PartiallyPlayedEpisodes--;
                    });
                    break;
                }
            }
        }

        private void subscriptionManager_OnPodcastChannelRemoved(PodcastSubscriptionModel s) 
        {
            Debug.WriteLine("Subscription deleted.");

            List<PodcastSubscriptionModel> subs = m_subscriptions.ToList();
            for (int i = 0; i < subs.Count; i++)
            {
                if (subs[i].PodcastId == s.PodcastId)
                {
                    subs.RemoveAt(i);
                    break;
                }
            }

            m_subscriptions = new ObservableCollection<PodcastSubscriptionModel>(subs);
            this.SubscriptionsList.ItemsSource = m_subscriptions;

            if (m_subscriptions.Count > 0)
            {
                NoSubscriptionsLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoSubscriptionsLabel.Visibility = Visibility.Visible;
            }

        }

        private int PodcastSubscriptionSortComparator(PodcastSubscriptionModel s1, PodcastSubscriptionModel s2)
        {
            return s1.PodcastName.CompareTo(s2.PodcastName);
        }

        private void subscriptionManager_OnPodcastChannelAdded(PodcastSubscriptionModel s) 
        {
            Debug.WriteLine("Subscription added");

            List<PodcastSubscriptionModel> subs = m_subscriptions.ToList();
            subs.Add(s);
            subs.Sort(PodcastSubscriptionSortComparator);

            m_subscriptions = new ObservableCollection<PodcastSubscriptionModel>(subs);
            this.SubscriptionsList.ItemsSource = m_subscriptions;

            NoSubscriptionsLabel.Visibility = Visibility.Collapsed;
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
            this.EpisodeDownloadList.ItemsSource = m_episodeDownloadManager.EpisodeDownloadQueue;            
            this.NowPlaying.SetupNowPlayingView();


            if (App.mainViewModels.LatestEpisodesListProperty.Count == 0)
            {
                this.LatestEpisodesList.Visibility = System.Windows.Visibility.Collapsed;

                if (PodcastPlaybackManager.getInstance().CurrentlyPlayingEpisode != null)
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
                this.LatestEpisodesList.Visibility = System.Windows.Visibility.Visible;
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
            setupApplicationBarForIndex(this.NavigationPivot.SelectedIndex);

            if (this.NavigationPivot.SelectedIndex == 2)
            {
                this.NowPlaying.SetupNowPlayingView();
            }
        }

        private void setupApplicationBarForIndex(int index)
        {
            bool applicationBarVisible = false;
            switch (index)
            {
                // Subscription list view.
                case 0:
                    applicationBarVisible = true;
                    this.ApplicationBar.MenuItems.Clear();
                    this.ApplicationBar.Buttons.Clear();
                    ApplicationBarIconButton addAppbarIcon = new ApplicationBarIconButton()
                    {
                        IconUri = new Uri("/Images/appbar.add.rest.png", UriKind.Relative), 
                        Text="Add"                                                                                    
                    };
                    addAppbarIcon.Click += new EventHandler(AddSubscriptionIconButton_Click);
                    this.ApplicationBar.Buttons.Add(addAppbarIcon);

                    ApplicationBarMenuItem item = new ApplicationBarMenuItem() { Text = "Settings" };
                    item.Click += new EventHandler(SettingsIconButton_Click);
                    this.ApplicationBar.MenuItems.Add(item);

                    item = new ApplicationBarMenuItem() { Text = "Export subscriptions" };
                    item.Click += new EventHandler(ExportSubscriptionsMenuItem_Click);
                    this.ApplicationBar.MenuItems.Add(item);

                    item = new ApplicationBarMenuItem() { Text = "About" };
                    item.Click += new EventHandler(AboutSubscriptionIconButton_Click);
                    this.ApplicationBar.MenuItems.Add(item);

                    break;

                // Play queue view
                case 3:
                    applicationBarVisible = true;
                    this.ApplicationBar.MenuItems.Clear();
                    this.ApplicationBar.Buttons.Clear();

                    if (App.mainViewModels.PlayQueue.Count > 0)
                    {
                        ApplicationBarIconButton playQueueButton = new ApplicationBarIconButton()
                        {
                            Text = "Play queue"
                        };
                        setupQueueApplicationButton(playQueueButton);
                        playQueueButton.Click += new EventHandler(PlayQueue_Click);
                        this.ApplicationBar.Buttons.Add(playQueueButton);

                        ApplicationBarMenuItem queueItem = new ApplicationBarMenuItem() { Text = "Clear play queue" };
                        queueItem.Click += new EventHandler(ClearPlayqueue_Click);
                        this.ApplicationBar.MenuItems.Add(queueItem);
                    }
                    break;
            }

            this.ApplicationBar.IsVisible = applicationBarVisible;
        }

        private void setupQueueApplicationButton(ApplicationBarIconButton button) 
        { 
            BackgroundAudioPlayer bap = BackgroundAudioPlayer.Instance;
            Uri iconUri = bap.PlayerState != PlayState.Playing ? new Uri("/Images/Dark/play.png", UriKind.Relative) :
                                                                 new Uri("/Images/Dark/pause.png", UriKind.Relative);
            button.IconUri = iconUri;
            
            bap.PlayStateChanged -= new EventHandler(bap_PlayStateChanged);
            bap.PlayStateChanged += new EventHandler(bap_PlayStateChanged);
        }

        void bap_PlayStateChanged(object sender, EventArgs e)
        {
            setupQueueApplicationButton(this.ApplicationBar.Buttons[0] as ApplicationBarIconButton);
        }

        private void AboutSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AboutView.xaml", UriKind.Relative));
        }

        private void PlayQueue_Click(object sender, EventArgs e)
        {
            switch(BackgroundAudioPlayer.Instance.PlayerState) 
            {
                case PlayState.Playing:
                    BackgroundAudioPlayer.Instance.Pause();
                    break;
                case PlayState.Paused:
                    BackgroundAudioPlayer.Instance.Play();
                    break;
                default:
                    PodcastPlaybackManager.getInstance().startPlaylistPlayback();
                    break;
            }    
        }

        private void ClearPlayqueue_Click(object sender, EventArgs e)
        {
            PodcastPlaybackManager.getInstance().clearPlayQueue();
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

        private void PlayOrderChanged(object sender, SelectionChangedEventArgs e)
        {
            ListPickerItem selectedItem = (sender as ListPicker).SelectedItem as ListPickerItem;
            if (selectedItem == null)
            {
                return;
            }

            using (var db = new PodcastSqlModel()) 
            {
                PodcastPlaybackManager.getInstance().sortPlaylist(db.settings().PlaylistSortOrder);
            }
        }

        private void PodcastLatestControl_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
    }
}
