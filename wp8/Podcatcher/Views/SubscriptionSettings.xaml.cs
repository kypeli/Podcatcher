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

namespace Podcatcher.Views
{
    public partial class SubscriptionSettings : PhoneApplicationPage
    {
        private PodcastSubscriptionModel m_subscription = null;

        public SubscriptionSettings()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            if (m_subscription == null)
            {
                int podcastId = int.Parse(NavigationContext.QueryString["podcastId"]);
                using (var db = new PodcastSqlModel())
                {
                    m_subscription = db.subscriptionModelForIndex(podcastId);
                }
            }

            this.DataContext = m_subscription;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            PodcastSubscriptionModel subscriptionDataContext = this.DataContext as PodcastSubscriptionModel;
            PodcastSubscriptionModel subscription = null;
            using (var db = new PodcastSqlModel())
            {
                subscription = db.subscriptionModelForIndex(m_subscription.PodcastId);
                subscription.SubscriptionSelectedKeepNumEpisodesIndex = subscriptionDataContext.SubscriptionSelectedKeepNumEpisodesIndex;
                subscription.IsSubscribed = subscriptionDataContext.IsSubscribed;
                subscription.IsAutoDownload = subscriptionDataContext.IsAutoDownload;
                subscription.SubscriptionIsDeleteEpisodes = subscriptionDataContext.SubscriptionIsDeleteEpisodes;

                db.SubmitChanges();
            }

            m_subscription = null;

            NavigationService.GoBack();
        }
    }
}