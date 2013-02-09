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
using Coding4Fun.Phone.Controls;
using System.Windows.Media.Imaging;

namespace Podcatcher.Views
{
    public partial class AddSubscription : PhoneApplicationPage
    {
        /************************************* Public implementations *******************************/
        public AddSubscription()
        {
            InitializeComponent();

            m_subscriptionManager = PodcastSubscriptionsManager.getInstance();

            m_subscriptionManager.OnPodcastChannelAddStarted
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelAddStarted);
            m_subscriptionManager.OnPodcastChannelAddFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelAddFinished);
            m_subscriptionManager.OnPodcastChannelAddFinishedWithError
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelAddFinishedWithError);
            m_subscriptionManager.OnPodcastChannelRequiresAuthentication
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelRequiresAuthentication);

            m_subscriptionManager.OnGPodderImportStarted
                += new SubscriptionManagerHandler(subscriptionManager_OnGPodderImportStarted);
            m_subscriptionManager.OnGPodderImportFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnGPodderImportFinished);
            m_subscriptionManager.OnGPodderImportFinishedWithError
                += new SubscriptionManagerHandler(subscriptionManager_OnGPodderImportFinishedWithError);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (App.IsTrial)
            {
                this.gpodderDisclaimer.Visibility = System.Windows.Visibility.Collapsed;
                this.gpodderNotAvailable.Visibility = System.Windows.Visibility.Visible;
                this.gpodderPassword.IsEnabled = false;
                this.gpodderUsername.IsEnabled = false;
                this.importFromGpodderButton.IsEnabled = false;
            }
            else
            {
                this.gpodderDisclaimer.Visibility = System.Windows.Visibility.Visible;
                this.gpodderNotAvailable.Visibility = System.Windows.Visibility.Collapsed;
                this.gpodderPassword.IsEnabled = true;
                this.gpodderUsername.IsEnabled = true;
                this.importFromGpodderButton.IsEnabled = true;
            }
        }

        /************************************* Private implementations *******************************/
        #region private
        private PodcastSubscriptionsManager m_subscriptionManager;

        private void addFromUrlButton_Click(object sender, RoutedEventArgs e)
        {
            m_subscriptionManager.addSubscriptionFromURL(addFromUrlInput.Text);
        }

        private void subscriptionManager_OnPodcastChannelAddStarted(object source, SubscriptionManagerArgs e)
        {
            ProgressText.Text = "Subscribing";
            progressOverlay.Show();
        }

        private void subscriptionManager_OnPodcastChannelAddFinishedWithError(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();

            ToastPrompt toast = new ToastPrompt();
            toast.Title = "Error";
            toast.Message = e.message;

            toast.Show();
        }

        private void subscriptionManager_OnPodcastChannelAddFinished(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void subscriptionManager_OnPodcastChannelRequiresAuthentication(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();
            NavigationService.Navigate(new Uri(string.Format("/Views/PodcastSubscriptionCredentials.xaml?url={0}", e.podcastFeedRSSUri.ToString()), UriKind.Relative));
        }

        private void importFromGpodderButton_Click(object sender, RoutedEventArgs e)
        {
            NetworkCredential nc = new NetworkCredential(gpodderUsername.Text, gpodderPassword.Password);
            m_subscriptionManager.importFromGpodderWithCredentials(nc);
        }

        private void subscriptionManager_OnGPodderImportStarted(object source, SubscriptionManagerArgs e)
        {
            ProgressText.Text = "Importing from gPodder...";
            progressOverlay.Show();
        }

        private void subscriptionManager_OnGPodderImportFinished(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            progressOverlay.Hide();
        }

        private void subscriptionManager_OnGPodderImportFinishedWithError(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();

            ToastPrompt toast = new ToastPrompt();
            toast.Title = "Error";
            toast.Message = e.message;

            toast.Show();
        }


        #endregion
    }
}