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
            set 
            {
                m_episodeFile = value;
                if (m_episodePlayState == EpisodePlayStateEnum.Idle
                    && String.IsNullOrEmpty(EpisodeFile) == false)
                {
                    EpisodePlayState = EpisodePlayStateEnum.Downloaded;
                    EpisodeDownloadState = EpisodeDownloadStateEnum.Downloaded;
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
                    NotifyPropertyChanged("ProgressBarValue");
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

        public enum EpisodePlayStateEnum
        {
            Idle,
            Downloaded,
            Playing,
            Paused,
            Streaming
        };

        public enum EpisodeDownloadStateEnum
        {
            Idle,
            Queued,
            Downloading,
            Downloaded,
            WaitingForWiFi,
            WaitingForWifiAndPower
        };


        private EpisodeDownloadStateEnum m_episodeDownloadState;
        public EpisodeDownloadStateEnum EpisodeDownloadState
        {
            get 
            { 
                return m_episodeDownloadState; 
            }

            set
            {
                m_episodeDownloadState = value;

                NotifyPropertyChanged("ShouldShowDownloadButton");
                NotifyPropertyChanged("EpisodeDownloadState");
                NotifyPropertyChanged("ProgressBarIsVisible");
                NotifyPropertyChanged("ProgressBarValue");
                NotifyPropertyChanged("EpisodeStatusText");
                if (PodcastSubscription != null)
                {
                    // No notify that the PlayableEpisodes list could have been chnaged, so it needs to be re-set.
                    PodcastSubscription.PlayableEpisodes = new List<PodcastEpisodeModel>();
                }
            }
        }

        private EpisodePlayStateEnum m_episodePlayState;
        public EpisodePlayStateEnum EpisodePlayState
        {
            get 
            {
                return m_episodePlayState; 
            }

            set
            {
                m_episodePlayState = value;
                NotifyPropertyChanged("EpisodePlayState");
                NotifyPropertyChanged("ProgressBarIsVisible");
                NotifyPropertyChanged("EpisodeStatusText");
                if (PodcastSubscription != null)
                {
                    // No notify that the PlayableEpisodes list could have been chnaged, so it needs to be re-set.
                    PodcastSubscription.PlayableEpisodes = new List<PodcastEpisodeModel>();
                }
            }
        }

        public String EpisodeDownloadStatusText
        {
            get 
            {
                String statusText = "";
                switch (m_episodeDownloadState)
                {
                    case EpisodeDownloadStateEnum.Downloading:
                        statusText = "Downloading...";
                        break;
                    case EpisodeDownloadStateEnum.Idle:
                        statusText = "";
                        break;
                    case EpisodeDownloadStateEnum.Queued:
                        statusText = "Queued";
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

        // I should do this in a Converter from XAML, but as this is dependant of two properties,
        // it's just easier to do it this way. 
        private Visibility m_progressBarIsVisible = Visibility.Collapsed;
        public Visibility ProgressBarIsVisible
        {
            get
            {
                m_progressBarIsVisible = (m_episodeDownloadState == EpisodeDownloadStateEnum.Downloading
                                          || m_episodePlayState == EpisodePlayStateEnum.Downloaded
                                          || m_episodePlayState == EpisodePlayStateEnum.Paused) ?
                                          Visibility.Visible :
                                          Visibility.Collapsed;

                return m_progressBarIsVisible;
            }

            private set { }
        }

        // And this too.
        private Visibility m_shouldShowDownloadButton = Visibility.Collapsed;
        public Visibility ShouldShowDownloadButton
        {

            get
            {
                m_shouldShowDownloadButton = playableMimeType(EpisodeFileMimeType)
                                             && (EpisodeDownloadState == EpisodeDownloadStateEnum.Idle 
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.Downloading 
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.WaitingForWiFi
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.WaitingForWifiAndPower
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.Queued)   ?
                                             Visibility.Visible                                            :
                                             Visibility.Collapsed;

                return m_shouldShowDownloadButton;                                                   
            }

            private set { }
 
        }

        private bool playableMimeType(string episodeMimeType)
        {
            if (episodeMimeType == "-ERROR-")
            {
                return false;
            }

            // Since we added the MIME type in version 2 of DB, we have to assume that if the 
            // value is empty, we show the button.
            if (String.IsNullOrEmpty(episodeMimeType))
            {
                return true;
            }

            bool playable = false;
            switch (episodeMimeType)
            {
                case "audio/mpeg":
                case "audio/mp3":
                case "audio/x-mp3":
                case "audio/mpeg3":
                case "audio/x-mpeg3":
                case "audio/mpg":
                case "audio/x-mpg":
                case "audio/x-mpegaudio":
                case "audio/x-m4a":
                    playable = true;
                    break;

                case "video/mp4":
                case "video/x-mp4":
                    playable = true;
                    break;
            }

            return playable;
        }

        public bool isPlayable() {
            return playableMimeType(m_episodeFileMimeType);
        }

        private double m_progressBarValue;
        public double ProgressBarValue
        {
            get
            {
                if (m_episodeDownloadState == EpisodeDownloadStateEnum.Downloading
                    || m_episodeDownloadState == EpisodeDownloadStateEnum.Queued)
                {
                    return DownloadPercentage;
                }
                else if (m_episodePlayState == EpisodePlayStateEnum.Downloaded)
                {
                    if (SavedPlayPos > 0 && TotalLengthTicks > 0)
                    {
                        return (((double)SavedPlayPos / (double)TotalLengthTicks) * (double)100);
                    }
                }
                else if (m_episodePlayState == EpisodePlayStateEnum.Playing
                         || m_episodePlayState == EpisodePlayStateEnum.Streaming)
                {
                    return m_progressBarValue;
                }

                return 0.0;
            }

            set 
            {
                if (m_episodePlayState == EpisodePlayStateEnum.Playing 
                    || m_episodePlayState == EpisodePlayStateEnum.Streaming)
                {
                    m_progressBarValue = value;
                    NotifyPropertyChanged("ProgressBarValue");
                }
            }
        }

        public String EpisodeStatusText
        {
            get 
            {
                String text = "";
                switch (m_episodeDownloadState)
                {
                    case EpisodeDownloadStateEnum.Downloading:
                        text = "Downloading...";
                        break;
                    case EpisodeDownloadStateEnum.Queued:
                        text = "Queued.";
                        break;
                    case EpisodeDownloadStateEnum.WaitingForWiFi:
                        text = "Waiting for WiFi.";
                        break;
                    case EpisodeDownloadStateEnum.WaitingForWifiAndPower:
                        text = "Waiting for WiFi and external power.";
                        break;
                }

                if (String.IsNullOrEmpty(text) == false)
                {
                    return text;
                }

                switch (m_episodePlayState)
                {
                    case EpisodePlayStateEnum.Idle:
                    case EpisodePlayStateEnum.Downloaded:
                        if (String.IsNullOrEmpty(m_episodeRunningTime) == false)
                        {
                            text = String.Format("Duration: {0}", m_episodeRunningTime);
                        }
                        break;                        
                    case EpisodePlayStateEnum.Playing:
                        text = "Playing locally.";
                        break;
                    case EpisodePlayStateEnum.Streaming:
                        text = "Playing remotely.";
                        break;
                    case EpisodePlayStateEnum.Paused:
                        text = "Paused.";
                        break;
                }

                return text;
            }

            private set { }
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
                    EpisodeFile = null;
                    EpisodePlayState = EpisodePlayStateEnum.Idle;
                    EpisodeDownloadState = EpisodeDownloadStateEnum.Idle;
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

                    EpisodeDownloadState = EpisodeDownloadStateEnum.Idle;
                    EpisodePlayState = EpisodePlayStateEnum.Idle;
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
            EpisodePlayState =  EpisodePlayStateEnum.Downloaded;
            EpisodeDownloadState = EpisodeDownloadStateEnum.Idle;
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
