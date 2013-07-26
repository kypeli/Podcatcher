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

namespace Podcatcher.CustomControls
{
    public partial class BrowsePodcastsControl : UserControl
    {
        public BrowsePodcastsControl()
        {
            InitializeComponent();
            Loaded += PageLoaded;
//            this.LoadingText.Visibility = Visibility.Visible;
        }

        void PageLoaded(object sender, RoutedEventArgs e)
        {
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Arts" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "CBS" });                 // https://gpodder.net/directory/CBC
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Comedy" });              // https://gpodder.net/directory/comedy
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Computer Science" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Drama" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Gadgets" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "News" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Religion" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Politics" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Technology" });          // https://gpodder.net/directory/technology
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "TV" });
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Science" });             // https://gpodder.net/directory/Science
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Sports" });              // https://gpodder.net/api/2/tag/Sports%20&%20Recreation/25.json
            BrowsePodcastsList.Items.Add(new BrowsePodcastsItemModel { Name = "Skepticism" });        
        }

        private void TextBlock_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {

        }
    }
}
