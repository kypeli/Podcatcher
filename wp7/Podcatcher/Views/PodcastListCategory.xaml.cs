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