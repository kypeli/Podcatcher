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
using Podcatcher.ViewModels;
using Podcatcher.Views;
using System.Diagnostics;

namespace Podcatcher.CustomControls
{
    public partial class BrowsePodcastsControl : UserControl
    {
        private const String GPODDER_API_BASEURL = "https://gpodder.net/api/2/";
        private const int NUMBER_OF_PODCASTS_PER_GROUP = 25;

        public BrowsePodcastsControl()
        {
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Arts", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/arts/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "CBS" });                 // https://gpodder.net/directory/CBC
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Comedy" });              // https://gpodder.net/directory/comedy
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Computer Science" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Drama" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Gadgets" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "News" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Religion" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Politics" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Technology" });          // https://gpodder.net/directory/technology
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "TV" });
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Science" });             // https://gpodder.net/directory/Science
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Sports" });              // https://gpodder.net/api/2/tag/Sports%20&%20Recreation/25.json
            AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Skepticism" });
            
            InitializeComponent();
            Loaded += PageLoaded;
        }

        void PageLoaded(object sender, RoutedEventArgs e)
        {
            BrowsePodcastsList.ItemsSource = AddSubscription.PodcastGroups.OrderBy(group => group.Name).ToArray();
        }

        private void PodcastGroupTapped(object sender, GestureEventArgs e)
        {
            BrowsePodcastsGroupModel group = (sender as StackPanel).DataContext as BrowsePodcastsGroupModel;
        }
    }
}
