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

namespace Podcatcher.Views
{
    public partial class PodcastSubscriptionIntroduction : PhoneApplicationPage
    {
        public PodcastSubscriptionIntroduction()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            string podcastUrl = (string)NavigationContext.QueryString["podcastUrl"];
            
            Uri podcastUri;
            try {
                podcastUri = new Uri(podcastUrl);
            } catch(Exception) {
               Console.WriteLine("Malformed podcast address.");
                // TODO: Show toast 
               return;
            }

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
            wc.DownloadStringAsync(podcastUri);
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine("Malformed podcast address.");

            }

            PodcastSubscriptionModel subscription = PodcastFactory.podcastModelFromRSS((string)e.Result);
            
            PodcastName.Text = subscription.PodcastName;
            PodcastIcon.Source = new BitmapImage(subscription.PodcastLogoUrl);
            PodcastDescription.Text = subscription.PodcastDescription;
        }
    }
}