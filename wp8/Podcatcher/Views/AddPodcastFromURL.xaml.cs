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
using Coding4Fun.Toolkit.Controls;

namespace Podcatcher.Views
{
    public partial class AddPodcastFromURL : PhoneApplicationPage
    {
        public AddPodcastFromURL()
        {
            InitializeComponent();

            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAddStarted += new SubscriptionManagerHandler(AddPodcastFromURL_OnPodcastChannelAddStarted);
            PodcastSubscriptionsManager.getInstance().OnPodcastChannelAddFinishedWithError += new SubscriptionManagerHandler(AddPodcastFromURL_OnPodcastChannelAddFinishedWithError);
        }

        void AddPodcastFromURL_OnPodcastChannelAddFinishedWithError(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Visibility = System.Windows.Visibility.Collapsed;
        }

        void AddPodcastFromURL_OnPodcastChannelAddStarted(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Visibility = System.Windows.Visibility.Visible;
        }

        private void addFromUrlButton_Click(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionsManager.getInstance().addSubscriptionFromURL(addFromUrlInput.Text, true);
        }
    }
}