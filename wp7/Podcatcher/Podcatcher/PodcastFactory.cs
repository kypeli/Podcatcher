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

namespace Podcatcher
{
    public class PodcastFactory
    {
        public static PodcastModel podcastModelFromRSS(string podcastRss)
        {
            XDocument rssXmlDoc;
            try
            {
                rssXmlDoc = XDocument.Parse(podcastRss);
            }
            catch (System.Xml.XmlException e)
            {
                Debug.WriteLine("ERROR: Cannot parse podcast RSS feed. Status: " + e.ToString());
                return null;
            }

            var query = (from channel in rssXmlDoc.Descendants("channel")
                         select new
                         {
                             Title          = channel.Element("title").Value,
                             Description    = channel.Element("description").Value,
                             ImageUrl       = channel.Element("image").Element("url").Value
                         }).FirstOrDefault();

            if (query == null)
            {
                Debug.WriteLine("ERROR: Cannot get all necessary fields from the podcast RSS.");
                return null;
            }

            PodcastModel podcastModel = new PodcastModel();
            podcastModel.PodcastName        = query.Title;
            podcastModel.PodcastDescription = query.Description;
            podcastModel.PodcastLogoUrl     = query.ImageUrl;

            return podcastModel;                
        }
    }
}
