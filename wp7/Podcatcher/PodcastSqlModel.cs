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
using System.Diagnostics;
using Podcatcher;
using System.Collections.Generic;

namespace Podcatcher.ViewModels
{
    public class PodcastSqlModel : DataContext, INotifyPropertyChanged
    {

        /************************************* Public properties *******************************/

        private List<PodcastSubscriptionModel> m_podcastSubscriptions;
        public List<PodcastSubscriptionModel> PodcastSubscriptions
        {         
            get 
            {
                var query = from PodcastSubscriptionModel podcastSubscription in m_podcastSubscriptionsSql
                            orderby podcastSubscription.PodcastName 
                            select podcastSubscription;

                m_podcastSubscriptions = new List<PodcastSubscriptionModel>(query);
                return m_podcastSubscriptions;
            }
        }

        /************************************* Public implementations *******************************/

        public static PodcastSqlModel getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastSqlModel();
            }

            return m_instance;
        }

        public PodcastSubscriptionModel subscriptionModelForIndex(int index)
        {
            if (isValidSubscriptionModelIndex(index) == false)
            {
                return null;
            }

            return m_podcastSubscriptions.ElementAt(index);
        }

        public void addSubscription(PodcastSubscriptionModel podcastModel)
        {
            m_podcastSubscriptionsSql.InsertOnSubmit(podcastModel);
            subscriptionModelChanged();
        }

        public void deleteSubscription(PodcastSubscriptionModel podcastModel)
        {

            m_podcastSubscriptionsSql.DeleteOnSubmit(podcastModel);
            subscriptionModelChanged();
        }

        public void addPodcastEpisodes(List<PodcastEpisodeModel> podcastEpisodeModels)
        {
            m_podcastEpisodesSql.InsertAllOnSubmit<PodcastEpisodeModel>(podcastEpisodeModels);
            episodesModelChanged();
        }

        public bool isPodcastInDB(PodcastSubscriptionModel subscription)
        {
            var query = (from PodcastSubscriptionModel s in m_podcastSubscriptionsSql
                         where s.PodcastShowLink.Equals(subscription.PodcastShowLink)
                         select new
                         {
                             url = s.PodcastShowLink
                         }).FirstOrDefault();

            if (query == null)
            {
                return false;
            }

            return true;
        }

        public List<PodcastEpisodeModel> episodesForSubscription(PodcastSubscriptionModel subscription)
        {
/*            var query = from PodcastEpisodeModel episode in m_podcastEpisodesSql
                        where episode.PodcastId == subscription.PodcastId
                        orderby episode.EpisodePublished descending
                        select episode;
            */
            return subscription.Episodes.ToList();
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations
        private const string m_connectionString = "Data Source=isostore:/Podcatcher.sdf";

        private static PodcastSqlModel m_instance = null;
        private Table<PodcastSubscriptionModel> m_podcastSubscriptionsSql;
        private Table<PodcastEpisodeModel> m_podcastEpisodesSql;

        private PodcastSqlModel()
            : base(m_connectionString)
        {
            if (DatabaseExists() == false)
            {
                CreateDatabase();
            }

            m_podcastSubscriptionsSql = GetTable<PodcastSubscriptionModel>();
            m_podcastEpisodesSql = GetTable<PodcastEpisodeModel>();
        }

        private bool isValidSubscriptionModelIndex(int index)
        {
            if (index > m_podcastSubscriptions.Count)
            {
                Debug.WriteLine("ERROR: Cannot fetch podcast subscription with index " + index + ". Model size: " + m_podcastSubscriptions.Count);
                return false;
            }

            if (index < 0)
            {
                Debug.WriteLine("ERROR: Cannot fetch podcast subscription with index " + index);
                return false;
            }

            return true;
        }

        private void subscriptionModelChanged()
        {
            SubmitChanges();
            NotifyPropertyChanged("PodcastSubscriptions");
        }

        private void episodesModelChanged()
        {
            SubmitChanges();
            NotifyPropertyChanged("PodcastEpisodes");
        }

        #region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #endregion

    }
}
