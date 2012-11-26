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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using Podcatcher.ViewModels;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

namespace Podcatcher
{
    public partial class PodcastEpisodeControl : UserControl
    {
        /************************************* Private implementations *******************************/
        public PodcastEpisodeControl()
        {
            // Required to initialize variables
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Page_Loaded);
        }

        /************************************* Private implementations *******************************/
        #region private
        private PodcastEpisodeModel m_episodeModel;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
            if (String.IsNullOrEmpty(m_episodeModel.EpisodeRunningTime) == false)
            {
                this.EpisodeRunningTime.Text = String.Format("Duration: {0}", m_episodeModel.EpisodeRunningTime);
            }

            if ((m_episodeModel.EpisodeState == PodcastEpisodeModel.EpisodeStateEnum.Paused 
                 || m_episodeModel.EpisodeState == PodcastEpisodeModel.EpisodeStateEnum.Playable)                 
                && m_episodeModel.SavedPlayPos > 0 
                && m_episodeModel.TotalLengthTicks > 0)
            {
                PlayProgressBar.Visibility = System.Windows.Visibility.Visible;
                PlayProgressBar.Value = (((double)m_episodeModel.SavedPlayPos / (double)m_episodeModel.TotalLengthTicks) * (double)100);
            }
            else
            {
                PlayProgressBar.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void EpisodeButton_Click(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;

            switch (m_episodeModel.EpisodeState)
            {
                // Episode is idle => start downloading. 
                case PodcastEpisodeModel.EpisodeStateEnum.Idle:
                    PodcastEpisodesDownloadManager downloadManager = PodcastEpisodesDownloadManager.getInstance();

                    bool continueDl = true;
                    if (PodcastPlayerControl.isAudioPodcast(m_episodeModel) == false)
                    {
                        continueDl = continueVideoDownload();                        
                    }

                    if (continueDl)
                    {
                        downloadManager.addEpisodeToDownloadQueue(m_episodeModel);
                    }
                    break;

                // Episode is playable -> Play episode.
                case PodcastEpisodeModel.EpisodeStateEnum.Playable:
                    PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                    player.playEpisode(m_episodeModel);
                    break;
            }
        }

        private bool continueVideoDownload()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (appSettings.Contains(App.LSKEY_PODCAST_VIDEO_DOWNLOAD_WIFI_ID) == false)
            {
                if (MessageBox.Show("Video podcasts can only be downloaded when the device is connected to a WiFi network and to a power source.",
                    "Attention",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    appSettings.Add(App.LSKEY_PODCAST_VIDEO_DOWNLOAD_WIFI_ID, true);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private void MenuItemStream_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = (sender as MenuItem).DataContext as PodcastEpisodeModel;
            if (PodcastPlayerControl.isAudioPodcast(podcastEpisode)) 
            {
                audioStreaming(podcastEpisode);
            }
            else
            {
                PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                player.StopPlayback();
                videoStreaming(podcastEpisode);
            }
        }

        private void videoStreaming(PodcastEpisodeModel podcastEpisode)
        {
            MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
            mediaPlayerLauncher.Media = new Uri(podcastEpisode.EpisodeDownloadUri, UriKind.Absolute);
            mediaPlayerLauncher.Controls = MediaPlaybackControls.All;
            mediaPlayerLauncher.Location = MediaLocationType.Data;
            mediaPlayerLauncher.Show();
        }

        private void audioStreaming(PodcastEpisodeModel podcastEpisode)
        {
            PodcastPlayerControl player = PodcastPlayerControl.getIntance();
            player.streamEpisode(podcastEpisode);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            PodcastEpisodeModel podcastEpisode = (sender as MenuItem).DataContext as PodcastEpisodeModel;
            podcastEpisode.deleteDownloadedEpisode();
            PlayProgressBar.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion
    }
}