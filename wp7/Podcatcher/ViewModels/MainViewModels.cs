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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data.Linq;
using System.Diagnostics;

namespace Podcatcher.ViewModels
{
    public class MainViewModels : INotifyPropertyChanged
    {
        /************************************* Public properties *******************************/

        private ObservableCollection<PodcastSubscriptionModel> m_podcastSubscriptions = null;
        public ObservableCollection<PodcastSubscriptionModel> PodcastSubscriptions
        {
            get
            {
                if (m_podcastSubscriptions == null) {
                    using (var db = new PodcastSqlModel())
                    {
                        var query = from PodcastSubscriptionModel podcastSubscription in db.Subscriptions
                                    orderby podcastSubscription.PodcastName
                                    select podcastSubscription;

                        m_podcastSubscriptions = null;
                        m_podcastSubscriptions = new ObservableCollection<PodcastSubscriptionModel>(query);
                    }
                }

                return m_podcastSubscriptions;
            }

            set
            {
                NotifyPropertyChanged("PodcastSubscriptions");
            }
        }

        public ObservableCollection<PodcastEpisodeModel> LatestEpisodesListProperty
        {
            get
            {
                using (var db = new PodcastSqlModel())
                {
                    List<PodcastEpisodeModel> latest = db.Episodes.OrderByDescending(ep => ep.EpisodePublished).Take(10).ToList<PodcastEpisodeModel>();
                    return new ObservableCollection<PodcastEpisodeModel>(latest);
                }
            }

            set
            {
                NotifyPropertyChanged("LatestEpisodesListProperty");
            }
        }

        private ObservableCollection<PlaylistItem> m_playQueue = new ObservableCollection<PlaylistItem>();
        public ObservableCollection<PlaylistItem> PlayQueue
        {
            get
            {
                return m_playQueue;
            }

            set
            {
                using (var db = new PlaylistDBContext())
                {
                    var query = from PlaylistItem e in db.Playlist
                                orderby e.OrderNumber
                                select e;

                    m_playQueue = null;
                    m_playQueue = new ObservableCollection<PlaylistItem>(query);
                }

                NotifyPropertyChanged("PlayQueue");
            }
        }

        public int PlaylistSortOrder
        {
            get
            {
                using (var db = new PodcastSqlModel())
                {
                    return db.settings().PlaylistSortOrder;
                }
            }

            set
            {
                using (var db = new PodcastSqlModel())
                {
                    SettingsModel s = db.settings();
                    s.PlaylistSortOrder = value;
                    db.SubmitChanges();
                }
            }
        }

        private List<PodcastEpisodeModel> createPlayHistory()
        {
            List<PodcastEpisodeModel> playHistory = new List<PodcastEpisodeModel>();

            using (var db = new PodcastSqlModel())
            {
                var query = from LastPlayedEpisodeModel e in db.PlayHistory
                            orderby e.TimeStamp descending
                            select e;

                int itemsCount = 0;
                foreach (LastPlayedEpisodeModel e in query)
                {
                    PodcastEpisodeModel episode = db.episodeForEpisodeId(e.LastPlayedEpisodeId);
                    if (episode == null)
                    {
                        Debug.WriteLine("Got NULL episode for play history. This probably means the subscription has been deleted.");
                        continue;
                    }

                    playHistory.Add(episode);

                    itemsCount++;
                    if (itemsCount >= 4)
                    {
                        break;
                    }
                }

                return playHistory;
            }
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
    }

}
