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
using Microsoft.Phone.BackgroundAudio;
using System.Windows.Threading;

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
                return m_description; 
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
                m_description = Converters.HtmlRemoval.StripTagsCharArray(m_description);
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
                String published = "";
                if (m_published != null)
                {
                    published = m_published.ToString("d"); 
                }

                return published;
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
            set { m_episodeFile = value; }
        }

        private String m_episodeFileMimeType = "";
        [Column(UpdateCheck = UpdateCheck.Never)]
        public String EpisodeFileMimeType
        {
            get { return m_episodeFileMimeType; }
            set { 
                m_episodeFileMimeType = value;
                if (String.IsNullOrEmpty(m_episodeFileMimeType))
                {
                    EpisodePlayState = EpisodePlayStateEnum.NoMedia;
                    EpisodeDownloadState = EpisodeDownloadStateEnum.NoMedia;
                } 
                else if (isPlayable() == false)
                {
                    EpisodePlayState = EpisodePlayStateEnum.UnsupportedFormat;
                    EpisodeDownloadState = EpisodeDownloadStateEnum.UnsupportedFormat;
                }

                NotifyPropertyChanged("EpisodePlayState");
                NotifyPropertyChanged("EpisodeDownloadState");
            }
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
            set {
                if (m_savedPlayPos != value)
                {
                    m_savedPlayPos = value;

                    NotifyPropertyChanged("ProgressBarIsVisible");
                }
            }
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
            Streaming,
            UnsupportedFormat,
            NoMedia,
            Listened
        };

        public enum EpisodeDownloadStateEnum
        {
            Idle,
            Queued,
            Downloading,
            Downloaded,
            WaitingForWiFi,
            WaitingForWifiAndPower,
            UnsupportedFormat,
            NoMedia
        };


        private EpisodeDownloadStateEnum m_episodeDownloadState = EpisodeDownloadStateEnum.Idle;
        [Column(DbType = "INT DEFAULT 0 NOT NULL", UpdateCheck = UpdateCheck.Never)]
        public EpisodeDownloadStateEnum EpisodeDownloadState
        {
            get {
                if (m_episodeDownloadState == EpisodeDownloadStateEnum.Idle 
                    && String.IsNullOrEmpty(m_episodeFile) == false)
                {
                    m_episodeDownloadState = EpisodeDownloadStateEnum.Downloaded;
                }

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
        [Column(DbType = "INT DEFAULT 0 NOT NULL", UpdateCheck = UpdateCheck.Never)]
        public EpisodePlayStateEnum EpisodePlayState
        {
            get { return m_episodePlayState; }

            set
            {
                if (m_episodePlayState == value)
                {
                    return;
                }

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
                if (m_newEpisodeVisibility != value) 
                {
                    m_newEpisodeVisibility = value;
                    NotifyPropertyChanged("NewEpisodeVisibility");
                }
            }
        }

        // I should do this in a Converter from XAML, but as this is dependant of multiple properties,
        // it's just easier to do it this way. 
        private Visibility m_ProgressBarIsVisible;
        public Visibility ProgressBarIsVisible
        {
            get
            {
/*                return (m_episodeDownloadState == EpisodeDownloadStateEnum.Downloading 
                        || ((ProgressBarValue > 0) && EpisodePlayState != EpisodePlayStateEnum.Listened) ? Visibility.Visible : Visibility.Collapsed);'
 */
                return m_ProgressBarIsVisible;
            }

            private set 
            {
                if (value != m_ProgressBarIsVisible)
                {
                    m_ProgressBarIsVisible = value;
                    NotifyPropertyChanged("ProgressBarIsVisible");
                }
            }
        }

        // And this too.
        private Visibility m_shouldShowDownloadButton = Visibility.Collapsed;
        public Visibility ShouldShowDownloadButton
        {

            get
            {
                m_shouldShowDownloadButton = EpisodeDownloadState == EpisodeDownloadStateEnum.Idle 
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.Downloading 
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.WaitingForWiFi
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.WaitingForWifiAndPower
                                             || EpisodeDownloadState == EpisodeDownloadStateEnum.Queued   ?
                                             Visibility.Visible                                           :
                                             Visibility.Collapsed;

                return m_shouldShowDownloadButton;                                                   
            }

            private set { }
 
        }

        private Visibility m_shouldShowPlayButton = Visibility.Collapsed;
        public Visibility ShouldShowPlayButton
        {

            get
            {
                m_shouldShowPlayButton = EpisodePlayState != EpisodePlayStateEnum.UnsupportedFormat
                                         && EpisodePlayState != EpisodePlayStateEnum.NoMedia ?
                                         Visibility.Visible                                         :
                                         Visibility.Collapsed;

                return m_shouldShowPlayButton;
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
                case "audio/mpegaudio":
                case "audio/m4a":
                case "audio/x-mpeg":
                    playable = true;
                    break;

                case "video/mp4":
                case "video/x-mp4":
                case "video/x-mpeg":
                case "video/x-m4v":
                case "video/m4v":
                case "video/mpeg":
                case "video/vnd.objectvideo":
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
                else if (m_episodePlayState == EpisodePlayStateEnum.Playing
                         || m_episodePlayState == EpisodePlayStateEnum.Streaming)
                {
                    return m_progressBarValue * 100;
                }
                else if (SavedPlayPos > 0 && TotalLengthTicks > 0)
                {
                    return (((double)SavedPlayPos / (double)TotalLengthTicks) * (double)100);
                }

                return 0.0;
            }

            set 
            {
                if (m_episodePlayState == EpisodePlayStateEnum.Playing 
                    || m_episodePlayState == EpisodePlayStateEnum.Streaming)
                {
                    NotifyPropertyChanging("ProgressBarValue");
                    m_progressBarValue = value;
                    NotifyPropertyChanged("ProgressBarValue");
                }
            }
        }

        private void DebugOutputEpisode()
        {
            Debug.WriteLine("Object: " + GetHashCode() + " Episode ID: " + EpisodeId);
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
                    case EpisodeDownloadStateEnum.UnsupportedFormat:
                        text = "Unsupported file format.";
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
                        String size = "";
                        if (m_episodeDownloadSize > 0)
                        {
                            size += ((float)m_episodeDownloadSize / (float)(1024 * 1000)).ToString("0.00");
                        }

                        if (String.IsNullOrEmpty(m_episodeRunningTime) == false)
                        {
                            text = String.Format("Duration: {0}, {1} MB", m_episodeRunningTime, size);
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
                    case EpisodePlayStateEnum.UnsupportedFormat:
                        text = "Unsupported file format.";
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
            if (String.IsNullOrEmpty(EpisodeFile))
            {
                return;
            }

            using (var episodeStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    Debug.WriteLine("Deleting downloaded episode: " + EpisodeFile);
                    if (episodeStore.FileExists(EpisodeFile))
                    {
                        episodeStore.DeleteFile(EpisodeFile);
                    }
                    else
                    {
                        Debug.WriteLine("Warning: Cannot delete episode with file: " + EpisodeFile);
                    }
                }
                catch (IsolatedStorageException)
                {
                    ToastPrompt toast = new ToastPrompt();
                    toast.Title = "Error";
                    toast.Message = "Could not delete episode.";

                    toast.Show();
                }
                catch (NullReferenceException)
                {
                    Debug.WriteLine("Got NULL pointer exception when deleting episode.");
                }
                finally
                {
                    SavedPlayPos = 0;
                    TotalLengthTicks = 0;
                    PodcastSubscription.unplayedEpisodesChanged();
                }

                EpisodeFile = "";
                EpisodeDownloadState = EpisodeDownloadStateEnum.Idle;
                EpisodePlayState = EpisodePlayStateEnum.Idle;
            }
        }

        /************************************* Private implementations *******************************/
        #region private
        private BackgroundAudioPlayer m_player = null;
        private static DispatcherTimer m_screenUpdateTimer = null;

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

        internal void markAsListened()
        {
            SavedPlayPos = 0;
            EpisodePlayState = EpisodePlayStateEnum.Listened;
        }

        void SavePodcastEpisodeCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("Episode written to disk. Filename: {0}", EpisodeFile);
            EpisodePlayState =  EpisodePlayStateEnum.Downloaded;
            EpisodeDownloadState = EpisodeDownloadStateEnum.Idle;
            m_downloadStream = null;
        }

        private void episodeStartedPlaying()
        {
            BackgroundAudioPlayer.Instance.PlayStateChanged += PlayStateChanged;

            m_screenUpdateTimer = new DispatcherTimer();
            m_screenUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500); // Fire the timer every half a second.
            m_screenUpdateTimer.Tick += new EventHandler(episodePlayback_Tick);
            m_screenUpdateTimer.Start();
        }

        private void episodeStoppedPlaying()
        {
            m_screenUpdateTimer.Stop();
            m_screenUpdateTimer.Tick -= new EventHandler(episodePlayback_Tick);
            m_screenUpdateTimer = null;

            setNoPlaying();
        }

        private void episodePlayback_Tick(object sender, EventArgs e)
        {
            ProgressBarValue = PodcastPlayerControl.getEpisodePlayPosition();
        }

        private void PlayStateChanged(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.Error != null)
            {
                Debug.WriteLine("PlayStateChanged: Podcast player is no longer available.");
                return;
            }

            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    Debug.WriteLine("Episode: Playing.");
                    if (TotalLengthTicks == 0)
                    {
                        TotalLengthTicks = BackgroundAudioPlayer.Instance.Track.Duration.Ticks;
                    }
                    break;

                case PlayState.Paused:
                    Debug.WriteLine("Episode: Paused.");
                    episodeStoppedPlaying();
                    break;

                case PlayState.Stopped:
                    Debug.WriteLine("Episode: Stopped.");
                    episodeStoppedPlaying();
                    break;
                
                case PlayState.Shutdown:
                    Debug.WriteLine("Episode: Shutdown.");
                    episodeStoppedPlaying();
                    break;
            }
        }

        #endregion

        #region propertyChanged
        public event PropertyChangedEventHandler PropertyChanged; 
        public event PropertyChangingEventHandler PropertyChanging;
        private Stream m_downloadStream;
        private void NotifyPropertyChanging(String propertyName)
        {
            PropertyChangingEventHandler handler = PropertyChanging;
            if (null != handler)
            {
                PropertyChangingEventArgs args = new PropertyChangingEventArgs(propertyName);
                handler(this, args);
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


        internal void setPlaying()
        {
            EpisodePlayState = String.IsNullOrEmpty(EpisodeFile) ? PodcastEpisodeModel.EpisodePlayStateEnum.Streaming
                                                                   : PodcastEpisodeModel.EpisodePlayStateEnum.Playing;
            episodeStartedPlaying();
            ProgressBarIsVisible = Visibility.Visible;
        }

        internal void setNoPlaying()
        {
            EpisodePlayState = String.IsNullOrEmpty(EpisodeFile) ? PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded
                                                                   : PodcastEpisodeModel.EpisodePlayStateEnum.Idle;
            ProgressBarIsVisible = Visibility.Collapsed;
        }
    }
}
