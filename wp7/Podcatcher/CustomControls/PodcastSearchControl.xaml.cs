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
using System.Xml.Linq;

namespace Podcatcher.CustomControls
{
    public partial class PodcastSearchControl : UserControl
    {
        public PodcastSearchControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(this.searchTerm.Text))
            {
                Debug.WriteLine("Empty search word. Do nothing.");
                return;
            }

            progressOverlay.Show();

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
            wc.DownloadStringAsync(new Uri(String.Format("https://gpodder.net/search.xml?q=%22{0}%22", this.searchTerm.Text)));
        }

        void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            XDocument searchXml;
            try
            {
                searchXml = XDocument.Parse(e.Result);
            }
            catch (System.Xml.XmlException ex)
            {
                Debug.WriteLine("ERROR: Cannot parse gPodder.net search result XML. Error: " + ex.Message);
                return;
            }

            var query = from podcast in searchXml.Descendants("podcast")
                        select podcast;

            List<PodcastSearchResultModel> results = new List<PodcastSearchResultModel>();
            foreach (var result in query)
            {
                PodcastSearchResultModel resultModel = new PodcastSearchResultModel();
                
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

            Debug.WriteLine("Found {0} results.", results.Count);
            this.SearchResultList.ItemsSource = results;
            this.progressOverlay.Hide();
        }
    }

    public class PodcastSearchResultModel
    {
        public Uri PodcastLogoUrl
        {
            get;
            set;
        }

        public String PodcastName
        {
            get;
            set;
        }

        public String PodcastUrl
        {
            get;
            set;
        }
    }
}
