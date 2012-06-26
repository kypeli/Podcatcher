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
using System.Linq;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Podcatcher.ViewModels
{
    public class PodcastSqlModel : DataContext, INotifyPropertyChanged
    {
        private const string m_connectionString = "Data Source=isostore:/Podcatcher.sdf";

        private static PodcastSqlModel m_instance = null;
        public static PodcastSqlModel getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastSqlModel();
            }

            return m_instance;
        }

        private PodcastSqlModel()
            : base(m_connectionString)
        {
            if (DatabaseExists() == false)
            {
                CreateDatabase();
            }
        }

        public Table<PodcastSubscriptionModel> m_podcastSubscriptionsSql;
        private ObservableCollection<PodcastSubscriptionModel> m_podcastSubscriptions;
        public ObservableCollection<PodcastSubscriptionModel> PodcastSubscriptions
        {
         
            get 
            {
                var query = from PodcastSubscriptionModel podcastSubscription in m_podcastSubscriptionsSql
                            select podcastSubscription;

                m_podcastSubscriptions = new ObservableCollection<PodcastSubscriptionModel>(query);
                return m_podcastSubscriptions;
            }
        }

        internal void addSubscription(PodcastSubscriptionModel podcastModel)
        {
            m_podcastSubscriptionsSql.InsertOnSubmit(podcastModel);
            SubmitChanges();
            NotifyPropertyChanged("PodcastSubscriptions");
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
