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
 using Newtonsoft.Json;

namespace Podcatcher.ViewModels
{
    public class BrowsePodcastItemModel
    {
        [JsonProperty("title")]
        public string PodcastName { get; set; }

        public string description { get; set; }

        public string url { get; set; }

        [JsonProperty("scaled_logo_url")]
        public string PodcastLogoUrl { get; set; }
    }
}
