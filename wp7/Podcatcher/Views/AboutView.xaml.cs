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
using Microsoft.Phone.Tasks;
using System.Reflection;

namespace Podcatcher.Views
{
    public partial class AboutView : PhoneApplicationPage
    {
        public AboutView()
        {
            InitializeComponent();
            
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            Version thisVersion = nameHelper.Version;
            version.Text = String.Format("{0}.{1}.{2}.{3}", thisVersion.Major, thisVersion.Minor, thisVersion.Build, thisVersion.Revision);

            setUIIfPurchased();
            this.AnimatedTitleText.Begin();
        }

        private void setUIIfPurchased()
        {
            if (App.IsTrial)
            {
                this.PurchasedText.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.PurchasedText.Visibility = Visibility.Visible;
                this.PurchaseButton.Content = "Give a review";
            }
        }

        private void PurchaseButton_Click(object sender, RoutedEventArgs e)
        {

            // pop up the link to rate and review the app
            if (App.IsTrial)
            {
                MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
                marketplaceDetailTask.ContentIdentifier = "5d5cebe9-420a-4566-a468-04c94aa34d93";
                marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;
                marketplaceDetailTask.Show();
            }
            else
            {
                // Let people review.
                MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                marketplaceReviewTask.Show();
            }

        }
    }
}