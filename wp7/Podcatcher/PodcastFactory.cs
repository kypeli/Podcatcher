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

            var query = (from channel in rssXmlDoc.Descendants("channel")
                         select new
                         {
                             Title          = channel.Element("title").Value,
                             Description    = channel.Element("description").Value,
                             ImageUrl       = channel.Element("image").Element("url").Value,
                             PubDate        = channel.Element("lastBuildDate").Value
                         }).FirstOrDefault();

            if (query == null)
            {
                Debug.WriteLine("ERROR: Cannot get all necessary fields from the podcast RSS.");
                return null;
            }

            PodcastSubscriptionModel podcastModel = new PodcastSubscriptionModel();
            podcastModel.PodcastName            = query.Title;
            podcastModel.PodcastDescription     = query.Description;
            podcastModel.PodcastLogoUrl         = new Uri(query.ImageUrl, UriKind.Absolute);
            podcastModel.LastUpdateTimestamp    = parsePubDate(query.PubDate);

            Debug.WriteLine("Got podcast subscription:"
                            + "\n\t* Name:\t\t\t\t\t"       + podcastModel.PodcastName
                            + "\n\t* Description:\t\t\t"    + podcastModel.PodcastDescription
                            + "\n\t* LogoUrl:\t\t\t\t"      + podcastModel.PodcastLogoUrl
                            + "\n\t* Updated timestamp:\t"  + podcastModel.LastUpdateTimestamp
                            );


            return podcastModel;                
        }

        private static DateTime parsePubDate(string pubDateString)
        {
            DateTime resultDateTime = new DateTime();
            
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
