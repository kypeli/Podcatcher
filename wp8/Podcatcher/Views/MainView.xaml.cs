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
using System.Threading;
using Microsoft.Phone.Reactive;

namespace Podcatcher
{
    public partial class MainView : PhoneApplicationPage
    {
        private const int PODCAST_PLAYER_PIVOR_INDEX = 2;
        private const int TRIAL_SUBSCRIPTION_LIMIT   = 2;
        
        // Pivot page indexes
        private const int PIVOT_INDEX_MAINVIEW  = 0;
        private const int PIVOT_INDEX_PLAYER    = 1;
        private const int PIVOT_INDEX_PLAYLIST  = 2;
        private const int PIVOT_INDEX_DOWNLOADS = 3;


        private PodcastEpisodesDownloadManager m_episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();
        private PodcastSubscriptionsManager m_subscriptionsManager;
        private PodcastPlaybackManager m_playbackManager;
        private IsolatedStorageSettings m_applicationSettings = null;
        private ObservableCollection<PodcastSubscriptionModel> m_subscriptions = App.mainViewModels.PodcastSubscriptions;
        private List<ApplicationBarIconButton> m_playerButtons = new List<ApplicationBarIconButton>();

        // Player app bar buttons
        private ApplicationBarIconButton rewPlayerButton = new ApplicationBarIconButton(new Uri("/Images/Light/rew.png", UriKind.Relative))
        {
            Text = "Rew"            
        };
        private ApplicationBarIconButton playPlayerButton = new ApplicationBarIconButton(new Uri("/Images/Light/play.png", UriKind.Relative))
        {
            Text = "Play"
        };
        private ApplicationBarIconButton pausePlayerButton = new ApplicationBarIconButton(new Uri("/Images/Light/pause.png", UriKind.Relative))
        {
            Text = "Pause"
        };
        private ApplicationBarIconButton stopPlayerButton = new ApplicationBarIconButton(new Uri("/Images/Light/stop.png", UriKind.Relative))
        {
            Text = "Stop"            
        };
        private ApplicationBarIconButton ffPlayerButton = new ApplicationBarIconButton(new Uri("/Images/Light/ff.png", UriKind.Relative))
        {
            Text = "FF"
        };

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
            m_playbackManager.OnPodcastStartedPlaying += new EventHandler(PodcastPlayer_PodcastPlayerStarted);
            m_playbackManager.OnPodcastStoppedPlaying += new EventHandler(PodcastPlayer_PodcastPlayerStopped);

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

            CheckLicense();

            // Setup player button events.
            rewPlayerButton.Click += rewButtonClicked;
            pausePlayerButton.Click += playButtonClicked;
            playPlayerButton.Click += playButtonClicked;
            stopPlayerButton.Click += stopButtonClicked;
            ffPlayerButton.Click += ffButtonClicked;
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
                        sub.reloadUnplayedPlayedEpisodes();
                        sub.reloadPartiallyPlayedEpisodes();
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

            // Update all episodes to the latest list.
            App.mainViewModels.LatestEpisodesListProperty = new ObservableCollection<PodcastEpisodeModel>();

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

            // Update all episodes to the latest list.
            App.mainViewModels.LatestEpisodesListProperty = new ObservableCollection<PodcastEpisodeModel>();
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
            NowPlaying.SetupNowPlayingView();

            m_playerButtons.Clear();
            m_playerButtons.Add(rewPlayerButton);
            m_playerButtons.Add(pausePlayerButton);
            m_playerButtons.Add(stopPlayerButton);
            m_playerButtons.Add(ffPlayerButton);
            updatePlayerButtonsInApplicationBar(m_playerButtons);

            this.ApplicationBar.IsVisible = PodcastPlaybackManager.getInstance().isCurrentlyPlaying() ? true : false;

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
            // TODO
            // NavigationService.Navigate(new Uri("/Views/PodcastPlayerView.xaml", UriKind.Relative));
            this.NowPlaying.SetupNowPlayingView();
            this.NowPlaying.Visibility = System.Windows.Visibility.Visible;
            Scheduler.Dispatcher.Schedule(() =>     // We have to do this hack so that Windows Phone's UI doesn't get confused when we add the application bar. 
            {
                this.ApplicationBar.IsVisible = true;
                Debug.WriteLine("Showing application bar.");
            }, TimeSpan.FromSeconds(1));
        }

        void PodcastPlayer_PodcastPlayerStopped(object sender, EventArgs e)
        {
            this.NowPlaying.Visibility = System.Windows.Visibility.Collapsed;
            this.ApplicationBar.IsVisible = false;
            Debug.WriteLine("Hiding application bar.");
        }

        private void updatePlayerButtonsInApplicationBar(List<ApplicationBarIconButton> playerButtons)
        {
            if (NavigationPivot.SelectedIndex != PIVOT_INDEX_PLAYER)
            {
                return;
            }

            this.ApplicationBar.MenuItems.Clear();
            this.ApplicationBar.Buttons.Clear();

            foreach (ApplicationBarIconButton button in playerButtons)
            {
                this.ApplicationBar.Buttons.Add(button);
            }

            this.ApplicationBar.IsVisible = (this.ApplicationBar.Buttons.Count > 0) ? true : false;
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
//                this.SubscriptionsList.SelectedIndex = -1;  // Aaargh... stupid Silverlight.
                tappedSubscription.NewEpisodesCount = 0;
            }
        }

        private void NavigationPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            setupApplicationBarForIndex(this.NavigationPivot.SelectedIndex);
        }

        private void setupApplicationBarForIndex(int index)
        {
            bool applicationBarVisible = false;
            switch (index)
            {
                // Subscription list view.
                case PIVOT_INDEX_MAINVIEW:
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

                case PIVOT_INDEX_PLAYER:
                    updatePlayerButtonsInApplicationBar(m_playerButtons);
                    applicationBarVisible = PodcastPlaybackManager.getInstance().isCurrentlyPlaying() ? true : false;
                    break;

                // Play queue view
                case PIVOT_INDEX_PLAYLIST:
                    applicationBarVisible = true;
                    this.ApplicationBar.MenuItems.Clear();
                    this.ApplicationBar.Buttons.Clear();

                    if (App.mainViewModels.PlayQueue.Count > 0)
                    {
                        ApplicationBarMenuItem queueItem = new ApplicationBarMenuItem() { Text = "Clear play queue" };
                        queueItem.Click += new EventHandler(ClearPlayqueue_Click);
                        this.ApplicationBar.MenuItems.Add(queueItem);
                    }
                    break;
            }

            this.ApplicationBar.IsVisible = applicationBarVisible;
        }

        private void AboutSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AboutView.xaml", UriKind.Relative));
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

        /// <summary>
        /// Check the current license information for this application
        /// </summary>
        private static void CheckLicense()
        {
            // When debugging, we want to simulate a trial mode experience. The following conditional allows us to set the _isTrial 
            // property to simulate trial mode being on or off. 
#if DEBUG
            string message = "Press 'OK' to simulate trial mode. Press 'Cancel' to run the application in normal mode.";
            if (MessageBox.Show(message, "Debug Trial",
                 MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                App.isTrial = true;
            }
            else
            {
                App.isTrial = false;
            }
#else
            App.isTrial = App.licenseInfo.IsTrial();
#endif
        }

        private void rewButtonClicked(object sender, EventArgs e)
        {
            BackgroundAudioPlayer player = BackgroundAudioPlayer.Instance;
            if (player != null && player.PlayerState == PlayState.Playing)
            {
                player.Position = (player.Position.TotalSeconds - 30 >= 0) ?
                                    TimeSpan.FromSeconds(player.Position.TotalSeconds - 30) :
                                    TimeSpan.FromSeconds(0);
            }
        }

        private void playButtonClicked(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                // Paused
                BackgroundAudioPlayer.Instance.Pause();
               // PodcastPlayer.setupUIForEpisodePaused();
                //       PlayButtonImage.Source = m_playButtonBitmap;
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = new Uri("/Images/Light/play.png", UriKind.Relative);
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).Text = "Play";
            }
            else if (BackgroundAudioPlayer.Instance.Track != null)
            {
                // Playing
                BackgroundAudioPlayer.Instance.Play();
               // PodcastPlayer.setupUIForEpisodePlaying();
                //            PlayButtonImage.Source = m_pauseButtonBitmap;
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = new Uri("/Images/Light/pause.png", UriKind.Relative);
                (this.ApplicationBar.Buttons[1] as ApplicationBarIconButton).Text = "Pause";
            }
            else
            {
                Debug.WriteLine("No track currently set. Trying to setup currently playing episode as track...");
                PodcastEpisodeModel ep = PodcastPlaybackManager.getInstance().CurrentlyPlayingEpisode;
                if (ep != null)
                {
                    PodcastPlaybackManager.getInstance().play(ep);
                }
                else
                {
                    Debug.WriteLine("Error: No currently playing track either! Giving up...");
                    App.showErrorToast("Something went wrong. Cannot play the track.");
                 //   PodcastPlayer.showNoPlayerLayout();
                }
            }
        }

        private void stopButtonClicked(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Stopped)
            {
                // We are already stopped (playback ended or something). Let's update the episode state.
                PodcastPlaybackManager.getInstance().CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded;
            }
            else
            {
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing
                    || BackgroundAudioPlayer.Instance.PlayerState == PlayState.Paused)
                {
                    BackgroundAudioPlayer.Instance.Stop();
                }
            }

           // PodcastPlayer.PlaybackStopped();
        }

        private void ffButtonClicked(object sender, EventArgs e)
        {
            BackgroundAudioPlayer player = BackgroundAudioPlayer.Instance;
            if (player != null && player.PlayerState == PlayState.Playing)
            {
                player.Position = (player.Position.TotalSeconds + 30 < player.Track.Duration.TotalSeconds) ? TimeSpan.FromSeconds(player.Position.TotalSeconds + 30) :
                                                                                                             TimeSpan.FromSeconds(player.Track.Duration.TotalSeconds);
            }
        }

        private void SubscriptionsList_RefreshRequested(object sender, EventArgs e)
        {

        }

    }
}
