/**
 * Copyright (c) 2012, 2013, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */



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
using Podcatcher.ViewModels;
using Coding4Fun.Phone.Controls;

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
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadSearchResultsCompleted);
            string searchQueryString = String.Format("https://gpodder.net/search.xml?q=\"{0}\"", this.searchTerm.Text);
            Debug.WriteLine("Search string: " + searchQueryString);
            wc.DownloadStringAsync(new Uri(searchQueryString));
        }

        void wc_DownloadSearchResultsCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine("ERROR: Web error happened. Error: " + e.Error.ToString());
                ToastPrompt toast = new ToastPrompt();
                toast.Title = "Error";
                toast.Message = "Could not get search results.";

                toast.Show();
                return;
            }

            XDocument searchXml;
            try
            {
                searchXml = XDocument.Parse(e.Result);
            }
            catch (System.Xml.XmlException ex)
            {
                Debug.WriteLine("ERROR: Cannot parse gPodder.net search result XML. Error: " + ex.Message);
                ToastPrompt toast = new ToastPrompt();
                toast.Title = "Error";
                toast.Message = "Could not get search results.";

                toast.Show();
                return;
            }

            var query = from podcast in searchXml.Descendants("podcast")
                        select podcast;

            List<GPodderResultModel> results = new List<GPodderResultModel>();
            foreach (var result in query)
            {
                GPodderResultModel resultModel = new GPodderResultModel();
                try
                {
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
                }
                catch (UriFormatException)
                {
                    Debug.WriteLine("Could not parse logo from the feed.");
                }

                results.Add(resultModel);
            }

            Debug.WriteLine("Found {0} results.", results.Count);
            this.SearchResultList.ItemsSource = results;
            this.progressOverlay.Hide();
        }
    }
}
