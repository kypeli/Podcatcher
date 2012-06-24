using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Podcatcher
{
    public class PodcastModel : INotifyPropertyChanged
    {
        private string m_PodcastName;
        public string PodcastName
        {
            get
            {
                return m_PodcastName;
            }
            set
            {
                if (value != m_PodcastName)
                {
                    m_PodcastName = value;
                    NotifyPropertyChanged("PodcastName");
                }
            }
        }

        private Image m_PodcastLogoImage;
        public Image PodcastLogo
        {
            get
            {
                return m_PodcastLogoImage;
            }
            set
            {
                if (value != m_PodcastLogoImage)
                {
                    m_PodcastLogoImage = value;
                    NotifyPropertyChanged("PodcastLogo");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}