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
    public partial class ImportPodcastsViaOPML : PhoneApplicationPage
    {
        private PodcastSubscriptionsManager m_subscriptionManager = PodcastSubscriptionsManager.getInstance();

        public ImportPodcastsViaOPML()
        {
            InitializeComponent();

            if (App.IsTrial)
            {
                this.opmlDisclaimer.Visibility = System.Windows.Visibility.Collapsed;
                this.opmlNotAvailable.Visibility = System.Windows.Visibility.Visible;
                this.opmlUrl.IsEnabled = false;
                this.importFromOpmlUrl.IsEnabled = false;
            }
            else
            {
                this.opmlDisclaimer.Visibility = System.Windows.Visibility.Visible;
                this.opmlNotAvailable.Visibility = System.Windows.Visibility.Collapsed;
                this.opmlUrl.IsEnabled = true;
                this.importFromOpmlUrl.IsEnabled = true;
            }            

        }

        private void importFromOpmlUrl_Click(object sender, RoutedEventArgs e)
        {
            PodcastSubscriptionsManager.getInstance().addSubscriptionFromOPMLFile(opmlUrl.Text);
        }

    }
}