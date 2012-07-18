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

namespace Podcatcher.ViewModels
{
    public class GPodderResultModel
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
