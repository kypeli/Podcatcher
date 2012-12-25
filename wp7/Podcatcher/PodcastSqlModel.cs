/**
 * Copyright (c) 2012, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the <organization> nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Podcatcher.ViewModels;
using System.Data.Linq;
using Microsoft.Phone.Data.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

namespace Podcatcher
{
    public class PodcastSqlModel : DataContext, INotifyPropertyChanged
    {
        private const int DB_VERSION = 5;

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

        private ObservableCollection<PodcastEpisodeModel> m_playHistory = new ObservableCollection<PodcastEpisodeModel>(); 
        public ObservableCollection<PodcastEpisodeModel> PlayHistoryListProperty
        {
            get
            {
                var query = from LastPlayedEpisodeModel e in PlayHistory
                            orderby e.TimeStamp descending
                            select e;

                m_playHistory.Clear();
                int itemsCount = 0;
                foreach (LastPlayedEpisodeModel e in query)
                {
                    PodcastEpisodeModel episode = PodcastSqlModel.getInstance().episodeForEpisodeId(e.LastPlayedEpisodeId);
                    if (episode == null)
                    {
                        Debug.WriteLine("Got NULL episode for play history. This probably means the subscription has been deleted.");
                        continue;
                    }

                    m_playHistory.Add(episode);
                    
                    itemsCount++;
                    if (itemsCount >= 4)
                    {
                        break;
                    }
                }

                return m_playHistory;
            }

            private set { }
        }

        public int PlayHistoryListCount
        {
            get
            {
                return m_playHistory.Count();
            }
        }

        /************************************* Public implementations *******************************/
        public delegate void PodcastSqlHandler(object source, PodcastSqlHandlerArgs e);
        public class PodcastSqlHandlerArgs
        {
            public enum SqlOperation
            {
                DeleteSubscriptionStarted,
                DeleteSubscriptionFinished
            }

            public SqlOperation operationStatus;
        }

        public event PodcastSqlHandler OnPodcastSqlOperationChanged;

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
            PodcastSqlHandlerArgs args = new PodcastSqlHandlerArgs();
            args.operationStatus = PodcastSqlHandlerArgs.SqlOperation.DeleteSubscriptionStarted;
            this.OnPodcastSqlOperationChanged(this, args);

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(deleteSubscriptionFromDB);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(deleteSubscriptionFromDBCompleted);
            worker.RunWorkerAsync(podcastModel);
        }

        void deleteSubscriptionFromDB(object sender, DoWorkEventArgs e)
        {
            PodcastSubscriptionModel podcastModel = e.Argument as PodcastSubscriptionModel;

            var queryDelEpisodes = from episode in Episodes
                                   where episode.PodcastId.Equals(podcastModel.PodcastId)
                                   select episode;

            foreach (var episode in queryDelEpisodes)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    episode.deleteDownloadedEpisode();
                });

                Episodes.DeleteOnSubmit(episode);
            }

            var queryDelSubscription = (from subscription in Subscriptions
                                        where subscription.PodcastId.Equals(podcastModel.PodcastId)
                                        select subscription).First();

            Subscriptions.DeleteOnSubmit(queryDelSubscription);
            SubmitChanges();
        }

        public void deleteEpisodeFromDB(PodcastEpisodeModel episode)
        {
            var queryDelEpisode = (from e in Episodes
                                   where episode.EpisodeId.Equals(episode.EpisodeId)
                                   select episode).FirstOrDefault();

            Episodes.DeleteOnSubmit(queryDelEpisode);
            SubmitChanges();
        }

        void deleteSubscriptionFromDBCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            PodcastSqlHandlerArgs args = new PodcastSqlHandlerArgs();
            args.operationStatus = PodcastSqlHandlerArgs.SqlOperation.DeleteSubscriptionFinished;
            this.OnPodcastSqlOperationChanged(this, args);

            NotifyPropertyChanged("PodcastSubscriptions");
        }

        public bool isPodcastInDB(string subscriptionRssUrl)
        {
            var query = (from PodcastSubscriptionModel s in Subscriptions
                         where s.PodcastRSSUrl.Equals(subscriptionRssUrl)
                         select new
                         {
                             url = s.PodcastRSSUrl
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

        public PodcastEpisodeModel episodeForEpisodeId(int episodeId)
        {
            PodcastEpisodeModel model = (from PodcastEpisodeModel episode in Episodes
                                         where episode.EpisodeId == episodeId
                                         select episode).FirstOrDefault();

            if (model == null)
            {
                Debug.WriteLine("Warning: Podcast episode with id {0} returned null!", episodeId);
            }

            return model;
        }

        public SettingsModel settings()
        {
            SettingsModel settingsModel = (from SettingsModel s in Settings
                                     select s).FirstOrDefault();

            if (settingsModel == null)
            {
                createSettings();
                return settings();
            }

            return settingsModel;
        }

        public void addEpisodeToPlayHistory(PodcastEpisodeModel episode)
        {
            if (episode == null)
            {
                Debug.WriteLine("Warning: Trying to add NULL episode to play history.");
                return;
            }

            LastPlayedEpisodeModel existingItem = (from LastPlayedEpisodeModel e in PlayHistory
                                                   where e.LastPlayedEpisodeId == episode.EpisodeId
                                                   select e).FirstOrDefault();

            // Episode is already in play history. Just update the timestamp instead of adding a duplicate one. 
            if (existingItem != null)
            {
                Debug.WriteLine("Found episode already in history. Updating timestamp. Name: " + episode.EpisodeName);
                existingItem.TimeStamp = DateTime.Now;
                existingItem.LastPlayedEpisodeId = episode.EpisodeId;
                SubmitChanges();
                NotifyPropertyChanged("PlayHistoryListProperty");
                return;
            }

            Debug.WriteLine("Play history count: " + PlayHistory.Count());
            // Clean old history items (if we have more than 10).
            if (PlayHistory.Count() >= 10)
            {
                var oldestHistoryItems = (from LastPlayedEpisodeModel e in PlayHistory
                                         orderby e.TimeStamp descending
                                         select e).Skip(10);

                if (oldestHistoryItems != null)
                {
                    Debug.WriteLine("Cleaning old episode from history.");
                    PlayHistory.DeleteAllOnSubmit(oldestHistoryItems);
                }
            }

            // Add a new item.
            LastPlayedEpisodeModel newHistoryItem = new LastPlayedEpisodeModel();
            newHistoryItem.LastPlayedEpisodeId = episode.EpisodeId;
            newHistoryItem.TimeStamp = DateTime.Now;

            Debug.WriteLine("Inserting episode to history: " + episode.EpisodeName);

            PlayHistory.InsertOnSubmit(newHistoryItem);
            SubmitChanges();

            NotifyPropertyChanged("PlayHistoryListProperty");
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations
        private const string m_connectionString = "Data Source=isostore:/Podcatcher.sdf";

        private static PodcastSqlModel m_instance = null;
        public Table<PodcastSubscriptionModel> Subscriptions;
        public Table<PodcastEpisodeModel> Episodes;
        public Table<SettingsModel> Settings;
        public Table<LastPlayedEpisodeModel> PlayHistory;

        private PodcastSqlModel()
            : base(m_connectionString)
        {
            DatabaseSchemaUpdater updater = null;
            if (DatabaseExists() == false)
            {
                CreateDatabase();
                updater = Microsoft.Phone.Data.Linq.Extensions.CreateDatabaseSchemaUpdater(this);
                updater.DatabaseSchemaVersion = DB_VERSION;
                updater.Execute();

                // Here we can determine if this was a new install or not. If yes, we install a key 
                // to count how many times the app has been restarted.
                IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                settings.Add(App.LSKEY_PODCATCHER_STARTS, 1);
                settings.Save();
            }


            if (updater == null)
            {
                updater = Microsoft.Phone.Data.Linq.Extensions.CreateDatabaseSchemaUpdater(this);

                if (updater.DatabaseSchemaVersion < 2)
                {
                    // Added in version 2 (release 1.1.0.0)
                    //  - PodcastEpisodeModel.EpisodeFileMimeType
                    updater.AddColumn<PodcastEpisodeModel>("EpisodeFileMimeType");
                }

                if (updater.DatabaseSchemaVersion < 3)
                {
                    updater.AddColumn<PodcastEpisodeModel>("TotalLengthTicks");
                }

                if (updater.DatabaseSchemaVersion < 4)
                {
                    updater.AddTable<SettingsModel>();
                }

                if (updater.DatabaseSchemaVersion < 5)
                {
                    updater.AddColumn<PodcastSubscriptionModel>("Username");
                    updater.AddColumn<PodcastSubscriptionModel>("Password");
                    updater.AddTable<LastPlayedEpisodeModel>();
                }

                updater.DatabaseSchemaVersion = DB_VERSION;
                updater.Execute();
            }

            // Force to check if we have tables or not.
            try
            {
                IEnumerator<PodcastEpisodeModel> enumEntityEpisodes = Episodes.GetEnumerator();
                IEnumerator<PodcastSubscriptionModel> enumEntitySubscriptions = Subscriptions.GetEnumerator();
            }
            catch (Exception)
            {
                Debug.WriteLine("Got exception while asking for table enumerator. Table probably doesn't exist...");

                DeleteDatabase();
                CreateDatabase();
                SubmitChanges();

                Debug.WriteLine("Recreated database.");
            }

            Subscriptions = GetTable<PodcastSubscriptionModel>();
            Episodes = GetTable<PodcastEpisodeModel>();
            Settings = GetTable<SettingsModel>();
            PlayHistory = GetTable<LastPlayedEpisodeModel>();
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

        internal void createSettings()
        {
            Settings.InsertOnSubmit(new SettingsModel());
            SubmitChanges();
        }

        private void subscriptionModelChanged()
        {
            SubmitChanges();
            NotifyPropertyChanged("PodcastSubscriptions");
        }

        internal void startOldEpisodeCleanup(PodcastSubscriptionModel podcastSubscriptionModel)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(oldEpisodeCleanup);
            worker.RunWorkerAsync(podcastSubscriptionModel);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(oldEpisodeCleanupCompleted);
        }

        private void oldEpisodeCleanup(object sender, DoWorkEventArgs args)
        {
            PodcastSubscriptionModel subscription = args.Argument as PodcastSubscriptionModel;
            var queryDelEpisodes = from episode in Episodes
                                   where (episode.PodcastId.Equals(subscription.PodcastId)      
                                          && episode.EpisodeFile != ""
                                          && (episode.TotalLengthTicks > 0 && episode.SavedPlayPos > 0)
                                          && ((float)((float)episode.SavedPlayPos/(float)episode.TotalLengthTicks) > 0.9 ))
                                   select episode;

            foreach (var episode in queryDelEpisodes)
            {
                if (episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                episode.deleteDownloadedEpisode();
                            });
                }
            }
        }

        private void oldEpisodeCleanupCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SubmitChanges();
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
