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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using Podcatcher.ViewModels;

namespace Podcatcher
{
    public class PodcastFactory
    {
        public static PodcastSubscriptionModel podcastModelFromRSS(string podcastRss, String itunesNamespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")
        {
            XDocument rssXmlDoc;
            try
            {
                rssXmlDoc = XDocument.Parse(podcastRss);
            }
            catch (System.Exception e) 
            {
                Debug.WriteLine("ERROR: Cannot parse podcast RSS feed. Status: " + e.ToString());
                return null;
            }

            bool validFeed = true;
            XNamespace namespaceDef = itunesNamespace;
            var query = (from channel in rssXmlDoc.Descendants("channel")
                         select new
                         {
                             Title          = (string)channel.Element("title"),
                             Description    = (string)channel.Element("description"),
                             ImageUrl = ((channel.Element(namespaceDef + "image") != null && channel.Element(namespaceDef + "image").Attribute("href") != null) ? 
                                        channel.Element(namespaceDef + "image").Attribute("href").Value : 
                                        @""),
                             Link           = (string)channel.Element("link")
                         }).FirstOrDefault();

            if (query == null) 
            {
                return podcastModelFromRSS(podcastRss, "http://www.itunes.com/DTDs/Podcast-1.0.dtd");
            }

            if (query == null)
            {
                validFeed = false;
            }

            if (String.IsNullOrEmpty(query.Link))
            {
                Debug.WriteLine("ERROR: Podcast URL is empty in RSS feed.");
                validFeed = false;
            }

            string imageUrl = "";
            if (String.IsNullOrEmpty(query.ImageUrl))
            {
                if (itunesNamespace != "http://www.itunes.com/DTDs/Podcast-1.0.dtd")
                {
                    return podcastModelFromRSS(podcastRss, "http://www.itunes.com/DTDs/Podcast-1.0.dtd");
                }
                else
                {
                    Debug.WriteLine("ERROR: Podcast logo URL in RSS is invalid.");
                    imageUrl = "";
                }

            }
            else
            {
                imageUrl = query.ImageUrl;
            }

            if (validFeed == false)
            {
                Debug.WriteLine("ERROR: Cannot get all necessary fields from the podcast RSS.");
                return null;
            }

            PodcastSubscriptionModel podcastModel = new PodcastSubscriptionModel();
            podcastModel.PodcastName            = query.Title;
            podcastModel.PodcastDescription     = query.Description;

            if (string.IsNullOrEmpty(imageUrl) == false)
            {
                podcastModel.PodcastLogoUrl = new Uri(imageUrl, UriKind.Absolute);
            }
            podcastModel.PodcastShowLink        = query.Link;

            Debug.WriteLine("Got podcast subscription:"
                            + "\n\t* Name:\t\t\t\t\t"       + podcastModel.PodcastName
                            + "\n\t* Description:\t\t\t"    + podcastModel.PodcastDescription
                            + "\n\t* LogoUrl:\t\t\t\t"      + podcastModel.PodcastLogoUrl
                            );


            return podcastModel;                
        }

        public static List<PodcastEpisodeModel> newPodcastEpisodes(string podcastRss, DateTime latestLocalEpisodeTimestamp)
        {
            if (String.IsNullOrEmpty(podcastRss))
            {
                Debug.WriteLine("ERROR: Given RSS to parse episodes from is empty or null. Cannot continue...");
                return null;
            }

            XDocument podcastRssXmlDoc;
            try
            {
                podcastRssXmlDoc = XDocument.Parse(podcastRss);
            } catch(System.Xml.XmlException e) {
                Debug.WriteLine("ERROR: Parse error when parsing podcast episodes. Message: " + e.Message);
                return null;
            }

            var episodesQuery = from episode in podcastRssXmlDoc.Descendants("item")
                                select episode;

            List<PodcastEpisodeModel> episodes = new List<PodcastEpisodeModel>();
            XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            foreach (var episode in episodesQuery)
            {
                // Only return newer than given timestamp episodes.
                DateTime pubDate = parsePubDate(episode.Element("pubDate").Value);
                if (pubDate > latestLocalEpisodeTimestamp)
                {
                    PodcastEpisodeModel episodeModel = new PodcastEpisodeModel();

                    // Get the RSS mandatory fields. 
                    XElement currentElement;

                    currentElement = getChildElementByName(episode, "title");
                    if (currentElement != null)
                    {
                        episodeModel.EpisodeName = currentElement.Value;
                    }
                    else
                    {
                        Debug.WriteLine("WARNING: Null element: title");
                    }

                    XElement summaryElement = episode.Element(itunes + "summary");
                    if (summaryElement != null)
                    {
                        episodeModel.EpisodeDescription = summaryElement.Value;
                    }
                    else
                    {
                        Debug.WriteLine("WARNING: Null element: description");
                    }

                    currentElement = getChildElementByName(episode, "enclosure");
                    if (currentElement != null)
                    {
                        XAttribute urlAttribute = currentElement.Attribute("url");
                        if (urlAttribute != null)
                        {
                            String url = urlAttribute.Value;
                            episodeModel.EpisodeDownloadUri = url.Trim();
                        }
                        else
                        {
                            episodeModel.EpisodeFileMimeType = "-ERROR-";
                            Debug.WriteLine("WARNING: Null element: enclosure - url");
                        }

                        XAttribute mimeTypeAttribute = currentElement.Attribute("type");
                        if (mimeTypeAttribute != null)
                        {
                            episodeModel.EpisodeFileMimeType = mimeTypeAttribute.Value;
                        }
                        else
                        {
                            episodeModel.EpisodeFileMimeType = "-ERROR-";
                            Debug.WriteLine("WARNING: Null element: enclosure - mime type");
                        }
                    }
                    else
                    {
                        episodeModel.EpisodeFileMimeType = "-ERROR-";
                        Debug.WriteLine("WARNING: Null element: enclosure");
                    }

                    currentElement = getChildElementByName(episode, "enclosure");
                    if (currentElement != null)
                    {
                        XAttribute downloadSizeAttribute = currentElement.Attribute("length");
                        if (downloadSizeAttribute != null)
                        {
                            try
                            {
                                episodeModel.EpisodeDownloadSize = Int64.Parse(downloadSizeAttribute.Value);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine("ERROR: Parse error. Message: " + e.Message);
                            }
 
                        }
                        else
                        {
                            Debug.WriteLine("WARNING: Null element: EpisodeDownloadSize - length");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("WARNING: Null element: EpisodeDownloadSize");
                    }

                    episodeModel.EpisodePublished = pubDate; 


                    XElement runningTimeElement = episode.Element(itunes + "duration");
                    if (runningTimeElement != null)
                    {
                        episodeModel.EpisodeRunningTime = runningTimeElement.Value;
                    }

                    episodes.Add(episodeModel);
                }
            }

            return episodes;
        }

        public static List<Uri> podcastUrlFromGpodderImport(string importRss) 
        {
            if (String.IsNullOrEmpty(importRss))
            {
                Debug.WriteLine("ERROR: Import XML file is empty. Cannot continue.");
                return null;
            }

            XDocument podcastRssXmlDoc;
            try
            {
                podcastRssXmlDoc = XDocument.Parse(importRss);
            }
            catch (System.Xml.XmlException e)
            {
                Debug.WriteLine("ERROR: Parse error when parsing imported subscriptions. Message: " + e.Message);
                return null;
            }

            var podcastsQuery = from podcast in podcastRssXmlDoc.Descendants("podcast")
                                select podcast;

            List<Uri> podcastUrls = new List<Uri>();
            foreach (var podcast in podcastsQuery)
            {
                XElement urlElement = podcast.Element("url");
                if (urlElement != null)
                {
                    podcastUrls.Add(new Uri(urlElement.Value));
                    Debug.WriteLine("Got new URI from gPodder: " + urlElement.Value);
                }
            }

            return podcastUrls;
        }

        private static XElement getChildElementByName(XElement episode, string name)
        {
            XElement element = episode.Element(name);
            return element;
        }

        private static DateTime parsePubDate(string pubDateString)
        {
            DateTime resultDateTime = new DateTime();
            
            if (String.IsNullOrEmpty(pubDateString))
            {
                Debug.WriteLine("WARNING: Empty pubDate string given. Cannot parse it...");
                return resultDateTime;
            }
            
            // pubDateString is e.g. 'Mon, 25 Jun 2012 11:53:25 -0700'
            int indexOfComma = pubDateString.IndexOf(',');
            if (indexOfComma >= 0) {
                pubDateString = pubDateString.Substring(indexOfComma + 2);                      // Find the ',' and remove 'Mon, '
            }

            // Next we try to parse the date string field in various formats that 
            // can be found in podcast RSS feeds.
            resultDateTime = getDateTimeWithFormat("dd MMM yyyy HH:mm:ss", pubDateString);      // Parse as 25 Jun 2012 11:53:25
            if (resultDateTime.Equals(DateTime.MinValue))   
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMM yyyy HH:mm:ss", pubDateString);   // Parse as 2 Jun 2012 11:53:25
            } 

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("dd MMM yyyy HH:mm", pubDateString);   // Parse as 2 Jun 2012 11:53
            } 

            if (resultDateTime.Equals(DateTime.MinValue))   
            {
                // Empty DateTime returned again. This is for you, Hacker Public Radio and the Economist!.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("yyyy-MM-dd", pubDateString);            // Parse as 2012-06-25
            } 

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                Debug.WriteLine("ERROR: Could not parse pub date!");
            }

            return resultDateTime;
        }

        private static DateTime getDateTimeWithFormat(string dateFormat, string pubDateString)
        {
            DateTime result = new DateTime();
            if (dateFormat.Length > pubDateString.Length)
            {
                Debug.WriteLine("Cannot parse pub date as its length doesn't match the format length we are looking for. Returning.");
                return result;
            }

            pubDateString = pubDateString.Substring(0, dateFormat.Length);     // Retrieve only the first part of the pubdate, 
                                                                               // and ignore timezone.
            Debug.WriteLine("Trying to parse pub date: '" + pubDateString + "', format: " + dateFormat);

            if (DateTime.TryParseExact(pubDateString,
                                       dateFormat,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.None,
                                       out result) == false)
            {
               //  Debug.WriteLine("Warning: Cannot parse feed's pubDate: '" + pubDateString + "', format: " + dateFormat);
            }

            return result;
        }
    }
}
