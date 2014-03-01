using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Tasks;
using Podcatcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Podcatcher
{
    class PodcastPlayer
    {
        private static PodcastPlayer m_instance = null;
        public static PodcastPlayer getIntance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastPlayer();
                BackgroundAudioPlayer.Instance.PlayStateChanged += m_instance.PlayStateChanged;
            }

            return m_instance;
        }

        public void playEpisode(PodcastEpisodeModel episodeModel)
        {
            Debug.WriteLine("Starting playback for episode: " + episodeModel.EpisodeName);

            if (PodcastPlaybackManager.isAudioPodcast(episodeModel))
            {
                try
                {
                    audioPlayback(episodeModel);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("Error: File not found. " + e.Message);
                    App.showErrorToast("Cannot find episode.");
                }
            }
            else
            {
                videoPlayback(episodeModel);
            }
        }

        public void streamEpisode(PodcastEpisodeModel episodeModel)
        {
            startNewRemotePlayback(episodeModel);
        }

        public static double getEpisodePlayPosition()
        {
            TimeSpan position = TimeSpan.Zero;
            TimeSpan duration = TimeSpan.Zero;

            try
            {
                if (BackgroundAudioPlayer.Instance == null
                    || BackgroundAudioPlayer.Instance.Track == null
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
            catch (ArgumentException arge)
            {
                Debug.WriteLine("Catched argument error when trying to access BackgroundAudioPlayer. Error: " + arge.Message);
                return 0.0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Some other exception from player. Message: " + ex.Message);
                return 0.0;
            }

            if (duration.Ticks > 0)
            {
                return (double)((double)position.Ticks / (double)duration.Ticks);
            }

            return 0.0;
        }

        public TimeSpan getTrackPlayPosition()
        {
            TimeSpan position = TimeSpan.Zero;
            try
            {
                if (BackgroundAudioPlayer.Instance == null
                    || BackgroundAudioPlayer.Instance.Track == null
                    || BackgroundAudioPlayer.Instance.Position == null)
                {
                    position = TimeSpan.Zero;
                }

                position = BackgroundAudioPlayer.Instance.Position;
            }
            catch (InvalidOperationException ioe)
            {
                Debug.WriteLine("Error when updating player: " + ioe.Message);
            }
            catch (ArgumentException arge)
            {
                Debug.WriteLine("Catched argument error when trying to access BackgroundAudioPlayer. Error: " + arge.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Some other exception from player. Message: " + ex.Message);
            }

            return position;
        }

        public TimeSpan getTrackPlayDuration()
        {
            TimeSpan duration = TimeSpan.Zero;
            try
            {
                if (BackgroundAudioPlayer.Instance == null
                    || BackgroundAudioPlayer.Instance.Track == null
                    || BackgroundAudioPlayer.Instance.Position == null)
                {
                    duration = TimeSpan.Zero;
                }

                duration = BackgroundAudioPlayer.Instance.Track.Duration;
            }
            catch (InvalidOperationException ioe)
            {
                Debug.WriteLine("Error when updating player: " + ioe.Message);
            }
            catch (ArgumentException arge)
            {
                Debug.WriteLine("Catched argument error when trying to access BackgroundAudioPlayer. Error: " + arge.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Some other exception from player. Message: " + ex.Message);
            }

            return duration;
        }



        // ************ Private implementation ***************/
        private static PodcastEpisodeModel m_currentPlayerEpisode = null;


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
            if (m_currentPlayerEpisode != null
                && m_currentPlayerEpisode.EpisodeId == episodeModel.EpisodeId
                && BackgroundAudioPlayer.Instance.PlayerState == PlayState.Paused)
            {
                BackgroundAudioPlayer.Instance.Play();
            }
            else
            {
                startNewLocalPlayback(episodeModel);
            }
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
            m_currentPlayerEpisode = episodeModel;

            if (episodeModel.SavedPlayPos > 0)
            {
                bool alwaysContinuePlayback = false;
                using (var db = new PodcastSqlModel())
                {
                    alwaysContinuePlayback = db.settings().IsAutomaticContinuedPlayback;
                }

                if (alwaysContinuePlayback)
                {
                    startPlayback(episodeModel, new TimeSpan(episodeModel.SavedPlayPos), streaming);
                }
                else
                {
                    askForContinueEpisodePlaying(episodeModel, streaming);
                }
            }
            else
            {
                startPlayback(episodeModel, TimeSpan.Zero, streaming);
            }
        }

        private void startPlayback(PodcastEpisodeModel episode, TimeSpan position, bool streamEpisode = false)
        {
            AudioTrack playTrack = null;
            if (streamEpisode)
            {
                playTrack = getAudioStreamForEpisode(episode);
            }
            else
            {
                playTrack = getAudioTrackForEpisode(episode);
            }

            if (playTrack == null)
            {
                App.showErrorToast("Cannot play the episode.");
                return;
            }


            try
            {
                BackgroundAudioPlayer.Instance.Track = playTrack;
                BackgroundAudioPlayer.Instance.Volume = 1.0;

                // This should really be on the other side of BackgroundAudioPlayer.Instance.Position
                // then for some reason it's not honored. 
                BackgroundAudioPlayer.Instance.Play();

                if (position.Ticks > 0)
                {
                    BackgroundAudioPlayer.Instance.Position = new TimeSpan(position.Ticks);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("I've read from Microsoft that something stupid can happen if you try to start " +
                                "playing and there's a YouTube video playing. This this try-catch is really just " +
                                "to guard against Microsoft's bug.");
            }
        }

        private void askForContinueEpisodePlaying(PodcastEpisodeModel episode, bool streaming)
        {
            MessageBoxButton messageButtons = MessageBoxButton.OKCancel;
            MessageBoxResult messageBoxResult = MessageBox.Show("You have previously played this episode. Do you wish to continue from the previous position?",
                                                                "Continue?",
                                                                messageButtons);
            if (messageBoxResult == MessageBoxResult.OK)
            {
                startPlayback(episode, new TimeSpan(episode.SavedPlayPos), streaming);
            }
            else
            {
                startPlayback(episode, new TimeSpan(0), streaming);
            }
        }

        private AudioTrack getAudioTrackForEpisode(PodcastEpisodeModel currentEpisode)
        {
            if (currentEpisode == null ||
                String.IsNullOrEmpty(currentEpisode.EpisodeFile))
            {
                return null;
            }

            Uri episodeLocation;
            try
            {
                episodeLocation = new Uri(currentEpisode.EpisodeFile, UriKind.Relative);
            }
            catch (Exception)
            {
                return null;
            }

            using (var db = new PodcastSqlModel())
            {
                PodcastSubscriptionModel sub = db.Subscriptions.First(s => s.PodcastId == currentEpisode.PodcastId);
                return new AudioTrack(episodeLocation,
                            currentEpisode.EpisodeName,
                            sub.PodcastName,
                            "",
                            new Uri(sub.PodcastLogoLocalLocation, UriKind.Relative));
            }
        }

        private AudioTrack getAudioStreamForEpisode(PodcastEpisodeModel episode)
        {
            if (episode == null ||
                String.IsNullOrEmpty(episode.EpisodeDownloadUri))
            {
                return null;
            }

            Uri episodeLocation;
            try
            {
                episodeLocation = new Uri(episode.EpisodeDownloadUri, UriKind.Absolute);
            }
            catch (Exception)
            {
                return null;
            }

            using (var db = new PodcastSqlModel())
            {
                PodcastSubscriptionModel sub = db.Subscriptions.First(s => s.PodcastId == episode.PodcastId);

                return new AudioTrack(episodeLocation,
                            episode.EpisodeName,
                            sub.PodcastName,
                            "",
                            new Uri(sub.PodcastLogoLocalLocation, UriKind.Relative));
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
                    break;

                case PlayState.Paused:
                    // Player is on pause
                    Debug.WriteLine("Podcast player is paused.");
                    break;

                case PlayState.Shutdown:
                case PlayState.Unknown:
                case PlayState.Stopped:
                    // Player stopped
                    Debug.WriteLine("Podcast player stopped.");
                    m_currentPlayerEpisode = null;
                    break;

                case PlayState.TrackEnded:
                    break;
            }
        }
    }
}
