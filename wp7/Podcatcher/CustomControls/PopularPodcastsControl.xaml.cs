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
using System.Xml.Linq;

namespace Podcatcher.CustomControls
{
    public partial class PopularPodcastsControl : UserControl
    {
        public PopularPodcastsControl()
        {
            InitializeComponent();
            
            Loaded += PageLoaded;
        }

        void PageLoaded(object sender, RoutedEventArgs e)
        {
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
            wc.DownloadStringAsync(new Uri("http://gpodder.net/toplist/15.xml"));
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            XDocument xmlResult = XDocument.Parse(e.Result);
        }
    }
}
