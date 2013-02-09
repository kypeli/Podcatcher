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

namespace Podcatcher
{
    public partial class App : Application
    {
        public const string PODCAST_ICON_DIR = "PodcastIcons";
        public const string PODCAST_DL_DIR   = "shared/transfers";

        /** IsolatedSettings keys.  **/
        // Key for storing the episode ID of the currently playing episode.
        public const string LSKEY_PODCAST_EPISODE_PLAYING_ID        = "playing_episodeId";
        // Key for storing the episode ID of the currently downloading episode.
        public const string LSKEY_PODCAST_EPISODE_DOWNLOADING_ID    = "dl_episodeId";
        // Key for verifying user knows special requirements for D/L videos.
        public const string LSKEY_PODCAST_VIDEO_DOWNLOAD_WIFI_ID    = "dl_videoEPisodesNeedWifi";
        // Key for storing the download queue information in.
        public const string LSKEY_PODCAST_DOWNLOAD_QUEUE            = "dl_queue";
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
        public const long MAX_SIZE_FOR_CELLULAR_DOWNLOAD            = 50000000;

        // Client ID for Live services. Currently we only use SkyDrive
        public const string LSKEY_LIVE_CLIENT_ID                    = "00000000400E9C91";

        // Name of our background task that checks for new episodes for pinned subscriptions
        public const string BGTASK_NEW_EPISODES                     = "SubscriptionsChecker";

        // Keys for storing episode location from AudioAgent.
        public const string LSKEY_AA_STORED_EPISODE_POSITION       = "aa_episode_position";
        public const string LSKEY_AA_EPISODE_PLAY_TITLE            = "aa_episode_title";

        
        private static LicenseInformation m_licenseInfo;
        private static bool m_isTrial = true;

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

        public static PodcastEpisodesDownloadManager episodeDownloadManager = PodcastEpisodesDownloadManager.getInstance();

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
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            //          IsolatedStorageExplorer.Explorer.Start("192.168.0.6");
            CheckLicense();
        }
        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
    //          IsolatedStorageExplorer.Explorer.RestoreFromTombstone();
            CheckLicense();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            PodcastSqlModel.getInstance().SubmitChanges();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            PodcastSqlModel.getInstance().SubmitChanges();
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
                Debug.WriteLine("Adding background service....");
                ScheduledActionService.LaunchForTest(BGTASK_NEW_EPISODES, TimeSpan.FromSeconds(10));
#endif
            }
            catch (InvalidOperationException e)
            {
                if (e.Message.Contains("BNS Error: The action is disabled"))
                {
                    App.showNotificationToast("Background tasks have been disabled from\nsystem settings.");
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