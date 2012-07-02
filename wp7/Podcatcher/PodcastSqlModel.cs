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
                var query = from PodcastSubscriptionModel podcastSubscription in this.Subscriptions
                            orderby podcastSubscription.PodcastName 
                            select podcastSubscription;

                m_podcastSubscriptions = new List<PodcastSubscriptionModel>(query);
          //      Debug.WriteLine("Episodes: " + m_podcastSubscriptions.ElementAt(0).Episodes.Count);
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
            Subscriptions.InsertOnSubmit(podcastModel);
            subscriptionModelChanged();
        }

        public void deleteSubscription(PodcastSubscriptionModel podcastModel)
        {

            Subscriptions.DeleteOnSubmit(podcastModel);
            subscriptionModelChanged();
        }

/*        public void addPodcastEpisodes(List<PodcastEpisodeModel> podcastEpisodeModels)
        {
            m_podcastEpisodesSql.InsertAllOnSubmit<PodcastEpisodeModel>(podcastEpisodeModels);
            episodesModelChanged();
        }
        */
        public bool isPodcastInDB(PodcastSubscriptionModel subscription)
        {
            var query = (from PodcastSubscriptionModel s in Subscriptions
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

        public void insertEpisodesForSubscription(PodcastSubscriptionModel subscriptionModel, List<PodcastEpisodeModel> newPodcastEpisodes)
        {
            // var subscription = this.Subscriptions.Single(s => s.PodcastId == subscriptionModel.PodcastId);

            foreach (PodcastEpisodeModel episode in newPodcastEpisodes)
            {
                subscriptionModel.Episodes.Add(episode);
//                episode.PodcastSubscription = subscription;
            }
            this.SubmitChanges();
        }

        public List<PodcastEpisodeModel> episodesForSubscription(PodcastSubscriptionModel subscriptionModel)
        {
/*            var query = from PodcastEpisodeModel episode in Subscriptions
                        where episode.PodcastId == subscriptionModel.PodcastId
                        orderby episode.EpisodePublished descending
                        select episode;
             var subscription = m_podcastSubscriptionsSql.Single(s => s.PodcastId == m_subscriptionModel.PodcastId);
            */
            var subscription = Subscriptions.Single(s => s.PodcastId == subscriptionModel.PodcastId);
            return subscriptionModel.Episodes.ToList();
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations
        private const string m_connectionString = "Data Source=isostore:/Podcatcher.sdf";

        private static PodcastSqlModel m_instance = null;
        public Table<PodcastSubscriptionModel> Subscriptions;
        public Table<PodcastEpisodeModel> Episodes;

        private PodcastSqlModel()
            : base(m_connectionString)
        {
            if (DatabaseExists() == false)
            {
                CreateDatabase();
            }

            Subscriptions = GetTable<PodcastSubscriptionModel>();
            Episodes = GetTable<PodcastEpisodeModel>();
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
