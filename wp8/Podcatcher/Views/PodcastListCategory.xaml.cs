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
using Newtonsoft.Json;
using Podcatcher.ViewModels;
using System.Diagnostics;

namespace Podcatcher.Views
{
    public partial class PodcastListCategory : PhoneApplicationPage
    {
        private BrowsePodcastsGroupModel m_groupModel = null;
        public PodcastListCategory()
        {
            InitializeComponent();
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAddStarted += new SubscriptionManagerHandler(PodcastListCategory_OnPodcastChannelAddStarted);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAddFinishedWithError += new SubscriptionManagerHandler(PodcastListCategory_OnPodcastChannelAddFinishedWithError);
        }

        void PodcastListCategory_OnPodcastChannelAddFinishedWithError(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Visibility = System.Windows.Visibility.Collapsed;
        }

        void PodcastListCategory_OnPodcastChannelAddStarted(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Visibility = System.Windows.Visibility.Visible;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            String categoryName = NavigationContext.QueryString["categoryName"] as String;
            m_groupModel = AddSubscription.PodcastGroups.FirstOrDefault(category => category.Name == categoryName);

            this.DataContext = m_groupModel;

            if (String.IsNullOrEmpty(m_groupModel.CachedJSON)) {
                WebClient wc = new WebClient();
                wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadCategoryJSONCompleted);
                wc.DownloadStringAsync(m_groupModel.RestAPIUrl);
            } else {
                populateCategoryList(m_groupModel.CachedJSON);
            }
        }

        void wc_DownloadCategoryJSONCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LoadingText.Text = "Error: Could not fetch category information from server. Please try again a bit later...";
                return;
            }

            String json = e.Result;
            if (String.IsNullOrEmpty(json) == false)
            {
                AddSubscription.PodcastGroups.Remove(m_groupModel);
                m_groupModel.CachedJSON = json;
                AddSubscription.PodcastGroups.Add(m_groupModel);
            }

            populateCategoryList(json);
        }

        private void populateCategoryList(String json)
        {
            List<BrowsePodcastItemModel> categoryItems = JsonConvert.DeserializeObject<List<BrowsePodcastItemModel>>(m_groupModel.CachedJSON);
            PodcastCategoryList.ItemsSource = categoryItems;
            LoadingText.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}