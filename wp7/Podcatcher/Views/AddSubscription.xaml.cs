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

            m_subscriptionManager.OnGPodderImportStarted
                += new SubscriptionManagerHandler(subscriptionManager_OnGPodderImportStarted);
            m_subscriptionManager.OnGPodderImportFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnGPodderImportFinished);
            m_subscriptionManager.OnGPodderImportFinishedWithError
                += new SubscriptionManagerHandler(subscriptionManager_OnGPodderImportFinishedWithError);
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