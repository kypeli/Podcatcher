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

namespace Podcatcher
{
    public partial class MainView : PhoneApplicationPage
    {
        // Constructor
        public MainView()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = PodcastSubscriptionsModel.Instance;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AddSubscriptionIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AddSubscription.xaml", UriKind.Relative));
        }
    }
}