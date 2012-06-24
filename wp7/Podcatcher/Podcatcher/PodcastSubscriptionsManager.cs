using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Podcatcher
{
    public class PodcastSubscriptionsManager
    {
        private static PodcastSubscriptionsManager m_instance = null;
        private PodcastSubscriptionsModel m_subscriptions = new PodcastSubscriptionsModel();

        private PodcastSubscriptionsManager()
        {
        }

        public static PodcastSubscriptionsManager getInstance()
        {
            if (m_instance == null) {
                m_instance = new PodcastSubscriptionsManager();
            }

            return m_instance;
        }

        public PodcastSubscriptionsModel PodcastSubscriptions {
            get { return m_subscriptions; }
            private set; 
        }
    }
}
