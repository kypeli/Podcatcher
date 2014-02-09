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
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Net.Http;

namespace Podcatcher.Views
{
    public partial class PodcastSubscriptionIntroduction : PhoneApplicationPage
    {
        public PodcastSubscriptionIntroduction()
        {
            InitializeComponent();
        }

        async protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            string podcastUrl = (string)NavigationContext.QueryString["podcastUrl"];

            try
            {
                Uri podcastUri = new Uri(podcastUrl);
                String podcastPageXML = await new HttpClient().GetStringAsync(podcastUri);

                PodcastSubscriptionModel subscription = PodcastFactory.podcastModelFromRSS(podcastPageXML);
                PodcastName.Text = subscription.PodcastName;
                PodcastIcon.Source = new BitmapImage(subscription.PodcastLogoUrl);
                PodcastDescription.Text = subscription.PodcastDescription;
            }
            catch (UriFormatException)
            {
                Console.WriteLine("Malformed podcast address.");
                App.showErrorToast("Cannot show information. Malformed web address.");
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Could not connect to the XML feed.");
                App.showErrorToast("Cannot fetch podcast information.");
            }
        }
    }
}