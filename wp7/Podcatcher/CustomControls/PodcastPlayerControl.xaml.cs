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
using System.Windows;
using System.Windows.Controls;
using Podcatcher.ViewModels;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Phone.BackgroundAudio;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using Microsoft.Phone.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;


namespace Podcatcher
{
    public partial class PodcastPlayerControl : UserControl
    {
        public event EventHandler PodcastPlayerStarted;
        public event EventHandler PodcastPlayerStopped;

        public PodcastEpisodeModel PlayingEpisode
        {
            get
            {
                return m_currentEpisode;
            }

            private set
            {

            }
        }

        public PodcastPlayerControl()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                return;
            }

            m_appSettings = IsolatedStorageSettings.ApplicationSettings;

            setupPlayerUI();
            if (m_instance == null) 
            {
                initializePlayerUI();
                checkPlayerEpisodeState();
                m_instance = this;
            }
        }

        public void initializePlayerUI()
        {
            if (BackgroundAudioPlayer.Instance.Track != null)
            {
                Debug.WriteLine("Restoring UI for currently playing episode.");
                showPlayerLayout();
                restoreEpisodeToPlayerUI();
            }
            else
            {
                showNoPlayerLayout();
            }
        }

        public static PodcastPlayerControl getIntance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastPlayerControl();
            }

            return m_instance;
        }

        public static bool isAudioPodcast(PodcastEpisodeModel episode)
        {
            bool audio = false;

             // We have to treat empty as an audio, because that was what the previous 
             // version had for mime type and we don't want to break the functionality.
            if (String.IsNullOrEmpty(episode.EpisodeFileMimeType))
            {
                return true;
            }

            switch (episode.EpisodeFileMimeType)
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
                case "audio/x-mpeg":
                case "media/mpeg":          // This is for The Money Pit who has wrong MIME type. http://www.moneypit.com/show/moneypit_podcast.xml
                    audio = true;
                    break;
            }

            return audio;
        }

        internal void playEpisode(PodcastEpisodeModel episodeModel)
        {
            Debug.WriteLine("Starting playback for episode: " + episodeModel.EpisodeName);

            if (isAudioPodcast(episodeModel))
            {
                try
                {
                    audioPlayback(episodeModel);
                    setupUIForEpisodePlaying();
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("Error: File not found. " + e.Message);
                    App.showErrorToast("Cannot find episode.");
                    showNoPlayerLayout();
                }
            }
            else
            {
                StopPlayback();
                videoPlayback(episodeModel);
            }
        }

        private void videoPlayback(PodcastEpisodeModel episodeModel)
        {
            MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
            mediaPlayerLauncher.Media = new Uri(episodeModel.EpisodeFile, UriKind.Relative);
            mediaPlayerLauncher.Controls = MediaPlaybackControls.All;
            mediaPlayerLauncher.Location = MediaLocationType.Data;
            mediaPlayerLauncher.Show();
        }

        private void audioPlayback(PodcastEpisodeModel episodeModel)
        {
            if (m_currentEpisode != null
                && m_currentEpisode == episodeModel
                && BackgroundAudioPlayer.Instance.PlayerState == PlayState.Paused)
            {
                BackgroundAudioPlayer.Instance.Play();
                setupUIForEpisodePlaying();
                PodcastPlayerStarted(this, new EventArgs());
            }
            else
            {
                startNewLocalPlayback(episodeModel);
            }
        }

        public void streamEpisode(PodcastEpisodeModel episodeModel)
        {
            startNewRemotePlayback(episodeModel);
        }

        public void StopPlayback()
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing
                || BackgroundAudioPlayer.Instance.PlayerState == PlayState.Paused)
            {

                showNoPlayerLayout();
                if (PodcastPlayerStopped != null)
                {
                    PodcastPlayerStopped(this, new EventArgs());
                }

                BackgroundAudioPlayer.Instance.Stop();
            }
        }

        /************************************* Private implementation *******************************/

        private static PodcastPlayerControl m_instance = null;
        private static PodcastEpisodeModel m_currentEpisode = null;
        private static bool settingSliderFromPlay;
        private IsolatedStorageSettings m_appSettings;
        private static DispatcherTimer m_screenUpdateTimer = null;

        // Loading the control images based on the theme color
        private BitmapImage m_playButtonBitmap;
        private BitmapImage m_pauseButtonBitmap;
        private BitmapImage m_stopButtonBitmap;
        private BitmapImage m_nextButtonBitmap;
        private BitmapImage m_prevButtonBitmap;

        private void setupPlayerUI()
        {
            Microsoft.Xna.Framework.Media.MediaLibrary library = new Microsoft.Xna.Framework.Media.MediaLibrary();

            m_playButtonBitmap = new BitmapImage(new Uri("/Images/" + App.CurrentTheme + "/play.png", UriKind.Relative));
            m_pauseButtonBitmap = new BitmapImage(new Uri("/Images/" + App.CurrentTheme + "/pause.png", UriKind.Relative));
            this.PlayButtonImage.Source = m_playButtonBitmap;

            m_stopButtonBitmap = new BitmapImage(new Uri("/Images/" + App.CurrentTheme + "/stop.png", UriKind.Relative));
            this.StopButtonImage.Source = m_stopButtonBitmap;

            m_nextButtonBitmap = new BitmapImage(new Uri("/Images/" + App.CurrentTheme + "/ff.png", UriKind.Relative));
            this.NextButtonImage.Source = m_nextButtonBitmap;

            m_prevButtonBitmap = new BitmapImage(new Uri("/Images/" + App.CurrentTheme + "/rew.png", UriKind.Relative));
            this.PrevButtonImage.Source = m_prevButtonBitmap;
        }

        private void restoreEpisodeToPlayerUI()
        {
            // If we have an episodeId stored in local cache, this means we returned to the app and 
            // have that episode playing. Hence, here we need to reload the episode data from the SQL. 
            if (m_appSettings.Contains(App.LSKEY_PODCAST_EPISODE_PLAYING_ID))
            {
                int episodeId = (int)m_appSettings[App.LSKEY_PODCAST_EPISODE_PLAYING_ID];
                App.currentlyPlayingEpisodeId = episodeId;

                if (App.currentlyPlayingEpisode != null)
                {
                    m_currentEpisode = App.currentlyPlayingEpisode;
                }
                else
                {
                    using (var db = new PodcastSqlModel())
                    {
                        m_currentEpisode = db.episodeForEpisodeId(episodeId);
                        App.currentlyPlayingEpisode = m_currentEpisode;
                        App.currentlyPlayingEpisode.setPlaying();
                    }
                }

                if (m_currentEpisode == null)
                {
                    // Episode not in SQL anymore (maybe it was deleted). So clear up a bit...
                    m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
                    m_appSettings.Save();
                    showNoPlayerLayout();
                    App.currentlyPlayingEpisodeId = -1;
                    App.currentlyPlayingEpisode = null;
                    return;
                }

                BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(PlayStateChanged);
                setupPlayerUIContent(m_currentEpisode);

                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                {
                    setupUIForEpisodePlaying();
                }
                else
                {
                    setupUIForEpisodePaused();
                }
            }
            else 
            {
                showNoPlayerLayout();
                Debug.WriteLine("WARNING: Could not find episode ID from app settings, so cannot restore player UI with episode information!");
            }
        }

        public void checkPlayerEpisodeState()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (appSettings.Contains(App.LSKEY_PODCAST_EPISODE_PLAYING_ID))
            {
                int episodeId = (int)appSettings[App.LSKEY_PODCAST_EPISODE_PLAYING_ID];

                if (BackgroundAudioPlayer.Instance.Track == null)
                {
                    appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
                    appSettings.Save();
                    App.currentlyPlayingEpisodeId = -1;
                    App.currentlyPlayingEpisode = null;

                    using (var db = new PodcastSqlModel())
                    {
                        PodcastEpisodeModel episode = db.episodeForEpisodeId(episodeId);
                        if (episode != null)
                        {
                            db.addEpisodeToPlayHistory(episode);
                        }
                    }
                }
            }
        }

        private void setupPlayerUIContent(PodcastEpisodeModel currentEpisode)
        {
            using (var db = new PodcastSqlModel())
            {
                PodcastEpisodeModel ep = db.Episodes.First(e => e.EpisodeId == currentEpisode.EpisodeId);
                this.PodcastLogo.Source = ep.PodcastSubscription.PodcastLogo;
                this.PodcastEpisodeName.Text = ep.EpisodeName;
            }
        }

        public void showNoPlayerLayout()
        {
            if (m_screenUpdateTimer != null)
            {
                m_screenUpdateTimer.Stop();
                m_screenUpdateTimer.Tick -= new EventHandler(m_screenUpdateTimer_Tick);
                m_screenUpdateTimer = null;
            }

            this.NoPlayingLayout.Visibility = Visibility.Visible;
            this.PlayingLayout.Visibility = Visibility.Collapsed;
        }

        private void showPlayerLayout()
        {
            if (m_screenUpdateTimer == null)
            {
                m_screenUpdateTimer = new DispatcherTimer();
            }

            m_screenUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500); // Fire the timer every half a second.
            m_screenUpdateTimer.Tick += new EventHandler(m_screenUpdateTimer_Tick);
            m_screenUpdateTimer.Start();

            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
        }

        private void startNewLocalPlayback(PodcastEpisodeModel episodeModel)
        {
            startNewPlayback(episodeModel, false);
        }

        private void startNewRemotePlayback(PodcastEpisodeModel episodeModel)
        {
            startNewPlayback(episodeModel, true);
        }

        private void startNewPlayback(PodcastEpisodeModel episodeModel, bool streaming)
        {
            // Save the state for the previously playing podcast episode. 
            if (m_currentEpisode != null)
            {
                saveEpisodePlayPosition(m_currentEpisode);
                addEpisodeToPlayHistory(m_currentEpisode);
            }

            m_currentEpisode = episodeModel;
            App.currentlyPlayingEpisode = episodeModel;

            setupPlayerUIContent(m_currentEpisode);
            showPlayerLayout();
            updatePrimary(m_currentEpisode);

            if (!streaming && episodeModel.SavedPlayPos > 0)
            {
                bool alwaysContinuePlayback = false;
                using (var db = new PodcastSqlModel())
                {
                    alwaysContinuePlayback = db.settings().IsAutomaticContinuedPlayback;
                }

                if (alwaysContinuePlayback)
                {
                    startPlayback(new TimeSpan(m_currentEpisode.SavedPlayPos), streaming);
                }
                else
                {
                    askForContinueEpisodePlaying(streaming);
                }
            }
            else
            {
                startPlayback(TimeSpan.Zero, streaming);
            }
        }

        private void startPlayback(TimeSpan position, bool streamEpisode = false)
        {
            AudioTrack playTrack = null;
            if (streamEpisode)
            {
                playTrack = getAudioStreamForEpisode(m_currentEpisode);
            }
            else
            {
                playTrack = getAudioTrackForEpisode(m_currentEpisode);
            }

            if (playTrack == null)
            {
                App.showErrorToast("Cannot play the episode.");
                return;
            }

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(PlayStateChanged);
            BackgroundAudioPlayer.Instance.Track = playTrack;
            BackgroundAudioPlayer.Instance.Volume = 1.0;

            try
            {
                this.PlayButtonImage.Source = m_pauseButtonBitmap;
                m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
                m_appSettings.Add(App.LSKEY_PODCAST_EPISODE_PLAYING_ID, m_currentEpisode.EpisodeId);
                m_appSettings.Save();

//                App.currentlyPlayingEpisodeId = m_currentEpisode.EpisodeId;
//                App.currentlyPlayingEpisode = m_currentEpisode;

                // This should really be on the other side of BackgroundAudioPlayer.Instance.Position
                // then for some reason it's not honored. 
                BackgroundAudioPlayer.Instance.Play();

                if (position.Ticks > 0)
                {
                    BackgroundAudioPlayer.Instance.Position = new TimeSpan(position.Ticks);
                }

                PodcastPlayerStarted(this, new EventArgs());
            }
            catch (Exception)
            {
                Debug.WriteLine("I've read from Microsoft that something stupid can happen if you try to start " +
                                "playing and there's a YouTube video playing. This this try-catch is really just " +
                                "to guard against Microsoft's bug.");
            }
        }

        private void saveEpisodePlayPosition(PodcastEpisodeModel m_currentEpisode)
        {
            try
            {
                m_currentEpisode.SavedPlayPos = BackgroundAudioPlayer.Instance.Position.Ticks;
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("BackgroundAudioPlayer returned NULL. Player didn't probably have a track that it was playing.");
            }
            catch (SystemException)
            {
                Debug.WriteLine("Got system exception when trying to save position.");
            }

            updateEpisodeToDB(m_currentEpisode);
        }

        private void askForContinueEpisodePlaying(bool streaming)
        {
            MessageBoxButton messageButtons = MessageBoxButton.OKCancel;
            MessageBoxResult messageBoxResult = MessageBox.Show("You have previously played this episode. Do you wish to continue from the previous position?",
                                                                "Continue?",
                                                                messageButtons);
            if (messageBoxResult == MessageBoxResult.OK)
            {
                startPlayback(new TimeSpan(m_currentEpisode.SavedPlayPos), streaming);
            }
            else
            {
                startPlayback(new TimeSpan(0), streaming);
            }
        }

        void PlayStateChanged(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.Error != null)
            {
                Debug.WriteLine("PlayStateChanged: Podcast player is no longer available.");
                return;
            }

            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    // Player is playing
                    Debug.WriteLine("Podcast player is playing...");
                    setupUIForEpisodePlaying();
                    break;

                case PlayState.Paused:
                    // Player is on pause
                    Debug.WriteLine("Podcast player is paused...");
                    saveEpisodePlayPosition(m_currentEpisode);
                    setupUIForEpisodePaused();
                    break;

                case PlayState.Stopped:
                    // Player stopped
                    playbackStopped();
                    Debug.WriteLine("Podcast player stopped.");
                    break;

                case PlayState.Shutdown:
                    playbackStopped();
                    Debug.WriteLine("Podcast player shut down.");
                    break;
            }
        }

        private void updatePrimary(PodcastEpisodeModel currentEpisode)
        {
            ShellTile PrimaryTile = ShellTile.ActiveTiles.First();

            if (PrimaryTile != null)
            {
                StandardTileData tile = new StandardTileData();
                String tileImageLocation = "";
                String podcastLogoLocalLocation = "";

                // Copy the logo file to tile's shared location.               
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var db = new PodcastSqlModel())
                    {
                        PodcastSubscriptionModel sub = db.Subscriptions.First(s => s.PodcastId == currentEpisode.PodcastId);
                        podcastLogoLocalLocation = sub.PodcastLogoLocalLocation;
                        tile.BackTitle = sub.PodcastName;
                    }

                    if (myIsolatedStorage.FileExists(podcastLogoLocalLocation) == false)
                    {
                        // Cover art does not exist, we cannot do anything. Give up, don't put it to Live tile.
                        Debug.WriteLine("Podcasts cover art not found.");
                        return;
                    }

                    tileImageLocation = "Shared/ShellContent/" + podcastLogoLocalLocation.Split('/')[1];

                    if (myIsolatedStorage.FileExists(tileImageLocation) == false)
                    {
                        myIsolatedStorage.CopyFile(podcastLogoLocalLocation,
                                                   tileImageLocation);
                    }
                }

                tile.BackBackgroundImage = new Uri("isostore:/" + tileImageLocation, UriKind.Absolute); 
                PrimaryTile.Update(tile);
            } 
        }

        private void playbackStopped()
        {
            saveEpisodePlayPosition(m_currentEpisode);
            addEpisodeToPlayHistory(m_currentEpisode);
            updateEpisodeToDB(m_currentEpisode);

            m_currentEpisode = null;
            BackgroundAudioPlayer.Instance.Track = null;

            PhoneApplicationFrame rootFrame = Application.Current.RootVisual as PhoneApplicationFrame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void setupUIForEpisodePaused()
        {
            m_currentEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Paused;
            m_screenUpdateTimer.Stop();                                                                                         
            this.PlayButtonImage.Source = m_playButtonBitmap;
        }

        private void setupUIForEpisodePlaying()
        {
            if (BackgroundAudioPlayer.Instance.Track == null)
            {
                Debug.WriteLine("Error: Cannot setup player UI when BackgroundAudioPlayer.Instance.Track == null");
                showNoPlayerLayout();
                return;
            }

            if (BackgroundAudioPlayer.Instance.Track.Source.ToString().IndexOf("http") > -1)
            {
                m_currentEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
            }
            else
            {
                m_currentEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Playing;
            }

            if (m_screenUpdateTimer != null && !m_screenUpdateTimer.IsEnabled)
            {
                m_screenUpdateTimer.Start();
            }

            this.PlayButtonImage.Source = m_pauseButtonBitmap;
        }

        private void rewButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.SkipPrevious();
            }
        }

        private void playButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.Pause();
                setupUIForEpisodePaused();
            }
            else
            {
                BackgroundAudioPlayer.Instance.Play();
                setupUIForEpisodePlaying();
            }
        }

        private void stopButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Stopped)
            {
                saveEpisodePlayPosition(m_currentEpisode);
                // We are already stopped (playback ended or something). Let's update the episode state.
                m_currentEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Downloaded;
                addEpisodeToPlayHistory(m_currentEpisode);
            }
            else
            {
                StopPlayback();
            }

            lock (m_appSettings)
            {
                if (m_appSettings.Contains(App.LSKEY_PODCAST_EPISODE_PLAYING_ID)) 
                {
                    m_appSettings.Remove(App.LSKEY_PODCAST_EPISODE_PLAYING_ID);
                    m_appSettings.Save();
                }
            }

            App.currentlyPlayingEpisodeId = -1;
            App.currentlyPlayingEpisode = null;

            showNoPlayerLayout();
        }

        private void ffButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.SkipNext();
            }
        }

        private AudioTrack getAudioTrackForEpisode(PodcastEpisodeModel m_currentEpisode)
        {
            if (m_currentEpisode == null ||
                String.IsNullOrEmpty(m_currentEpisode.EpisodeFile))
            {
                return null;
            }

            Uri episodeLocation;
            try 
            {
                episodeLocation = new Uri(m_currentEpisode.EpisodeFile, UriKind.Relative);
            } catch(Exception) {
                return null;
            }

            using (var db = new PodcastSqlModel())
            {
                PodcastSubscriptionModel sub = db.Subscriptions.First(s => s.PodcastId == m_currentEpisode.PodcastId);
                return new AudioTrack(episodeLocation,
                            m_currentEpisode.EpisodeName,
                            sub.PodcastName,
                            "",
                            new Uri(sub.PodcastLogoLocalLocation, UriKind.Relative));
            }
        }

        private AudioTrack getAudioStreamForEpisode(PodcastEpisodeModel episode)
        {
            if (m_currentEpisode == null ||
                String.IsNullOrEmpty(m_currentEpisode.EpisodeDownloadUri))
            {
                return null;
            }

            Uri episodeLocation;
            try
            {
                episodeLocation = new Uri(m_currentEpisode.EpisodeDownloadUri, UriKind.Absolute);
            }
            catch (Exception)
            {
                return null;
            }

            using (var db = new PodcastSqlModel())
            {
                PodcastSubscriptionModel sub = db.Subscriptions.First(s => s.PodcastId == m_currentEpisode.PodcastId);

                return new AudioTrack(episodeLocation,
                            m_currentEpisode.EpisodeName,
                            sub.PodcastName,
                            "",
                            new Uri(sub.PodcastLogoLocalLocation, UriKind.Relative));
            }
        }

        void m_screenUpdateTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Tick.");

            if (m_currentEpisode == null)
            {
                Debug.WriteLine("Warning: Current episode playing is NULL!");
                return;
            }

            this.PositionSlider.Value = 0;
            TimeSpan position = TimeSpan.Zero;

            settingSliderFromPlay = true;

            try
            {
                if (BackgroundAudioPlayer.Instance.Track == null
                    || BackgroundAudioPlayer.Instance.Position == null)
                {
                    return;
                }

                position = BackgroundAudioPlayer.Instance.Position;

                this.CurrentPositionText.Text = position.ToString("hh\\:mm\\:ss");
                this.TotalDurationText.Text = BackgroundAudioPlayer.Instance.Track.Duration.ToString("hh\\:mm\\:ss");
                this.PositionSlider.Value = getEpisodePlayPosition();
            }
            catch (InvalidOperationException ioe)
            {
                Debug.WriteLine("Error when updating player: " + ioe.Message);
                return;
            }
            catch (SystemException syse)
            {
                Debug.WriteLine("Error when updating player: " + syse.Message);
                App.showErrorToast("WP8 cannot play from this location.");
                StopPlayback();
                return;
            }

            settingSliderFromPlay = false;
        }

        public static double getEpisodePlayPosition() 
        {
            TimeSpan position = TimeSpan.Zero;
            TimeSpan duration = TimeSpan.Zero;

            try
            {
                if (BackgroundAudioPlayer.Instance.Track == null
                    || BackgroundAudioPlayer.Instance.Position == null)
                {
                    return 0.0;
                }

                duration = BackgroundAudioPlayer.Instance.Track.Duration;
                position = BackgroundAudioPlayer.Instance.Position;
            }
            catch (InvalidOperationException ioe)
            {
                Debug.WriteLine("Error when updating player: " + ioe.Message);
                return 0.0;
            }

            if (duration.Ticks > 0)
            {
                return (double)((double)position.Ticks / (double)duration.Ticks);
            }
            
            return 0.0;
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!settingSliderFromPlay)
            {
                if (e.NewValue <= 0.1)
                {
                    return;
                }

                AudioTrack audioTrack = null;
                TimeSpan duration = TimeSpan.Zero;

                try
                {
                    // Sometimes these property accesses will raise exceptions
                    audioTrack = BackgroundAudioPlayer.Instance.Track;

                    if (audioTrack != null)
                        duration = audioTrack.Duration;
                }
                catch
                {
                }

                if (audioTrack == null)
                    return;

                long ticks = (long)(e.NewValue * duration.Ticks);
                BackgroundAudioPlayer.Instance.Position = new TimeSpan(ticks);
            }
        }

        private void addEpisodeToPlayHistory(PodcastEpisodeModel episode)
        {
            using (var db = new PodcastSqlModel())
            {
                db.addEpisodeToPlayHistory(episode);
            }
        }

        private void updateEpisodeToDB(PodcastEpisodeModel episode)
        {
            PodcastSubscriptionModel sub = null;
            using (var db = new PodcastSqlModel())
            {
                PodcastEpisodeModel e = db.Episodes.Single(ep => ep.EpisodeId == episode.EpisodeId);  // db.episodeForEpisodeId(episode.EpisodeId);
                sub = e.PodcastSubscription;
                
                e.SavedPlayPos = episode.SavedPlayPos;
                // e.TotalLengthTicks = episode.TotalLengthTicks;

                db.SubmitChanges();
            }

            PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(sub);
        }


    }
}
