using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;


namespace Podcatcher
{
    public class PodcastSubscriptionsModel : INotifyPropertyChanged
    {
        public PodcastSubscriptionsModel()
        {
            m_podcastsModel = new ObservableCollection<PodcastModel>();
        }

        private ObservableCollection<PodcastModel> m_podcastsModel;
        public ObservableCollection<PodcastModel> PodcastSubscriptions {
            get
            {
                return m_podcastsModel;
            }

            private set
            {
                if (m_podcastsModel != value)
                {
                    m_podcastsModel = value;
                }
            }
        }

        private string _sampleProperty = "Sample Runtime Property Value";
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string SampleProperty
        {
            get
            {
                return _sampleProperty;
            }
            set
            {
                if (value != _sampleProperty)
                {
                    _sampleProperty = value;
                    NotifyPropertyChanged("SampleProperty");
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