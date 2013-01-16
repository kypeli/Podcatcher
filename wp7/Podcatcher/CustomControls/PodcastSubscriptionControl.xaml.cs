/**
 * Copyright (c) 2012, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the <organization> nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

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
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

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
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionModel subscriptionToDelete = (sender as MenuItem).DataContext as PodcastSubscriptionModel;
            Debug.WriteLine("Delete podcast subscription. Name: " + subscriptionToDelete.PodcastName);

            PodcastSubscriptionsManager subscriptionsManager = PodcastSubscriptionsManager.getInstance();
            subscriptionsManager.deleteSubscription(subscriptionToDelete);
        }

        private void MenuItemPin_Click(object sender, RoutedEventArgs e)
        {
            // Copy the logo file to tile's shared location.
            String tileImageLocation = "Shared/ShellContent/" + m_subscription.PodcastLogoLocalLocation.Split('/')[1];
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(tileImageLocation) == false)
                {
                    myIsolatedStorage.CopyFile(m_subscription.PodcastLogoLocalLocation,
                                               tileImageLocation);
                }
            }

            // Setup data for the live tile.
            StandardTileData tileData = new StandardTileData();
            tileData.BackgroundImage = new Uri("isostore:/" + tileImageLocation, UriKind.Absolute);
            tileData.Title = m_subscription.PodcastName;

            // Store information about the subscription for the background agent.
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            String subscriptionLatestEpisodeKey = App.LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + m_subscription.PodcastId;
            if (settings.Contains(subscriptionLatestEpisodeKey)) 
            {
                settings.Remove(subscriptionLatestEpisodeKey);
            }
            
            String subscriptionData = String.Format("{0}|{1}|{2}",
                                                    m_subscription.PodcastId,
                                                    m_subscription.Episodes[0].EpisodePublished.ToString("r"),
                                                    m_subscription.PodcastRSSUrl);
            settings.Add(subscriptionLatestEpisodeKey, subscriptionData);
            settings.Save();
            Debug.WriteLine("Storing latest episode publish date for subscription as: " + m_subscription.Episodes[0].EpisodePublished.ToString("r"));

            try
            {
                Uri tileUri = new Uri(string.Format("/Views/PodcastEpisodes.xaml?podcastId={0}&forceUpdate=true", m_subscription.PodcastId), UriKind.Relative);
                Debug.WriteLine(string.Format("Pinning to start: Image: {0} Title: {1} Navigation uri: {2}", tileData.BackgroundImage, tileData.Title, tileUri));
                ShellTile.Create(tileUri, tileData);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Could not pin to start screen. The subscription is already pinned.");
            }
        }
    }
}