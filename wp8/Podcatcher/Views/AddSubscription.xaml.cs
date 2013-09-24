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
using Coding4Fun.Toolkit.Controls;
using System.Windows.Media.Imaging;
using Podcatcher.ViewModels;

namespace Podcatcher.Views
{
    public partial class AddSubscription : PhoneApplicationPage
    {
        internal static List<BrowsePodcastsGroupModel> PodcastGroups = new List<BrowsePodcastsGroupModel>();

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

        }

        /************************************* Private implementations *******************************/
        #region private
        private PodcastSubscriptionsManager m_subscriptionManager;

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
                NavigationService.Navigated += new System.Windows.Navigation.NavigatedEventHandler(NavigationService_Navigated);
                NavigationService.GoBack();
            }
        }

        void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService.Navigated -= new System.Windows.Navigation.NavigatedEventHandler(NavigationService_Navigated);
            }
        }

        private void subscriptionManager_OnPodcastChannelRequiresAuthentication(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();
            NavigationService.Navigate(new Uri(string.Format("/Views/PodcastSubscriptionCredentials.xaml?url={0}", e.podcastFeedRSSUri.ToString()), UriKind.Relative));
        }

        #endregion


        private void addPodcastFromURL_clicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/AddPodcastFromURL.xaml"), UriKind.Relative));
        }

        private void importPodcastsFromGPodder_clicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/ImportPodcastsFromGPodder.xaml"), UriKind.Relative));
        }

        private void importViaOPML_clicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/ImportPodcastsViaOPML.xaml"), UriKind.Relative));
        }
    }
}