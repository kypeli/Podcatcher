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
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;
using System.Collections.ObjectModel;
using Microsoft.Phone.Shell;

namespace Podcatcher.Views
{
    public partial class PodcastEpisodes : PhoneApplicationPage 
    {
        private int m_podcastId = 0;        

        /************************************* Public implementations *******************************/
        public PodcastEpisodes()
        {
            InitializeComponent();
        }

        void m_subscription_PodcastCleanStarted()
        {
            cleanProgressOverlay.Visibility = Visibility.Visible;
        }

        void m_subscription_PodcastCleanFinished()
        {
            cleanProgressOverlay.Visibility = Visibility.Collapsed;
            using (var db = new PodcastSqlModel()) {
                m_subscription = db.subscriptionModelForIndex(m_podcastId);
            }
            this.DataContext = m_subscription;
            this.EpisodeList.ItemsSource = m_subscription.EpisodesPublishedDescending;
        }
      
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            m_podcastId = int.Parse(NavigationContext.QueryString["podcastId"]);
            using (var db = new PodcastSqlModel())
            {
                m_subscription = db.subscriptionModelForIndex(m_podcastId);

                if (db.settings().IsAutoDelete)
                {
                    db.cleanListenedEpisodes(m_subscription);
                }
            }

            m_playableEpisodes = m_subscription.PlayableEpisodes;
            this.DataContext = m_subscription;

            PodcastSubscriptionsManager.getInstance().NewPlayableEpisode += new PodcastSubscriptionsManager.EpisodesEventHandler(m_subscription_NewPlayableEpisode);
            PodcastSubscriptionsManager.getInstance().RemovedPlayableEpisode += new PodcastSubscriptionsManager.EpisodesEventHandler(m_subscription_RemovedPlayableEpisode);
            
            bool forceUpdate = false;
            try
            {
                forceUpdate = String.IsNullOrEmpty(NavigationContext.QueryString["forceUpdate"]) == false
                    && bool.Parse(NavigationContext.QueryString["forceUpdate"]);
            }
            catch (KeyNotFoundException)
            {
                forceUpdate = false;
            }

            if (forceUpdate)
            {
                ShellTile pinnedSubscriptionTile = m_subscription.getSubscriptionsLiveTile();
                if (pinnedSubscriptionTile != null)
                {
                    StandardTileData tileData = new StandardTileData();
                    tileData.Count = 0;
                    tileData.BackTitle = "";
                    pinnedSubscriptionTile.Update(tileData);
                }

                PodcastSubscriptionsManager.getInstance().refreshSubscription(m_subscription);
            }

            m_subscription.PodcastCleanStarted -= new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanStarted);
            m_subscription.PodcastCleanFinished -= new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanFinished);

            m_subscription.PodcastCleanStarted += new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanStarted);
            m_subscription.PodcastCleanFinished += new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanFinished);

            // Clean old episodes from the listing.
            if (SettingsModel.keepNumEpisodesForSelectedIndex(m_subscription.SubscriptionSelectedKeepNumEpisodesIndex) != SettingsModel.KEEP_ALL_EPISODES)
            {
                m_subscription.cleanOldEpisodes(SettingsModel.keepNumEpisodesForSelectedIndex(m_subscription.SubscriptionSelectedKeepNumEpisodesIndex));
            }

            if (App.episodeDownloadManager == null)
            {
                App.episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();
            }

            App.episodeDownloadManager.OnPodcastEpisodeDownloadStateChanged -= new PodcastDownloadManagerHandler(episodeDownloadManager_PodcastEpisodeDownloadStateChanged);
            App.episodeDownloadManager.OnPodcastEpisodeDownloadStateChanged += new PodcastDownloadManagerHandler(episodeDownloadManager_PodcastEpisodeDownloadStateChanged);
        }

        void episodeDownloadManager_PodcastEpisodeDownloadStateChanged(object source, PodcastDownloadManagerArgs args)
        {
            int episodeId = args.episodeId;
            PodcastEpisodeModel episode = this.EpisodeList.Items.Cast<PodcastEpisodeModel>().FirstOrDefault(e => e.EpisodeId == episodeId);
            if (episode != null)
            {
                episode.EpisodeDownloadState = args.downloadState;
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            using (var db = new PodcastSqlModel())
            {
                PodcastSubscriptionModel sub = db.Subscriptions.First(s => s.PodcastId == m_subscription.PodcastId);
                sub.NewEpisodesCount = 0;
                db.SubmitChanges();
            }                

            PodcastSubscriptionsManager.getInstance().NewPlayableEpisode -= new PodcastSubscriptionsManager.EpisodesEventHandler(m_subscription_NewPlayableEpisode);
        }

        /************************************* Priovate implementations *******************************/
        private PodcastSubscriptionModel m_subscription;
        
        private void m_subscription_NewPlayableEpisode(PodcastEpisodeModel e)
        {
            addEpisodeToPlayableList(e);
        }

        private void m_subscription_RemovedPlayableEpisode(PodcastEpisodeModel e)
        {
            removeEpisodeFromPlayableList(e);
        }

        private ObservableCollection<PodcastEpisodeModel> m_playableEpisodes = null;
        private void addEpisodeToPlayableList(PodcastEpisodeModel episode)
        {
            List<PodcastEpisodeModel> tmpList = m_playableEpisodes.ToList();
            tmpList.Add(episode);
            tmpList.Sort(CompareEpisodesByPublishDate);

            m_playableEpisodes = new ObservableCollection<PodcastEpisodeModel>(tmpList);
            m_subscription.PlayableEpisodes = m_playableEpisodes;
        }

        private void removeEpisodeFromPlayableList(PodcastEpisodeModel episode)
        {
            List<PodcastEpisodeModel> tmpList = m_playableEpisodes.ToList();
            for (int i = 0; i<tmpList.Count; i++) 
            {
                if (tmpList[i].EpisodeId == episode.EpisodeId)
                {
                    tmpList.RemoveAt(i);
                    break;
                }
            }

            tmpList.Sort(CompareEpisodesByPublishDate);

            m_playableEpisodes = new ObservableCollection<PodcastEpisodeModel>(tmpList);
            m_subscription.PlayableEpisodes = m_playableEpisodes;
        }

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/SubscriptionSettings.xaml?podcastId={0}", m_subscription.PodcastId), UriKind.Relative));
        }

        private void NavigationPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            setupApplicationBarForIndex(this.NavigationPivot.SelectedIndex);
        }

        private void setupApplicationBarForIndex(int index)
        {
            this.ApplicationBar.MenuItems.Clear();
            this.ApplicationBar.Buttons.Clear();

            switch (index)
            {
                // Episode listing
                case 0:
                    ApplicationBarIconButton settingsAppbarIcon = new ApplicationBarIconButton()
                    {
                        IconUri = new Uri("/Images/settings.png", UriKind.Relative), 
                        Text="Settings"                                                                              
                    };
                    settingsAppbarIcon.Click += new EventHandler(ApplicationBarSettingsButton_Click);
                    this.ApplicationBar.Buttons.Add(settingsAppbarIcon);

                    ApplicationBarMenuItem item = new ApplicationBarMenuItem() { Text = "Mark all as listened" };
                    item.Click += new EventHandler(MarkAllListened_Click);
                    this.ApplicationBar.MenuItems.Add(item);

                    item = new ApplicationBarMenuItem() { Text = "Delete all downloads" };
                    item.Click += new EventHandler(DeleteAllDownloads_Click);
                    this.ApplicationBar.MenuItems.Add(item);
                    break;

                // Downloaded listing.
                case 1:
                    if (m_playableEpisodes.Count > 0)
                    {
                        ApplicationBarMenuItem downloadedItem = new ApplicationBarMenuItem() { Text = "Add all to play queue" };
                        downloadedItem.Click += new EventHandler(AddDownloadedToPlayQueue_Clicked);
                        this.ApplicationBar.MenuItems.Add(downloadedItem);
                    }
                    break;
            }
        }

        private int CompareEpisodesByPublishDate(PodcastEpisodeModel e1, PodcastEpisodeModel e2) 
        {
            if (e1.EpisodePublished > e2.EpisodePublished)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        private void AddDownloadedToPlayQueue_Clicked(object sender, EventArgs e)
        {
            PodcastPlaybackManager playbackManager = PodcastPlaybackManager.getInstance();
            playbackManager.addToPlayqueue(m_playableEpisodes);
            App.showNotificationToast(m_playableEpisodes.Count + " podcasts added to playlist.");
        }

        private void MarkAllListened_Click(object sender, EventArgs e) 
        {
            if (MessageBox.Show("Are you sure you want to mark all episodes as listened?",
                            "Are you sure?",
                            MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                List<PodcastEpisodeModel> episodes = null;
                using (var db = new PodcastSqlModel())
                {
                    episodes = db.episodesForSubscription(m_subscription);
                    foreach (PodcastEpisodeModel episode in episodes)
                    {
                        episode.markAsListened();
                    }
                    db.SubmitChanges();
                    PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(m_subscription);
                }

                using (var db = new PodcastSqlModel())
                {
                    m_subscription = db.subscriptionModelForIndex(m_podcastId);
                }

                this.DataContext = m_subscription;
                this.EpisodeList.ItemsSource = m_subscription.EpisodesPublishedDescending;

                App.showNotificationToast("All episodes marked as listened.");
            }
        }

        private void DeleteAllDownloads_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete all downloaded episodes in this subscription?",
                    "Delete?",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)                
            {
                List<PodcastEpisodeModel> episodes = null;
                using (var db = new PodcastSqlModel())
                {
                    episodes = db.episodesForSubscription(m_subscription);
                    foreach (PodcastEpisodeModel episode in episodes)
                    {
                        if (episode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
                        {
                            episode.deleteDownloadedEpisode();
                            PodcastSubscriptionsManager.getInstance().removedPlayableEpisode(episode);
                        }
                    }
                }

                using (var db = new PodcastSqlModel())
                {
                    m_subscription = db.subscriptionModelForIndex(m_podcastId);
                }
            }

            this.DataContext = m_subscription;
            this.EpisodeList.ItemsSource = m_subscription.EpisodesPublishedDescending;
        }
    }
}