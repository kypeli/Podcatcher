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
    public partial class AddSubscription : PhoneApplicationPage
    {
        public AddSubscription()
        {
            InitializeComponent();
        }

        private void addFromUrlButton_Click(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionsManager subscriptionManager = PodcastSubscriptionsManager.getInstance();
            subscriptionManager.addSubscriptionFromURL(addFromUrlInput.Text);
        }

    }
}