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



using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Marketplace;
using System.Diagnostics;
using Coding4Fun.Phone.Controls;
using System.Windows.Media;
using Microsoft.Phone.Scheduler;
using System;
using System.IO.IsolatedStorage;
using Podcatcher.ViewModels;
using Microsoft.Phone.BackgroundAudio;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Podcatcher
{
    public partial class App : Application
    {
        public const string PODCAST_ICON_DIR = "PodcastIcons";
        public const string PODCAST_DL_DIR   = "shared/transfers";

        /** IsolatedSettings keys.  **/
        // Key for storing the episode ID of the currently downloading episode.
        public const string LSKEY_PODCAST_EPISODE_DOWNLOADING_ID    = "dl_episodeId";
        // Key for verifying user knows special requirements for D/L videos.
        public const string LSKEY_PODCAST_VIDEO_DOWNLOAD_WIFI_ID    = "dl_videoEPisodesNeedWifi";
        // Key for storing the play history information in.
        public const string LSKEY_PODCAST_PLAY_HISTORY              = "play_history";
        // Key for determining how many restarts there's been since installing the app.
        public const string LSKEY_PODCATCHER_STARTS                 = "podcatcher_starts";
        public const int PODCATCHER_NEW_STARTS_BEFORE_SHOWING_REVIEW = 10;
        // Root key name for storing episode information for background service.
        public const string LSKEY_BG_SUBSCRIPTION_LATEST_EPISODE    = "bg_subscription_latest_episode";
        
        public const string LSKEY_NOTIFY_DOWNLOADING_WITH_CELLULAR  = "dl_withCellular";
        public const string LSKEY_NOTIFY_DOWNLOADING_WITH_WIFI      = "dl_withWifi";
        public const long MAX_SIZE_FOR_WIFI_DOWNLOAD_NO_POWER       = 104857600;
        public const long MAX_SIZE_FOR_CELLULAR_DOWNLOAD            = 20000000;

        // Client ID for Live services. Currently we only use SkyDrive
        public const string LSKEY_LIVE_CLIENT_ID                    = "00000000400E9C91";

        // Name of our background task that checks for new episodes for pinned subscriptions
        public const string BGTASK_NEW_EPISODES                     = "SubscriptionsChecker";

        private static LicenseInformation m_licenseInfo;
        private static bool m_isTrial = true;

        public static MainViewModels mainViewModels = new MainViewModels();

        public static PodcastPlaybackManager playbackManager = PodcastPlaybackManager.getInstance();

        public static bool IsTrial
        {
            get
            {
                return m_isTrial;
            }
        }

        // An enum to specify the theme.
        public enum AppTheme
        {
            Dark,
            Light
        }

        private static AppTheme m_currentTheme = AppTheme.Dark;
        internal static AppTheme CurrentTheme
        {
            get
            {
                return m_currentTheme;
            }
        }

        public static PodcastEpisodesDownloadManager episodeDownloadManager = null; 
        

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            
            m_licenseInfo = new LicenseInformation();

            detectCurrentTheme();
            refreshEpisodesFromAudioAgent();
        }

        public static long getPlayposFromAudioAgentForEpisode(PodcastEpisodeModel episode)
        {
            using (var playlistdb = new PlaylistDBContext())
            {
                List<PlaylistItem> playlistItems = playlistdb.Playlist.ToList();
                using (var db = new PodcastSqlModel())
                {
                    foreach (PlaylistItem i in playlistItems)
                    {
                        PodcastEpisodeModel e = db.Episodes.FirstOrDefault(ep => ep.EpisodeId == i.EpisodeId);
                        if (e == null || e.EpisodeId != episode.EpisodeId)
                        {
                            continue;
                        }

                        Debug.WriteLine("Found play pos for episode: " + episode.EpisodeId + ", pos: " + i.SavedPlayPosTick);
                        return i.SavedPlayPosTick;
                    }
                }
            }

            Debug.WriteLine("Warning: No play pos found for episode: " + episode.EpisodeId);
            return 0;
        }

        public static void refreshEpisodesFromAudioAgent()
        {
            Debug.WriteLine("Refreshing episode information that has been updated from AudioPlayer.");

            using (var playlistdb = new PlaylistDBContext())
            {
                List<PlaylistItem> playlistItems = playlistdb.Playlist.ToList();
                using (var db = new PodcastSqlModel())
                {
                    bool deleteListened = false;
                    if (playlistItems.Count > 0)
                    {
                        deleteListened = db.settings().IsAutoDelete;                        
                    }

                    foreach (PlaylistItem i in playlistItems)
                    {
                        PodcastEpisodeModel e = db.Episodes.FirstOrDefault(ep => ep.EpisodeId == i.EpisodeId);
                        if (e == null)
                        {
                            Debug.WriteLine("Warning: Could not fetch episode with ID: " + i.EpisodeId);
                            continue;
                        }

                        Debug.WriteLine("Updating episode '" + e.EpisodeName + "' playpos to: " + i.SavedPlayPosTick);
                        e.SavedPlayPos = i.SavedPlayPosTick;

                        // Update play state to listened as appropriate.
                        if (e.isListened())
                        {
                            e.markAsListened(deleteListened);
                            PodcastPlaybackManager.getInstance().addEpisodeToPlayHistory(e);
                        }

                        db.SubmitChanges();
                    }
                }
            }
        }

        internal static PodcastEpisodeModel refreshEpisodeFromAudioAgent(PodcastEpisodeModel episode)
        {
            List<PlaylistItem> playlistItems = null;
            using (var playlistdb = new PlaylistDBContext())
            {
                playlistItems = playlistdb.Playlist.ToList();
            }

            PodcastEpisodeModel e = null;
            using (var db = new PodcastSqlModel())
            {
                e = db.Episodes.FirstOrDefault(ep => ep.EpisodeId == episode.EpisodeId);

                bool deleteListened = false;
                if (playlistItems.Count > 0)
                {
                    deleteListened = db.settings().IsAutoDelete;
                }

                foreach (PlaylistItem i in playlistItems)
                {
                    if (i.EpisodeId != episode.EpisodeId)
                    {
                        continue;
                    }

                    Debug.WriteLine("Updating episode '" + e.EpisodeName + "' playpos to: " + i.SavedPlayPosTick);
                    e.SavedPlayPos = i.SavedPlayPosTick;

                    // Update play state to listened as appropriate.
                    if (e.isListened())
                    {
                        e.markAsListened(deleteListened);
                        PodcastPlaybackManager.getInstance().addEpisodeToPlayHistory(e);
                    }

                    db.SubmitChanges();
                }
            }

            return e;
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            //          IsolatedStorageExplorer.Explorer.Start("192.168.0.6");
            using (var db = new PodcastSqlModel())
            {
                db.createDB();
            }

            CheckLicense();
        }
        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
    //          IsolatedStorageExplorer.Explorer.RestoreFromTombstone();
            CheckLicense();
            mainViewModels.PlayQueue = new System.Collections.ObjectModel.ObservableCollection<PlaylistItem>();
            PodcastPlaybackManager.getInstance().updateCurrentlyPlayingEpisode();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void showErrorToast(string message) 
        {
                Debug.WriteLine("ERROR Toast: " + message);
                ToastPrompt toast = new ToastPrompt();
                toast.Title = "Error";
                toast.Message = message;
                toast.Show();        
        }

        public static void showNotificationToast(string message)
        {
            ToastPrompt toast = new ToastPrompt();
            toast.Message = message;
            toast.Show();
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            // RootFrame = new PhoneApplicationFrame();
            RootFrame = new Microsoft.Phone.Controls.TransitionFrame();

            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;

            try
            {
                PeriodicTask backgroundTask = null;
                backgroundTask = ScheduledActionService.Find(BGTASK_NEW_EPISODES) as PeriodicTask;
                if (backgroundTask != null)
                {
                    ScheduledActionService.Remove(backgroundTask.Name);
                }

                // Start our background agent.
                backgroundTask = new PeriodicTask(BGTASK_NEW_EPISODES);
                backgroundTask.Description = "Podcatcher's background task that checks if new episodes have arrived for pinned subscriptions";

                ScheduledActionService.Add(backgroundTask);
#if DEBUG
                // Disable lock screen.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

                // Start background agent updates.
                Debug.WriteLine("Adding background service....");
                ScheduledActionService.LaunchForTest(BGTASK_NEW_EPISODES, TimeSpan.FromSeconds(10));

#endif
            }
            catch (InvalidOperationException e)
            {
                if (e.Message.Contains("BNS Error: The action is disabled"))
                {
                    Debug.WriteLine("Background agent disabled.");
                }
            }
            catch (Exception) { /* In case we get some other scheduler related exception. But we are not interested. */ }
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        /// <summary>
        /// Check the current license information for this application
        /// </summary>
        private static void CheckLicense()
        {
            // When debugging, we want to simulate a trial mode experience. The following conditional allows us to set the _isTrial 
            // property to simulate trial mode being on or off. 
#if DEBUG
            string message = "Press 'OK' to simulate trial mode. Press 'Cancel' to run the application in normal mode.";
            if (MessageBox.Show(message, "Debug Trial",
                 MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                m_isTrial = true;
            }
            else
            {
                m_isTrial = false;
            }
#else
            m_isTrial = m_licenseInfo.IsTrial();
#endif
        }

        private void detectCurrentTheme()
        {
            Color lightThemeBackground = Color.FromArgb(255, 255, 255, 255);
            Color darkThemeBackground = Color.FromArgb(255, 0, 0, 0);
            SolidColorBrush backgroundBrush  = Application.Current.Resources["PhoneBackgroundBrush"] as SolidColorBrush;

            if (backgroundBrush.Color == lightThemeBackground)
                m_currentTheme = AppTheme.Light;
            else if (backgroundBrush.Color == darkThemeBackground)
                m_currentTheme = AppTheme.Dark;
            else
                Debug.WriteLine("Warning: Could not get current background theme color!");
        }

        #endregion
    }
}