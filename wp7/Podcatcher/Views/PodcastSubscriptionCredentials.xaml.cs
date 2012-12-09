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