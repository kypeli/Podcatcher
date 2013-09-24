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
    public partial class PodcastSubscriptionCredentials : PhoneApplicationPage
    {
        public PodcastSubscriptionCredentials()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            String podcastUri = NavigationContext.QueryString["url"];
            NetworkCredential nc = new NetworkCredential();
            nc.UserName = this.Username.Text;
            nc.Password = this.Password.Password;
#if DEBUG
            nc.UserName = "user@example.com";
            nc.Password = "secret";
#endif
            PodcastSubscriptionsManager.getInstance().addSubscriptionFromURLWithCredentials(podcastUri, nc);
            NavigationService.GoBack();
        }
    }
}