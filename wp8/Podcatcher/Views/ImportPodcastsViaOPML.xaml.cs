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

namespace Podcatcher.Views
{
    public partial class ImportPodcastsViaOPML : PhoneApplicationPage
    {
        private PodcastSubscriptionsManager m_subscriptionManager = PodcastSubscriptionsManager.getInstance();

        public ImportPodcastsViaOPML()
        {
            InitializeComponent();

            if (App.IsTrial)
            {
                this.opmlDisclaimer.Visibility = System.Windows.Visibility.Collapsed;
                this.opmlNotAvailable.Visibility = System.Windows.Visibility.Visible;
                this.opmlUrl.IsEnabled = false;
                this.importFromOpmlUrl.IsEnabled = false;
            }
            else
            {
                this.opmlDisclaimer.Visibility = System.Windows.Visibility.Visible;
                this.opmlNotAvailable.Visibility = System.Windows.Visibility.Collapsed;
                this.opmlUrl.IsEnabled = true;
                this.importFromOpmlUrl.IsEnabled = true;
            }

            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAddStarted += new SubscriptionManagerHandler(ImportPodcastViaOPML_OnPodcastChannelAddStarted);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAddFinishedWithError += new SubscriptionManagerHandler(ImportPodcastViaOPML_OnPodcastChannelAddFinishedWithError);
        }

        void ImportPodcastViaOPML_OnPodcastChannelAddFinishedWithError(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Visibility = System.Windows.Visibility.Collapsed;
        }

        void ImportPodcastViaOPML_OnPodcastChannelAddStarted(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Visibility = System.Windows.Visibility.Visible;
        }

        private void importFromOpmlUrl_Click(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionsManager.getInstance().addSubscriptionFromOPMLFile(opmlUrl.Text);
        }

    }
}