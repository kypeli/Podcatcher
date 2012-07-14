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
using System.Diagnostics;

namespace Podcatcher.CustomControls
{
    public partial class PodcastSearchResultControl : UserControl
    {
        PodcastSearchResultModel m_searchResultModel;
        PodcastSubscriptionsManager m_subscriptionManager;

        public PodcastSearchResultControl()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Page_Loaded);

            m_subscriptionManager = PodcastSubscriptionsManager.getInstance();
            m_subscriptionManager.OnPodcastChannelFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelFinished);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_searchResultModel = this.DataContext as PodcastSearchResultModel;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            m_subscriptionManager.addSubscriptionFromURL(m_searchResultModel.PodcastUrl);
        }


        private void subscriptionManager_OnPodcastChannelFinished(object source, SubscriptionManagerArgs e)
        {
            Debug.WriteLine("Added.");
        }

    }
}
