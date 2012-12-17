using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.ComponentModel;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone.BackgroundTransfer;
using System.Windows;
using Coding4Fun.Phone.Controls;



namespace Podcatcher.ViewModels
{
    [Table]
    public class LastPlayedEpisodeModel
    {
        private int m_historyId;
        [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
        public int LastPlayedID
        {
            get { return m_historyId; }
            set { m_historyId = value; }
        }

        private EntityRef<PodcastEpisodeModel> m_playHistoryEpisode = new EntityRef<PodcastEpisodeModel>();
        [Association(Storage = "m_playHistoryEpisode", ThisKey = "LastPlayedID", OtherKey = "EpisodeId")]
        public PodcastEpisodeModel LastPlayedEpisode
        {
            get { return m_playHistoryEpisode.Entity; }
            set { m_playHistoryEpisode.Entity = value; }
        }

        [Column]
        public DateTime TimeStamp
        {
            get;
            set;
        }

#region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;
        
        private void NotifyPropertyChanging()
        {
            if ((this.PropertyChanging != null))
            {
                this.PropertyChanging(this, null);
            }
        }

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
