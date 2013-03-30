/**
 * Copyright (c) 2012, 2013, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
    public class PodcastSqlModel : DataContext
    {
        public Table<PodcastSubscriptionModel> Subscriptions;
        public Table<PodcastEpisodeModel> Episodes;
        public Table<SettingsModel> Settings;
        public Table<LastPlayedEpisodeModel> PlayHistory;

        /************************************* Public implementations *******************************/
        public PodcastSqlModel()
            : base(m_connectionString)
        {
            Subscriptions = GetTable<PodcastSubscriptionModel>();
            Episodes = GetTable<PodcastEpisodeModel>();
            Settings = GetTable<SettingsModel>();
            PlayHistory = GetTable<LastPlayedEpisodeModel>();
        }

        public void createDB()
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

                if (updater.DatabaseSchemaVersion < 6)
                {
                    updater.AddColumn<SettingsModel>("SelectedExportIndex");
                }

                if (updater.DatabaseSchemaVersion < 7)
                {
                    updater.AddColumn<SettingsModel>("ListenedThreashold");
                    updater.AddColumn<PodcastEpisodeModel>("EpisodeDownloadState");
                    updater.AddColumn<PodcastEpisodeModel>("EpisodePlayState");
                    updater.AddColumn<PodcastSubscriptionModel>("SubscriptionSelectedKeepNumEpisodesIndex");
                    updater.AddColumn<PodcastSubscriptionModel>("SubscriptionIsDeleteEpisodes");
                }

                if (updater.DatabaseSchemaVersion < 8)
                {
                    updater.AddColumn<PodcastSubscriptionModel>("IsContinuousPlayback");
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
            SubmitChanges();
        }

        public void deleteSubscription(PodcastSubscriptionModel podcastModel)
        {
            var queryDelEpisodes = from episode in Episodes
                                   where episode.PodcastId.Equals(podcastModel.PodcastId)
                                   select episode;

            foreach (var episode in queryDelEpisodes)
            {
                using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        Debug.WriteLine("Deleting downloaded episode: " + episode.EpisodeFile);
                        if (String.IsNullOrEmpty(episode.EpisodeFile) == false
                            && episodeStore.FileExists(episode.EpisodeFile))
                        {
                            episodeStore.DeleteFile(episode.EpisodeFile);
                        }
                    }
                    catch (IsolatedStorageException)
                    {
                    }
                }

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
                Episodes.InsertOnSubmit(episode);
                subscriptionModel.Episodes.Add(episode);
            }

            SubmitChanges();
        }

        public List<PodcastEpisodeModel> episodesForSubscription(PodcastSubscriptionModel subscriptionModel)
        {
            var query = from PodcastEpisodeModel episode in Episodes
                        where episode.PodcastId == subscriptionModel.PodcastId
                        orderby episode.EpisodePublished descending
                        select episode;

            List<PodcastEpisodeModel> episodes = new List<PodcastEpisodeModel>(query); 
            return episodes;
        }

        public List<PodcastEpisodeModel> episodesForSubscriptionId(int podcastId)
        {
            var query = from PodcastEpisodeModel episode in Episodes
                        where episode.PodcastId == podcastId
                        orderby episode.EpisodePublished descending
                        select episode;

            return new List<PodcastEpisodeModel>(query);
        }

        public IEnumerable<PodcastEpisodeModel> playableEpisodesForSubscription(PodcastSubscriptionModel subscription)
        {
            var query = from PodcastEpisodeModel episode in Episodes
                        where (episode.PodcastId == subscription.PodcastId
                               && episode.EpisodeFile != ""
                               && (episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded
                               || episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Playing
                               || episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Listened
                               || episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Paused))
                        orderby episode.EpisodePublished descending
                        select episode;

            return query;
        }

        public List<PodcastEpisodeModel> unplayedEpisodesForSubscription(PodcastSubscriptionModel subscription)
        {
            var query = from episode in Episodes
                        where (episode.PodcastId == subscription.PodcastId 
                               && episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded
                               && episode.EpisodePlayState != PodcastEpisodeModel.EpisodePlayStateEnum.Listened
                               && episode.SavedPlayPos == 0)
                        select episode;

            return new List<PodcastEpisodeModel>(query);
        }

        public List<PodcastEpisodeModel> unplayedEpisodesForSubscription(int subscriptionId)
        {
            var query = from episode in Episodes
                        where (episode.PodcastId == subscriptionId
                               && episode.EpisodePlayState == PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded
                               && episode.EpisodePlayState != PodcastEpisodeModel.EpisodePlayStateEnum.Listened
                               && episode.SavedPlayPos == 0)
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

        public List<PodcastEpisodeModel> allEpisodes()
        {
            var query = from PodcastEpisodeModel episode in Episodes
                        where episode.EpisodeFile != ""
                        select episode;

            return query.ToList<PodcastEpisodeModel>();
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
                return;
            }

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
        }

        /************************************* Private implementation *******************************/
        #region privateImplementations
        private const string m_connectionString = "Data Source=isostore:/Podcatcher.sdf";

        private const int DB_VERSION = 8;

        private bool isValidSubscriptionModelIndex(int index)
        {
            if (index > App.mainViewModels.PodcastSubscriptions.Count)
            {
                Debug.WriteLine("ERROR: Cannot fetch podcast subscription with index " + index);
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

        internal void cleanListenedEpisodes(PodcastSubscriptionModel podcastSubscriptionModel)
        {
            float listenedEpisodeThreshold = 0.0F;
            using (var db = new PodcastSqlModel())
            {
                listenedEpisodeThreshold = (float)db.settings().ListenedThreashold / (float)100.0;
            }

            var queryDelEpisodes = from episode in podcastSubscriptionModel.Episodes
                                   where (episode.EpisodeFile != ""
                                          && (episode.TotalLengthTicks > 0 && episode.SavedPlayPos > 0)
                                          && ((float)((float)episode.SavedPlayPos / (float)episode.TotalLengthTicks) > listenedEpisodeThreshold))
                                   select episode;

            foreach (var episode in queryDelEpisodes)
            {
                if (episode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
                {
                    episode.deleteDownloadedEpisode();
                    SubmitChanges();
                }
            }
        }

        internal PodcastEpisodeModel episodesForTitle(String episodeTitle)
        {
            PodcastEpisodeModel episode = (from   e in Episodes
                                           where  e.EpisodeName == episodeTitle
                                           select e).FirstOrDefault();

            return episode;
        }

        public void deleteEpisodesPerQuery(IEnumerable<PodcastEpisodeModel> query)
        {
            Episodes.DeleteAllOnSubmit(query);
            SubmitChanges();
        }

        #endregion
    }
}
