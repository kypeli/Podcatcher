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
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;

namespace Podcatcher.CustomControls
{
    public partial class PodcastSearchResultControl : UserControl
    {
        GPodderResultModel m_searchResultModel;
        PodcastSubscriptionsManager m_subscriptionManager;

        public PodcastSearchResultControl()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Page_Loaded);

            m_subscriptionManager = PodcastSubscriptionsManager.getInstance();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            m_searchResultModel = this.DataContext as GPodderResultModel;
            m_subscriptionManager.addSubscriptionFromURL(m_searchResultModel.PodcastUrl);
        }
    }
}
