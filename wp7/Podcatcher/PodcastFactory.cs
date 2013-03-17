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
        public static PodcastSubscriptionModel podcastModelFromRSS(string podcastRss, String itunesNamespace = "")
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
                             ImageUrl       = channel.Element(namespaceDef + "image"),
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
                Debug.WriteLine("Warning: Podcast URL is empty in RSS feed.");
            }

            string imageUrl = "";
            if (query.ImageUrl == null)
            {
                // We try with three different iTunes namespaces to get the logo image. When we get here, we have
                // tried once - with empty namespace.
                // Then we try with two more.
                // If we then haven't found a logo, we hit the default branch.
                switch (itunesNamespace)
                {
                    case "":
                        return podcastModelFromRSS(podcastRss, "http://www.itunes.com/dtds/podcast-1.0.dtd");
                    case "http://www.itunes.com/dtds/podcast-1.0.dtd":
                        return podcastModelFromRSS(podcastRss, "http://www.itunes.com/DTDs/Podcast-1.0.dtd");
                    default:
                        Debug.WriteLine("ERROR: Podcast logo URL in RSS is invalid.");
                        imageUrl = "";
                        break;
                        
                }
            }
            else
            {
                // Find the logo URL as the attribute of the 'image' element.
                XElement logoXmlElement = query.ImageUrl;
                if (logoXmlElement.Attribute("href") != null)
                {
                    imageUrl = logoXmlElement.Attribute("href").Value;
                }
                // Find the logo URL as the child element of the 'image' element.
                else if (logoXmlElement.Element("url") != null)
                {
                    imageUrl = logoXmlElement.Element("url").Value;
                }
                else
                {
                    imageUrl = "";
                }
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
            if (string.IsNullOrEmpty(query.Link))
            {
                podcastModel.PodcastShowLink = query.Title;
            }

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
                if (episode.Element("pubDate") == null)
                {
                    Debug.WriteLine("Episode has no pubDate. Ignoring.");
                    continue;
                }

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
                            episodeModel.EpisodeDownloadUri = "";
                            Debug.WriteLine("WARNING: Null element: enclosure - url. Defaulting to empty.");
                        }

                        XAttribute mimeTypeAttribute = currentElement.Attribute("type");
                        if (mimeTypeAttribute != null)
                        {
                            episodeModel.EpisodeFileMimeType = mimeTypeAttribute.Value;
                        }
                        else
                        {
                            episodeModel.EpisodeFileMimeType = "";
                            Debug.WriteLine("WARNING: Null element: enclosure - mime type. Defaulting to empty.");
                        }
                    }
                    else
                    {
                        episodeModel.EpisodeFileMimeType = "";
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

        public static List<Uri> podcastUrlFromOPMLImport(string importRss)
        {
            if (String.IsNullOrEmpty(importRss))
            {
                Debug.WriteLine("ERROR: Import OPML file is empty. Cannot continue.");
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

            var podcastsQuery = from podcast in podcastRssXmlDoc.Descendants("outline")
                                select podcast;

            List<Uri> podcastUrls = new List<Uri>();
            foreach (var podcast in podcastsQuery)
            {
                XAttribute type = podcast.Attribute("type");
                XAttribute url = podcast.Attribute("xmlUrl");
                if (type != null && type.Value == "rss")
                {
                    if (url != null
                        && url.Value != null)
                    {
                        podcastUrls.Add(new Uri(url.Value));
                        Debug.WriteLine("Got new URI from OPML: " + url.Value);
                    }
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
            resultDateTime = getDateTimeWithFormat("dd MMM yyyy HH:mm:ss", pubDateString, "dd MMM yyyy HH:mm:ss".Length);      // Parse as 25 Jun 2012 11:53:25
            if (resultDateTime.Equals(DateTime.MinValue))   
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMM yyyy HH:mm:ss", pubDateString, "d MMM yyyy HH:mm:ss".Length);   // Parse as 2 Jun 2012 11:53:25
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("dd MMMM yyyy HH:mm:ss GMT", pubDateString, pubDateString.Length);   // Parse as 2 December 2012 11:53:23 GMT
            }
            
            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Empty DateTime returned.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("dd MMM yyyy HH:mm", pubDateString, "dd MMM yyyy HH:mm".Length);   // Parse as 2 Jun 2012 11:53
            } 

            if (resultDateTime.Equals(DateTime.MinValue))   
            {
                // Empty DateTime returned again. This is for you, Hacker Public Radio and the Economist!.
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("yyyy-MM-dd", pubDateString, "yyyy-MM-dd".Length);            // Parse as 2012-06-25
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // Talk Radio 702 - The Week That Wasn't
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("yyyy/MM/dd HH:mm:ss", pubDateString, "yyyy/MM/dd HH:mm:ss".Length);  // Parse as 2012/12/17 03:18:16 PM
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // The Dan Patrick Show: Podcast
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMMM yyyy HH:mm:ss EST", pubDateString, pubDateString.Length);  // Parse as 11 January 2013 10:10:10 EST
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // gPodder test feed
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("ddd MMM dd HH:mm:ss yyyy", pubDateString, pubDateString.Length);  // Parse as Fri Jan 11 14:40:38 2013
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                // gPodder test feed
                Debug.WriteLine("Warning: Could not parse pub date! Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMM yyyy HH:mm", pubDateString, "d MMM yyyy HH:mm".Length);  // Parse as Mon, 4 Feb 2013 11:00 GMT
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                Debug.WriteLine("ERROR: Could not parse pub date!");
            }

            return resultDateTime;
        }

        private static DateTime getDateTimeWithFormat(string dateFormat, string pubDateString, int parseLength)
        {
            DateTime result = new DateTime();
            if (parseLength > pubDateString.Length)
            {
                Debug.WriteLine("Cannot parse pub date as its length doesn't match the format length we are looking for. Returning.");
                return result;
            }

            pubDateString = pubDateString.Substring(0, parseLength);
            if (DateTime.TryParseExact(pubDateString,
                                       dateFormat,
                                       new CultureInfo("en-US"),
                                       // CultureInfo.InvariantCulture,
                                       DateTimeStyles.None,
                                       out result) == false)
            {
               //  Debug.WriteLine("Warning: Cannot parse feed's pubDate: '" + pubDateString + "', format: " + dateFormat);
            }

            return result;
        }
    }
}
