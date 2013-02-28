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
using Podcatcher.ViewModels;
using System.Collections.ObjectModel;
using Microsoft.Phone.Shell;

namespace Podcatcher.Views
{
    public partial class PodcastEpisodes : PhoneApplicationPage
    {

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
        }
        
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            int podcastId = int.Parse(NavigationContext.QueryString["podcastId"]);
            using (var db = new PodcastSqlModel())
            {
                m_subscription = db.subscriptionModelForIndex(podcastId);
            }

            this.DataContext = m_subscription;

            m_subscription.PodcastCleanStarted -= new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanStarted);
            m_subscription.PodcastCleanFinished -= new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanFinished);

            m_subscription.PodcastCleanStarted += new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanStarted);
            m_subscription.PodcastCleanFinished += new PodcastSubscriptionModel.SubscriptionModelHandler(m_subscription_PodcastCleanFinished);

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

            // Delete listened episodes.
            using (var db = new PodcastSqlModel())
            {
                if (db.settings().IsAutoDelete)
                {
                    db.startOldEpisodeCleanup(m_subscription);
                }
            }

            // Clean old episodes from the listing.
            if (SettingsModel.keepNumEpisodesForSelectedIndex(m_subscription.SubscriptionSelectedKeepNumEpisodesIndex) != SettingsModel.KEEP_ALL_EPISODES)
            {
                m_subscription.cleanOldEpisodes(SettingsModel.keepNumEpisodesForSelectedIndex(m_subscription.SubscriptionSelectedKeepNumEpisodesIndex));
            }
        }

        /************************************* Priovate implementations *******************************/
        #region private

        private PodcastSubscriptionModel m_subscription;
        
        #endregion

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/SubscriptionSettings.xaml?podcastId={0}", m_subscription.PodcastId), UriKind.Relative));
        }

        private void NavigationPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ApplicationBar.IsVisible = this.NavigationPivot.SelectedIndex == 0 ? true : false;
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
                        if (episode.SavedPlayPos > 0)
                        {
                            episode.markAsListened();
                        }
                    }
                    db.SubmitChanges();
                }

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
                        }
                    }
                    db.SubmitChanges();
                }
            }
        }
    }
}