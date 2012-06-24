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

        private static PodcastSubscriptionsModel podcastSubscriptionsModel = null;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static PodcastSubscriptionsModel Instance
        {
            get
            {
                // Delay creation of the view model until necessary
                if (podcastSubscriptionsModel == null)
                    podcastSubscriptionsModel = new PodcastSubscriptionsModel();

                return podcastSubscriptionsModel;
            }
        }

        private PodcastSubscriptionsModel()
        {
            this.PodcastSubscriptions = new ObservableCollection<PodcastModel>();
        }

        public ObservableCollection<PodcastModel> PodcastSubscriptions { get; private set; }

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

        public bool IsDataLoaded
        {
            get;
            private set;
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