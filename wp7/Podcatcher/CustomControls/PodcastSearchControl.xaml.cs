/**
 * Copyright (c) 2012, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the <organization> nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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

            Debug.WriteLine("Found {0} results.", results.Count);
            this.SearchResultList.ItemsSource = results;
            this.progressOverlay.Hide();
        }
    }
}
