using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Podcatcher.ViewModels;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.BackgroundAudio;
using System.Linq;
using System.Diagnostics;


namespace Podcatcher
{
    public class PodcastPlaybackManager
    {
        private static PodcastPlaybackManager m_instance;

        private PodcastPlaybackManager()
        {
            initializeCurrentlyPlayingEpisode();
            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(PlayStateChanged);
        }

        public static PodcastPlaybackManager getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new PodcastPlaybackManager();
            }

            return m_instance;
        }

        public void play(PodcastEpisodeModel episode)
        {
            if (episode == null)
            {
                Debug.WriteLine("Warning: Trying to play a NULL episode.");
                return;
            }

            App.CurrentlyPlayingEpisode = episode;

            // Play locally from a downloaded file.
            if (episode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
            {
                PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                player.playEpisode(episode);
                episode.setPlaying();
            } else {
                // Stream it if not downloaded. 
                if (PodcastPlayerControl.isAudioPodcast(episode))
                {
                    episode.setPlaying();
                    audioStreaming(episode);
                }
                else
                {
                    PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                    player.StopPlayback();
                    videoStreaming(episode);
                }
            }
        }

        public PodcastEpisodeModel currentlyPlayingEpisode()
        {
            PlaylistItem plItem = null;
            using (var playlistDb = new PlaylistDBContext())
            {
                if (playlistDb.Playlist.Count() == 0)
                {
                    return null;
                }

                plItem = playlistDb.Playlist.Where(item => item.IsCurrent).FirstOrDefault();
            }

            if (plItem != null)
            {
                using (var db = new PodcastSqlModel())
                {
                    return db.Episodes.Where(ep => ep.EpisodeId == plItem.EpisodeId).FirstOrDefault();
                }
            }

            return null;
        }

        /****************************** Private implementations *******************************/

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

        private void addEpisodeToPlayHistory(PodcastEpisodeModel episode)
        {
            using (var db = new PodcastSqlModel())
            {
                db.addEpisodeToPlayHistory(episode);
            }
        }

        private void clearPlayList()
        {
            using (var db = new PlaylistDBContext())
            {
                db.Playlist.DeleteAllOnSubmit(db.Playlist);
                db.SubmitChanges();
            }
        }

        private void initializeCurrentlyPlayingEpisode()
        {
            // If we have an episodeId stored in local cache, this means we returned to the app and 
            // have that episode playing. Hence, here we need to reload the episode data from the SQL. 
            App.CurrentlyPlayingEpisode = currentlyPlayingEpisode();
            if (App.CurrentlyPlayingEpisode != null)
            {
                App.CurrentlyPlayingEpisode.setPlaying();
            }
        }

        private void PlayStateChanged(object sender, EventArgs e)
        {
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    PodcastEpisodeModel currentEpisode = currentlyPlayingEpisode();
                    if (currentEpisode == null)
                    {
                        Debug.WriteLine("Error: No playing episode in DB.");
                        return;
                    }

                    if (App.CurrentlyPlayingEpisode != null
                        && (currentEpisode.EpisodeId != App.CurrentlyPlayingEpisode.EpisodeId))
                    {
                        // If next episode is different from currently playing, the track changed.
                        App.CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Listened;
                        App.CurrentlyPlayingEpisode.setNoPlaying();
                    }

                    App.CurrentlyPlayingEpisode = currentEpisode;
                    App.CurrentlyPlayingEpisode.setPlaying();
                    break;

                case PlayState.Paused:
                    break;

                case PlayState.Stopped:
                case PlayState.Shutdown:
                    if (App.CurrentlyPlayingEpisode == null)
                    {
                        return;
                    }

                    addEpisodeToPlayHistory(App.CurrentlyPlayingEpisode);
                    clearPlayList();
                    PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(App.CurrentlyPlayingEpisode.PodcastSubscriptionInstance);
                    App.CurrentlyPlayingEpisode = null;
                    BackgroundAudioPlayer.Instance.Close();
                    break;

                case PlayState.TrackReady:
                    break;

                case PlayState.Unknown:
                    break;
            }
        }

    }
}
