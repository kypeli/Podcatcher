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
        public static PodcastSubscriptionModel podcastModelFromRSS(string podcastRss)
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
            var query = (from channel in rssXmlDoc.Descendants("channel")
                         select new
                         {
                             Title          = (string)channel.Element("title"),
                             Description    = (string)channel.Element("description"),
                             ImageUrl       = (channel.Element("image") != null ? channel.Element("image").Element("url").Value : @""),
                             PubDate        = (string)channel.Element("lastBuildDate"),
                             Link           = (string)channel.Element("link")
                         }).FirstOrDefault();

            if (query == null)
            {
                validFeed = false;
            }

            if (String.IsNullOrEmpty(query.PubDate))
            {
                validFeed = false;
            }

            if (validFeed == false)
            {
                Debug.WriteLine("ERROR: Cannot get all necessary fields from the podcast RSS.");
                return null;
            }

            PodcastSubscriptionModel podcastModel = new PodcastSubscriptionModel();
            podcastModel.PodcastName            = query.Title;
            podcastModel.PodcastDescription     = query.Description;
            podcastModel.PodcastLogoUrl         = new Uri(query.ImageUrl, UriKind.RelativeOrAbsolute);
            podcastModel.LastUpdateTimestamp    = parsePubDate(query.PubDate);
            podcastModel.PodcastShowLink        = query.Link;

            Debug.WriteLine("Got podcast subscription:"
                            + "\n\t* Name:\t\t\t\t\t"       + podcastModel.PodcastName
                            + "\n\t* Description:\t\t\t"    + podcastModel.PodcastDescription
                            + "\n\t* LogoUrl:\t\t\t\t"      + podcastModel.PodcastLogoUrl
                            + "\n\t* Updated timestamp:\t"  + podcastModel.LastUpdateTimestamp
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
                    episodeModel.EpisodeName = episode.Element("title").Value;
                    episodeModel.EpisodeDescription = episode.Element("description").Value;
                    episodeModel.EpisodePublished = pubDate;
                    episodeModel.EpisodeDownloadUri = episode.Element("enclosure").Attribute("url").Value;
                    episodeModel.EpisodeDownloadSize = Int64.Parse(episode.Element("enclosure").Attribute("length").Value);

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
                Debug.WriteLine("Warning: Could not parse pub date. Trying with next format...");
                resultDateTime = getDateTimeWithFormat("d MMM yyyy HH:mm:ss", pubDateString);   // Parse as 2 Jun 2012 11:53:25
            }

            if (resultDateTime.Equals(DateTime.MinValue))   
            {
                // Empty DateTime returned again. This is for you, Hacker Public Radio.
                Debug.WriteLine("Warning: Could not parse pub date. Trying with next format...");
                resultDateTime = getDateTimeWithFormat("yyyy-MM-dd", pubDateString);            // Parse as 2012-06-25
            }

            if (resultDateTime.Equals(DateTime.MinValue))
            {
                Debug.WriteLine("Warning: Could not parse pub date!");
            }
            return resultDateTime;
        }

        private static DateTime getDateTimeWithFormat(string dateFormat, string pubDateString)
        {
            pubDateString = pubDateString.Substring(0, dateFormat.Length);     // Retrieve only the first part of the pubdate, 
                                                                               // and ignore timezone.
            Debug.WriteLine("Trying to parse pub date: '" + pubDateString + "', format: " + dateFormat);

            DateTime result = new DateTime();
            if (DateTime.TryParseExact(pubDateString,
                                       dateFormat,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.None,
                                       out result) == false)
            {
                Debug.WriteLine("ERROR: Cannot parse feed's pubDate: '" + pubDateString + "', format: " + dateFormat);
            }

            return result;
        }
    }
}
