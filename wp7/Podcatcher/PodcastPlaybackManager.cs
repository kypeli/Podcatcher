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
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;


namespace Podcatcher
{
    public class PodcastPlaybackManager
    {
        public event EventHandler OnOpenPodcastPlayer;

        private static PodcastPlaybackManager m_instance;

        private PodcastPlaybackManager()
        {
//            initializeCurrentlyPlayingEpisode();
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

        public void play(PodcastEpisodeModel episode, bool openPlayerView = true)
        {
            bool startedFromEpisodeListing = openPlayerView;

            if (episode == null)
            {
                Debug.WriteLine("Warning: Trying to play a NULL episode.");
                return;
            }

            Debug.WriteLine("Starting playback for episode: ");
            Debug.WriteLine(" Name: " + episode.EpisodeName);
            Debug.WriteLine(" File: " + episode.EpisodeFile);
            Debug.WriteLine(" Location: " + episode.EpisodeDownloadUri);

            if (App.CurrentlyPlayingEpisode != null
                && (episode.EpisodeId != App.CurrentlyPlayingEpisode.EpisodeId))
            {
                addEpisodeToPlayHistory(App.CurrentlyPlayingEpisode);
                saveEpisodePlayPosition(App.CurrentlyPlayingEpisode);

                // If next episode is different from currently playing, the track changed.
                App.CurrentlyPlayingEpisode.setNoPlaying();
            }

            // We started to play a new podcast by tapping on the "Play" button in the subscription. 
            // We would then assume that a new playlist is created where this episode is the first item.
            if (startedFromEpisodeListing)
            {
                if (BackgroundAudioPlayer.Instance.PlayerState != PlayState.Paused
                    || (App.CurrentlyPlayingEpisode != null 
                        && App.CurrentlyPlayingEpisode.EpisodeId != episode.EpisodeId))
                {
                    App.CurrentlyPlayingEpisode = episode;
                    clearPlayQueue();
                }
            }
            else
            {
                App.CurrentlyPlayingEpisode = episode;
            }

            // Play locally from a downloaded file.
            if (episode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
            {
                PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                episode.setPlaying();
                episode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Playing;
                player.playEpisode(episode);
            }
            else
            {
                // Stream it if not downloaded. 
                if (PodcastPlayerControl.isAudioPodcast(episode))
                {
                    episode.setPlaying();
                    audioStreaming(episode);
                    episode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
                }
                else
                {
                    PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                    player.StopPlayback();
                    videoStreaming(episode);
                    episode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
                    openPlayerView = false;
                }
            }
            
            if (openPlayerView)
            {
                OnOpenPodcastPlayer(this, new EventArgs());
            }

            App.mainViewModels.PlayQueue = new System.Collections.ObjectModel.ObservableCollection<PlaylistItem>(); // Notify playlist changed.
        }

        public void startPlaylistPlayback()
        {
            int playlistItemId = -1;
            using (var db = new PlaylistDBContext())
            {
                PlaylistItem firstItem = db.Playlist.OrderBy(item => item.OrderNumber).FirstOrDefault();
                playlistItemId = firstItem.ItemId;                
            }

            playPlaylistItem(playlistItemId);
        }

        public void playPlaylistItem(int tappedPlaylistItemId)
        {
            int episodeId = -1;
            using (var db = new PlaylistDBContext())
            {
                if (db.Playlist.Count() < 1)
                {
                    return;
                }

                PlaylistItem current = db.Playlist.FirstOrDefault(item => item.IsCurrent == true);
                if (current != null
                    && current.ItemId == tappedPlaylistItemId)
                {
                    Debug.WriteLine("Tapped on the currently playing episode. I am not changing the track...");
                    return;
                }

                episodeId = (int)db.Playlist.Where(item => item.ItemId == tappedPlaylistItemId).Select(item => item.EpisodeId).First();
            }

            PodcastEpisodeModel episode = null;
            using (var db = new PodcastSqlModel())
            {
                episode = db.Episodes.First(ep => ep.EpisodeId == episodeId);
            }

            if (episode != null)
            {
                play(episode, false);
            }
            else
            {
                Debug.WriteLine("Warning: Could not play episode: " + episodeId);
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

        public void addToPlayqueue(Collection<PodcastEpisodeModel> episodes)
        {
            using (var db = new PlaylistDBContext())
            {
                foreach (PodcastEpisodeModel e in episodes)
                {
                    addToPlayqueue(e, db);
                }
                db.SubmitChanges();
            }

            using (var db = new PodcastSqlModel())
            {
                sortPlaylist(db.settings().PlaylistSortOrder);
            }

            App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
            showAddedNotification(episodes.Count);
        }

        public void addToPlayqueue(PodcastEpisodeModel episode, bool showNotification = true)
        {
            using (var db = new PlaylistDBContext())
            {
                addToPlayqueue(episode, db);
                db.SubmitChanges();
            }

            using (var db = new PodcastSqlModel())
            {
                sortPlaylist(db.settings().PlaylistSortOrder);
            }

            App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();

            if (showNotification)
            {
                showAddedNotification(1);
            }
        }

        public void addSilentlyToPlayqueue(PodcastEpisodeModel episode)
        {
            addToPlayqueue(episode, false);
        }

        public void removeFromPlayqueue(int itemId)
        {
            using (var db = new PlaylistDBContext())
            {
                PlaylistItem itemToRemove = db.Playlist.FirstOrDefault(item => item.IsCurrent == false && item.ItemId == itemId);
                if (itemToRemove != null) 
                {
                    db.Playlist.DeleteOnSubmit(itemToRemove);
                    db.SubmitChanges();
                    App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
                }
            }
        }

        public void clearPlayQueue()
        {
            using (var db = new PlaylistDBContext())
            {
                List<PlaylistItem> playlist = db.Playlist.ToList();
                foreach (PlaylistItem item in playlist)
                {
                    if (item.IsCurrent == false)
                    {
                        db.Playlist.DeleteOnSubmit(item);
                    }
                }

                db.SubmitChanges();
            }

            App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
        }

        public void sortPlaylist(int sortOrder)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(workerSortPlaylist);
            worker.RunWorkerAsync(sortOrder);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerSortPlaylistCompleted);
        }

        /****************************** Private implementations *******************************/

        private void showAddedNotification(int count)
        {
            String notification = String.Format("{0} podcast{1} added to play queue.", count, (count > 1) ? "s" : "");
            App.showNotificationToast(notification);
        }
        
        private void workerSortPlaylist(object sender, DoWorkEventArgs args)
        {
            int selectedSortOrderIndex = (int)args.Argument;
            using (var playlistDB = new PlaylistDBContext())
            {
                PodcastSqlModel sqlContext = new PodcastSqlModel();
                IEnumerable<PlaylistItem> newSortOrderQuery = null;
                List<PlaylistItem> newSortOrder = new List<PlaylistItem>();

                var query = playlistDB.Playlist.AsEnumerable().Join(episodes(sqlContext),
                                                                    item => item.EpisodeId,
                                                                    episode => episode.EpisodeId,
                                                                    (item, episode) => new { PlaylistItem = item, PodcastEpisodeModel = episode });
                switch (selectedSortOrderIndex)
                {
                    // Oldest first
                    case 0:
                        newSortOrderQuery = query.OrderBy(newPlaylistItem => newPlaylistItem.PodcastEpisodeModel.EpisodePublished)
                                            .Select(newPlaylistItem => newPlaylistItem.PlaylistItem).AsEnumerable();
                        break;
                    // Newest first.
                    case 1:
                        newSortOrderQuery = query.OrderByDescending(newPlaylistItem => newPlaylistItem.PodcastEpisodeModel.EpisodePublished)
                                            .Select(newPlaylistItem => newPlaylistItem.PlaylistItem).AsEnumerable();
                        break;
                }

                int i = 0;
                foreach (PlaylistItem item in newSortOrderQuery)
                {
                    PlaylistItem newItem = item;
                    newItem.OrderNumber = i++;
                    newSortOrder.Add(newItem);
                }

                playlistDB.Playlist.DeleteAllOnSubmit(playlistDB.Playlist);
                playlistDB.Playlist.InsertAllOnSubmit(newSortOrder);
                playlistDB.SubmitChanges();
            }
        }

        private void workerSortPlaylistCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
        }

        private static IEnumerable<PodcastEpisodeModel> episodes(PodcastSqlModel sqlContext)
        {
            return sqlContext.Episodes.AsQueryable();
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

        private void addEpisodeToPlayHistory(PodcastEpisodeModel episode)
        {
            using (var db = new PodcastSqlModel())
            {
                db.addEpisodeToPlayHistory(episode);
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

        private void saveEpisodePlayPosition(PodcastEpisodeModel episode)
        {
            try
            {
                episode.SavedPlayPos = BackgroundAudioPlayer.Instance.Position.Ticks;
            }
            catch (NullReferenceException)
            {
                Debug.WriteLine("BackgroundAudioPlayer returned NULL. Player didn't probably have a track that it was playing.");
                return;
            }
            catch (SystemException)
            {
                Debug.WriteLine("Got system exception when trying to save position.");
                return;
            }

            using (var db = new PodcastSqlModel())
            {
                PodcastEpisodeModel e = db.Episodes.Where(ep => ep.EpisodeId == episode.EpisodeId).First();
                e.SavedPlayPos = episode.SavedPlayPos;
                db.SubmitChanges();
            }
        }

        private void GoBack()
        {
            PhoneApplicationFrame rootFrame = Application.Current.RootVisual as PhoneApplicationFrame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void addToPlayqueue(PodcastEpisodeModel e, PlaylistDBContext dbContext)
        {
            PlaylistItem existingItem = dbContext.Playlist.FirstOrDefault(item => item.EpisodeId == e.EpisodeId);
            if (existingItem != null)
            {
                Debug.WriteLine("Item already in playlist.");
                return;
            }

            dbContext.Playlist.InsertOnSubmit(new PlaylistItem
            {
                EpisodeId = e.EpisodeId,
                EpisodeName = e.EpisodeName,
                EpisodeLocation = (!String.IsNullOrEmpty(e.EpisodeFile) ? e.EpisodeFile : e.EpisodeDownloadUri),
                PodcastLogoLocation = e.PodcastSubscriptionInstance.PodcastLogoLocalLocation,
                PodcastName = e.PodcastSubscriptionInstance.PodcastName
            });
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

                    if (App.CurrentlyPlayingEpisode == null)
                    {
                        App.CurrentlyPlayingEpisode = currentEpisode;
                    } else if (currentEpisode.EpisodeId != App.CurrentlyPlayingEpisode.EpisodeId)
                    {
                        App.CurrentlyPlayingEpisode = currentEpisode;
                        App.CurrentlyPlayingEpisode.setPlaying();
                    }
                    break;

                case PlayState.Paused:
                    saveEpisodePlayPosition(App.CurrentlyPlayingEpisode);
                    if (App.CurrentlyPlayingEpisode != null)
                    {
                        App.CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Paused;
                    }
                    else
                    {
                        Debug.WriteLine("SHOULD NOT HAPPEND! Cannot save episode state to paused!");
                    }
                    break;

                case PlayState.Stopped:
                case PlayState.Shutdown:
                    if (App.CurrentlyPlayingEpisode == null)
                    {
                        // We didn't have a track playing.
                        return;
                    }

                    saveEpisodePlayPosition(App.CurrentlyPlayingEpisode);
                    addEpisodeToPlayHistory(App.CurrentlyPlayingEpisode);
                    PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(App.CurrentlyPlayingEpisode.PodcastSubscriptionInstance);
                    
                    // Cleanup
                    App.CurrentlyPlayingEpisode = null;
                    BackgroundAudioPlayer.Instance.Close();
                    GoBack();
                    break;

                case PlayState.TrackReady:
                    break;

                case PlayState.TrackEnded:
                    saveEpisodePlayPosition(App.CurrentlyPlayingEpisode);
                    break;

                case PlayState.Unknown:
                    break;
            }
        }

    }
}
