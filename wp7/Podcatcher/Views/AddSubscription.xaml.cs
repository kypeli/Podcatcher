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
using Coding4Fun.Phone.Controls;
using System.Windows.Media.Imaging;

namespace Podcatcher.Views
{
    public partial class AddSubscription : PhoneApplicationPage
    {
        /************************************* Public implementations *******************************/
        public AddSubscription()
        {
            InitializeComponent();

            m_subscriptionManager = PodcastSubscriptionsManager.getInstance();

            m_subscriptionManager.OnPodcastChannelFinished
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelFinished);
            m_subscriptionManager.OnPodcastChannelFinishedWithError
                += new SubscriptionManagerHandler(subscriptionManager_OnPodcastChannelFinishedWithError);

        }

        /************************************* Private implementations *******************************/
        #region private
        private PodcastSubscriptionsManager m_subscriptionManager;

        private void addFromUrlButton_Click(object sender, RoutedEventArgs e)
        {
            progressOverlay.Show();

            m_subscriptionManager.addSubscriptionFromURL(addFromUrlInput.Text);
        }

        private void subscriptionManager_OnPodcastChannelFinishedWithError(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();

            ToastPrompt toast = new ToastPrompt();
            toast.Title = "Error";
            toast.Message = e.message;

            toast.Show();
        }

        private void subscriptionManager_OnPodcastChannelFinished(object source, SubscriptionManagerArgs e)
        {
            progressOverlay.Hide();
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
        #endregion
    }
}