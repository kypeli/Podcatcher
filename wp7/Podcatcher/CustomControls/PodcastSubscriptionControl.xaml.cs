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
using System.Windows.Media.Imaging;

namespace Podcatcher
{
    public partial class PodcastSubscriptionControl : UserControl
    {
        public PodcastSubscriptionControl()
        {
            // Required to initialize variables
            InitializeComponent();
        }

        void PodcastSubscriptionControl_Unloaded(object sender, RoutedEventArgs e)
        {
            BitmapImage i = this.Logo.Source as BitmapImage;
            i.UriSource = null;
            this.Logo = null;
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
                if (myIsolatedStorage.FileExists(subscriptionToPin.PodcastLogoLocalLocation) == false) 
                {
                    Debug.WriteLine("Podcast logo not found. Cannot pin.");
                    App.showNotificationToast("Podcast logo not found. Cannot pin.");
                    return;
                }

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