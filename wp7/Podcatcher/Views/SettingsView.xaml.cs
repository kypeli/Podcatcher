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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace Podcatcher.Views
{
    public partial class SettingsView : PhoneApplicationPage
    {
        private static bool initialized = false;
        private SettingsModel m_settings = null;
        private String m_podcastUsage = null;
        private List<String> m_fileList = null;

        public SettingsView()
        {
            InitializeComponent();
            initialized = true;
            m_settings = new PodcastSqlModel().settings();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            this.DataContext = m_settings;
            this.DeleteEpisodeThreshold.Value = m_settings.ListenedThreashold;
            this.DeleteThresholdPercent.Text = String.Format("{0} %", this.DeleteEpisodeThreshold.Value.ToString());
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            using (var db = new PodcastSqlModel()) 
            {
                SettingsModel s = db.settings();

                s.IsAutomaticContinuedPlayback = m_settings.IsAutomaticContinuedPlayback;
                s.IsAutoDelete = m_settings.IsAutoDelete;
                s.IsUseCellularData = m_settings.IsUseCellularData;
                s.SelectedExportIndex = m_settings.SelectedExportIndex;
                s.ListenedThreashold = m_settings.ListenedThreashold;

                db.SubmitChanges();
            }
        }
        
        private void DeleteEpisodeThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!initialized
                || this.DeleteEpisodeThreshold == null)
            {
                return;
            }

            this.DeleteThresholdPercent.Text = String.Format("{0} %", ((int)e.NewValue).ToString());
            m_settings.ListenedThreashold = (int)e.NewValue;
        }

        private void NavigationPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Find out when we are in the Usage pivot.
            if (this.NavigationPivot.SelectedIndex == 3)
            {
                if (m_podcastUsage == null)
                {
                    m_podcastUsage = getUsageString();
                }

                this.UsageText.Text = m_podcastUsage;
            }
        }

        private String getUsageString()
        {
            long usedBytes = 0;
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                m_fileList = storage.GetFileNames(App.PODCAST_DL_DIR + "/*").ToList<String>();
                IsolatedStorageFileStream fileStream = null;
                foreach (String filename in m_fileList)
                {
                    try
                    {
                        fileStream = storage.OpenFile(App.PODCAST_DL_DIR + "/" + filename, System.IO.FileMode.Open);
                        Debug.WriteLine("File: {0}, Size: {1}", filename, fileStream.Length);
                        usedBytes += fileStream.Length;
                        fileStream.Close();
                    }
                    catch (IsolatedStorageException)
                    {
                        App.showNotificationToast("Notice: Could not read all files.");
                    }
                }
            }

            String units = "GB";
            // Check if we are over a gigabyte
            if ((usedBytes >> 30) != 0)
            {
                usedBytes >>= 30;
            }
            else
            {
                // Then convert to megabytes
                units = "MB";
                usedBytes >>= 20;
            }

            return String.Format("{0} {1}", usedBytes, units);
        }

        private void DeleteAllButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete all downloaded podcasts?",
                    "Delete all?",
                    MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                return;
            }

            List<PodcastEpisodeModel> episodes = null;
            using (var db = new PodcastSqlModel())
            {
                episodes = db.allEpisodes();
            }

            foreach (PodcastEpisodeModel episode in episodes)
            {
                episode.deleteDownloadedEpisode();
            }

            this.UsageText.Text = getUsageString();
        }
    }
}