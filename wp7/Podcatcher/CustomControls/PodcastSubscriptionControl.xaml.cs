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
        public PodcastSubscriptionControl()
        {
            // Required to initialize variables
            InitializeComponent();
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
            PodcastSubscriptionModel subscriptionToPin = (sender as MenuItem).DataContext as PodcastSubscriptionModel;

            // Copy the logo file to tile's shared location.
            String tileImageLocation = "Shared/ShellContent/" + subscriptionToPin.PodcastLogoLocalLocation.Split('/')[1];
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(tileImageLocation) == false)
                {
                    myIsolatedStorage.CopyFile(subscriptionToPin.PodcastLogoLocalLocation,
                                               tileImageLocation);
                }
            }

            // Setup data for the live tile.
            StandardTileData tileData = new StandardTileData();
            tileData.BackgroundImage = new Uri("isostore:/" + tileImageLocation, UriKind.Absolute);
            tileData.Title = subscriptionToPin.PodcastName;

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            String subscriptionLatestEpisodeKey = App.LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE + subscriptionToPin.PodcastId;
            if (settings.Contains(subscriptionLatestEpisodeKey) == false)
            {
                settings.Add(subscriptionLatestEpisodeKey, ""); // Create empty key so we know the subscription is pinned.
            }

            subscriptionToPin.EpisodesManager.updatePinnedInformation();

            try
            {
                Uri tileUri = new Uri(string.Format("/Views/PodcastEpisodes.xaml?podcastId={0}&forceUpdate=true", subscriptionToPin.PodcastId), UriKind.Relative);
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