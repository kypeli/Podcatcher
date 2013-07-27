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
using Microsoft.Phone.Controls;

namespace Podcatcher.CustomControls
{
    public partial class BrowsePodcastsControl : UserControl
    {
        private const String GPODDER_API_BASEURL = "https://gpodder.net/api/2/";
        private const int NUMBER_OF_PODCASTS_PER_GROUP = 40;

        public BrowsePodcastsControl()
        {
            if (AddSubscription.PodcastGroups.Count == 0)
            {
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Arts", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/arts/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "BBC", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/bbc/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Business", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Business News/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "CBS", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/cbs/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Comedy", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/comedy/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Computer Science", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Computer Science/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Drama", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/drama/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Gadgets", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/gadgets/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "News", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/News/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Religion", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/religion/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Politics", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Politics/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Talk Radio", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Talk Radio/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Technology", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/technology/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "TV", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/tv/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Science", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Science/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Sports", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Sports%20&%20Recreation/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Skepticism", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/skepticism/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Literature", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Literature/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "Video Games", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/Video Games/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
                AddSubscription.PodcastGroups.Add(new BrowsePodcastsGroupModel { Name = "NPR", RestAPIUrl = new Uri(GPODDER_API_BASEURL + "tag/NPR/" + NUMBER_OF_PODCASTS_PER_GROUP + ".json") });
            }

            InitializeComponent();
            Loaded += PageLoaded;
        }

        void PageLoaded(object sender, RoutedEventArgs e)
        {
            BrowsePodcastsList.ItemsSource = AddSubscription.PodcastGroups.OrderBy(group => group.Name).ToArray();
        }

        private void PodcastGroupTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            BrowsePodcastsGroupModel group = (sender as StackPanel).DataContext as BrowsePodcastsGroupModel;

            PhoneApplicationFrame applicationFrame = Application.Current.RootVisual as PhoneApplicationFrame;
            applicationFrame.Navigate(new Uri(string.Format("/Views/PodcastListCategory.xaml?categoryName={0}", group.Name), UriKind.Relative));
        }
    }
}
