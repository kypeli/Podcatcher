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
    public class PodcastEpisodeModel : INotifyPropertyChanged
    {
        private const int NVARCHAR_MAX = 4000;

        /****************************** PodcastEpisodeHandler definitions ***************************/

        public delegate void PodcastEpisodesHandler(object source, PodcastEpisodesArgs e);

        public class PodcastEpisodesArgs
        {
        }

        /************************************* Public properties *******************************/
        #region properties

        private int m_episodeId;
        [Column(Storage = "m_episodeId", IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
        public int EpisodeId
        {
            get { return m_episodeId; }
            set { m_episodeId = value; }
        }

        private EntityRef<PodcastSubscriptionModel> m_podcast = new EntityRef<PodcastSubscriptionModel>();
        [Association(Storage = "m_podcast", ThisKey="PodcastId", OtherKey = "PodcastId", IsForeignKey = true)]
        public PodcastSubscriptionModel PodcastSubscription
        {
            get { return m_podcast.Entity; }
            set { m_podcast.Entity = value; }
        }

        private int m_podcastId;
        [Column(Storage = "m_podcastId", UpdateCheck = UpdateCheck.Never)]
        public int PodcastId
        {
            get { return m_podcastId; }
            set { m_podcastId = value; }
        }

        private string m_name;
        [Column(UpdateCheck=UpdateCheck.Never)]
        public String EpisodeName
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private string m_description;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeDescription
        {
            get 
            {
                string desc = m_description;
                if (!String.IsNullOrEmpty(desc))
                {
                    desc = Converters.HtmlRemoval.StripTagsCharArray(desc);
                }

                return desc; 
            }

            set 
            {
                if (String.IsNullOrEmpty(value))
                {
                    return;
                }

                // Stupid MSQL!!
                // Yes, there's a 8kB limit per row in SQL so we want to make sure we don't 
                // hit that, and hence take first 4kB of the description.
                if (value.Length > 3000)
                {
                    value = value.Substring(0, 3000);
                }

                m_description = value; 
            }
        }

        private DateTime m_published;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public DateTime EpisodePublished 
        {
            get { return m_published; }
            set { m_published = value; }
        }

        public String EpisodePublishedString
        {
            get 
            {
                if (m_published == null)
                {
                    return "";
                }

                string format = "dd MMM yyyy";
                return m_published.ToString(format); 
            }
        }
        
        private string m_episodeDownloadUrl;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public string EpisodeDownloadUri
        {
            get { return m_episodeDownloadUrl; }
            set { m_episodeDownloadUrl = value; }
        }

        private long m_episodeDownloadSize = 0;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public long EpisodeDownloadSize
        {
            get { return m_episodeDownloadSize; }
            set { m_episodeDownloadSize = value; }
        }

        private String m_episodeRunningTime;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeRunningTime
        {
            get { return m_episodeRunningTime; }
            set { m_episodeRunningTime = value; }
        }

        private String m_episodeFile;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeFile
        {
            get { return m_episodeFile; }
            set { 
                m_episodeFile = value;
                EpisodeState = EpisodeStateEnum.Idle;
                if (m_episodeFile != null)
                {
                    EpisodeState = EpisodeStateEnum.Playable;
                }
            }
        }

        private String m_episodeFileMimeType = "";
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeFileMimeType
        {
            get { return m_episodeFileMimeType; }
            set { m_episodeFileMimeType = value; }
        }
        
        private long m_episodePlayBookmark;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public long EpisodePlayBookmark
        {
            get { return m_episodePlayBookmark; }
            set { m_episodePlayBookmark = value; }
        }

        private int m_downloadPercentage;
        public int DownloadPercentage
        {
            get
            {
                return m_downloadPercentage;
            }

            set
            {
                if (m_downloadPercentage != value)
                {
                    m_downloadPercentage = value;
                    NotifyPropertyChanged("DownloadPercentage");
                }
            }
        }

        private long m_savedPlayPos = 0;
        [Column(UpdateCheck = UpdateCheck.Never)]
        public long SavedPlayPos
        {
            get { return m_savedPlayPos; }
            set { m_savedPlayPos = value; }
        }

        private long m_totalLengthTicks = 0;
        [Column(UpdateCheck = UpdateCheck.Never, DbType = "BIGINT DEFAULT 0 NOT NULL")] 
        public long TotalLengthTicks
        {
            get { return m_totalLengthTicks; }
            set { m_totalLengthTicks = value; }
        }

        public enum EpisodeStateEnum
        {
            Idle,
            Queued,
            Downloading,
            Saving,
            Playable,
            Playing,
            Paused
        };

        private EpisodeStateEnum m_episodeState;
        public EpisodeStateEnum EpisodeState
        {
            get 
            { 
                return m_episodeState; 
            }

            set
            {
                m_episodeState = value;

                NotifyPropertyChanged("EpisodeState");
                NotifyPropertyChanged("EpisodeStatusText");
            }
        }

        public String EpisodeStatusText
        {
            get 
            {
                String statusText = "";
                switch (m_episodeState)
                {
                    case EpisodeStateEnum.Downloading:
                        statusText = "Downloading...";
                        break;
                    case EpisodeStateEnum.Saving:
                        statusText = "Saving...";
                        break;
                    case EpisodeStateEnum.Idle:
                        statusText = "";
                        break;
                    case EpisodeStateEnum.Playable:
                        statusText = "Play";
                        break;
                    case EpisodeStateEnum.Queued:
                        statusText = "Queued";
                        break;
                    case EpisodeStateEnum.Playing:
                        statusText = "Playing";
                        break;
                    case EpisodeStateEnum.Paused:
                        statusText = "Paused";
                        break;
                }

                return statusText;
            }
        }

        private BackgroundTransferRequest m_transferRequest;
        public BackgroundTransferRequest DownloadRequest
        {
            get
            {
                return m_transferRequest;
            }

            set
            {
                m_transferRequest = value;
                DownloadPercentage = 0;
            }
        }

        private Visibility m_newEpisodeVisibility = Visibility.Collapsed;
        public Visibility NewEpisodeVisibility
        {
            get
            {
                return m_newEpisodeVisibility;
            }

            set
            {
                m_newEpisodeVisibility = value;
            }
        }

        #endregion

        /************************************* Public implementations *******************************/
        public PodcastEpisodeModel()
        {
        }

        public void deleteDownloadedEpisode()
        {
            if (String.IsNullOrEmpty(m_episodeFile)) 
            {
                return;
            }

            using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (episodeStore.FileExists(EpisodeFile) == false)
                {
                    // If we cannot find the episode file to delete, then we at least have to reset the episode state 
                    // back to idle.
                    Debug.WriteLine("WARNING: Could not find downloaded episode to delete. Name: " + EpisodeFile);
                    EpisodeState = EpisodeStateEnum.Idle;
                    EpisodeFile = null;
                    return;
                }

                Debug.WriteLine("Deleting downloaded episode: " + EpisodeFile);

                try
                {
                    episodeStore.DeleteFile(EpisodeFile);
                    EpisodeFile = null;
                    PodcastSubscription.UnplayedEpisodes--;
                    SavedPlayPos = 0;
                    TotalLengthTicks = 0;
                }
                catch (IsolatedStorageException)
                {
                    ToastPrompt toast = new ToastPrompt();
                    toast.Title = "Error";
                    toast.Message = "Could not delete episode.";

                    toast.Show();                    
                }
            }
        }


        /************************************* Private implementations *******************************/
        #region private
        private void PodcastEpisodeModel_OnPodcastEpisodeFinishedDownloading(object source, PodcastEpisodeModel.PodcastEpisodesArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(savePodcastAsync);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SavePodcastEpisodeCompleted);
            bw.RunWorkerAsync();
            EpisodeState = EpisodeStateEnum.Saving;
        }

        void savePodcastAsync(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Writing episode to disk.");

            using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                byte[] buffer = new byte[4096];
                using (IsolatedStorageFileStream fileStream = episodeStore.OpenFile(EpisodeFile, FileMode.CreateNew))
                {
                    if (fileStream == null)
                    {
                        Debug.WriteLine("ERROR: Isolated storage file stream is NULL!");
                        return;
                    }

                    int bytesRead = 0;
                    while ((bytesRead = m_downloadStream.Read(buffer, 0, 4096)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        void SavePodcastEpisodeCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("Episode written to disk. Filename: {0}", EpisodeFile);
            EpisodeState =  EpisodeStateEnum.Playable;
            m_downloadStream = null;
        }

        #endregion

        #region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private Stream m_downloadStream;
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
