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
using Microsoft.Phone.Tasks;

namespace Podcatcher.Views
{
    public partial class AboutView : PhoneApplicationPage
    {
        public AboutView()
        {
            InitializeComponent();
            setUIIfPurchased();
        }

        private void setUIIfPurchased()
        {
            if ((Application.Current as App).IsTrial)
            {
                this.PurchaseButton.Visibility = Visibility.Visible;
                this.PurchasedText.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.PurchaseButton.Visibility = Visibility.Collapsed;
                this.PurchasedText.Visibility = Visibility.Visible;
            }
        }

        private void PurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            // pop up the link to rate and review the app
            MarketplaceReviewTask purchase = new MarketplaceReviewTask();
            purchase.Show();
        }
    }
}