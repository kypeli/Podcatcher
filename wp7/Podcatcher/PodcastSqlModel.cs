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
            PodcastSubscriptionModel model = (from s in Subscriptions
                                              where s.PodcastId.Equals(index)
                                              select s).Single();

            return model;
        }

        public void addSubscription(PodcastSubscriptionModel podcastModel)
        {
            Subscriptions.InsertOnSubmit(podcastModel);
            subscriptionModelChanged();
        }

        public void deleteSubscription(PodcastSubscriptionModel podcastModel)
        {

            var queryDelEpisodes = from episode in Episodes
                                   where episode.PodcastId.Equals(podcastModel.PodcastId)
                                   select episode;

            foreach (var episode in queryDelEpisodes)
            {
                Episodes.DeleteOnSubmit(episode);
            }

            var queryDelSubscription = (from subscription in Subscriptions
                                        where subscription.PodcastId.Equals(podcastModel.PodcastId)
                                        select subscription).First();
            
            Subscriptions.DeleteOnSubmit(queryDelSubscription);

            SubmitChanges();
            NotifyPropertyChanged("PodcastSubscriptions");
        }

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
            if (newPodcastEpisodes.Count < 1)
            {
                return;
            }

            Debug.WriteLine("Writing {0} new episodes to SQL.", newPodcastEpisodes.Count);

            foreach (PodcastEpisodeModel episode in newPodcastEpisodes)
            {
                episode.PodcastId = subscriptionModel.PodcastId;
                subscriptionModel.Episodes.Add(episode);
            }

            this.SubmitChanges();
        }

        public List<PodcastEpisodeModel> episodesForSubscription(PodcastSubscriptionModel subscriptionModel)
        {
            var query = from PodcastEpisodeModel episode in subscriptionModel.Episodes
                        orderby episode.EpisodePublished descending
                        select episode;

            return new List<PodcastEpisodeModel>(query);
        }


        public List<PodcastEpisodeModel> episodesForSubscriptionId(int podcastId)
        {
            var query = from PodcastEpisodeModel episode in Episodes
                        where episode.PodcastId == podcastId
                        orderby episode.EpisodePublished descending
                        select episode;

            return new List<PodcastEpisodeModel>(query);
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
