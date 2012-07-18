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
using System.Diagnostics;
using Podcatcher.ViewModels;

namespace Podcatcher.CustomControls
{
    public partial class PopularPodcastsControl : UserControl
    {
        public PopularPodcastsControl()
        {
            InitializeComponent();
            
            Loaded += PageLoaded;
            this.LoadingText.Visibility = Visibility.Visible;
        }

        void PageLoaded(object sender, RoutedEventArgs e)
        {
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadTopPodcastsXMLCompleted);
            wc.DownloadStringAsync(new Uri("http://gpodder.net/toplist/15.xml"));
        }

        void wc_DownloadTopPodcastsXMLCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            XDocument xmlResult = XDocument.Parse(e.Result);

            try
            {
                xmlResult = XDocument.Parse(e.Result);
            }
            catch (System.Xml.XmlException ex)
            {
                Debug.WriteLine("ERROR: Cannot parse gPodder.net popular result XML. Error: " + ex.Message);
                return;
            }

            var query = from podcast in xmlResult.Descendants("podcast")
                        select podcast;

            List<GPodderResultModel> results = new List<GPodderResultModel>();
            foreach (var result in query)
            {
                GPodderResultModel resultModel = new GPodderResultModel();
                
                XElement logoElement = result.Element("logo_url");

                if (logoElement == null 
                    || String.IsNullOrEmpty(logoElement.Value))
                {
                    logoElement = result.Element("scaled_logo_url");
                }

                if (logoElement != null
                    && String.IsNullOrEmpty(logoElement.Value) == false)
                {
                    resultModel.PodcastLogoUrl = new Uri(logoElement.Value);
                }
                
                resultModel.PodcastName = result.Element("title").Value;
                resultModel.PodcastUrl = result.Element("url").Value;

                results.Add(resultModel);
            }

            Debug.WriteLine("Found {0} popular results.", results.Count);
            this.PopularResultList.ItemsSource = results;

            this.LoadingText.Visibility = Visibility.Collapsed;
        }
    }
}
