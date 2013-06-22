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
        public event EventHandler OnPodcastStartedPlaying;
        public event EventHandler OnPodcastStoppedPlaying;

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

        private PodcastEpisodeModel m_currentlyPlayingEpisode = null;
        public PodcastEpisodeModel CurrentlyPlayingEpisode
        {
            get
            {
                return m_currentlyPlayingEpisode;
            }

            set
            {
                using (var db = new Podcatcher.PlaylistDBContext())
                {
                    PlaylistItem current = db.Playlist.FirstOrDefault(item => item.IsCurrent == true);
                    if (current != null)
                    {
                        current.IsCurrent = false;
                    }

                    m_currentlyPlayingEpisode = value;
                    if (m_currentlyPlayingEpisode != null)
                    {
                        PodcastSubscriptionModel subscription = m_currentlyPlayingEpisode.PodcastSubscriptionInstance;
                        PlaylistItem newCurrent = db.Playlist.FirstOrDefault(item => item.EpisodeId == m_currentlyPlayingEpisode.EpisodeId);
                        if (newCurrent == null)
                        {
                            newCurrent = new PlaylistItem
                            {
                                PodcastName = subscription.PodcastName,
                                PodcastLogoLocation = subscription.PodcastLogoLocalLocation,
                                EpisodeName = m_currentlyPlayingEpisode.EpisodeName,
                                EpisodeLocation = (String.IsNullOrEmpty(m_currentlyPlayingEpisode.EpisodeFile)) ?
                                                                    m_currentlyPlayingEpisode.EpisodeDownloadUri :
                                                                    m_currentlyPlayingEpisode.EpisodeFile,
                                EpisodeId = m_currentlyPlayingEpisode.EpisodeId,
                            };

                            db.Playlist.InsertOnSubmit(newCurrent);
                        }

                        newCurrent.IsCurrent = true;
                        m_currentlyPlayingEpisode.setPlaying();
                    }

                    db.SubmitChanges();
                }
            }
        }



        public void play(PodcastEpisodeModel episode, bool startedFromPlayQueue = false)
        {
            if (episode == null)
            {
                Debug.WriteLine("Warning: Trying to play a NULL episode.");
                return;
            }

            Debug.WriteLine("Starting playback for episode: ");
            Debug.WriteLine(" Name: " + episode.EpisodeName);
            Debug.WriteLine(" File: " + episode.EpisodeFile);
            Debug.WriteLine(" Location: " + episode.EpisodeDownloadUri);

            if (CurrentlyPlayingEpisode != null
                && (episode.EpisodeId != CurrentlyPlayingEpisode.EpisodeId))
            {
                addEpisodeToPlayHistory(CurrentlyPlayingEpisode);

                // If next episode is different from currently playing, the track changed.
                CurrentlyPlayingEpisode.setNoPlaying();
            }

            if (startedFromPlayQueue)
            {
                if (BackgroundAudioPlayer.Instance.PlayerState != PlayState.Paused
                    || (CurrentlyPlayingEpisode != null
                        && CurrentlyPlayingEpisode.EpisodeId != episode.EpisodeId))
                {
                    CurrentlyPlayingEpisode = episode;
                }
            }
            else
            {
                if (PodcastPlayerControl.isAudioPodcast(episode))
                {
                    CurrentlyPlayingEpisode = episode;
                }

                // Clear play queue (yes) when we start playback from episode listing.
                // And we clear the queue after the current episode is being set, so that we don't delete the currently 
                // playing one.
                clearPlayQueue();
            }

            // Play locally from a downloaded file.
            if (CurrentlyPlayingEpisode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
            {
                PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                CurrentlyPlayingEpisode.setPlaying();
                CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Playing;
                player.playEpisode(CurrentlyPlayingEpisode);
            }
            else
            {
                // Stream it if not downloaded. 
                if (PodcastPlayerControl.isAudioPodcast(CurrentlyPlayingEpisode))
                {
                    CurrentlyPlayingEpisode.setPlaying();
                    audioStreaming(CurrentlyPlayingEpisode);
                    CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
                }
                else
                {
                    PodcastPlayerControl player = PodcastPlayerControl.getIntance();
                    player.StopPlayback();
                    videoStreaming(episode);
                    CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
                }
            }

            // Always open the player view.
            var handler = OnOpenPodcastPlayer;
            if (handler != null)
            {
                OnOpenPodcastPlayer(this, new EventArgs());
            }

            var handlerStartedPlaying = OnPodcastStartedPlaying;
            if (handlerStartedPlaying != null)
            {
                if (PodcastPlayerControl.isAudioPodcast(episode))
                {
                    OnPodcastStartedPlaying(this, new EventArgs());
                }
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

                // Did we tap the item that is currently playing? 
                PlaylistItem current = db.Playlist.FirstOrDefault(item => item.IsCurrent == true);
                if (current != null
                    && current.ItemId == tappedPlaylistItemId)
                {
                    Debug.WriteLine("Tapped on the currently playing episode. I am not changing the track...");

                    // Always open the player UI when playlist item is tapped.
                    var handler = OnOpenPodcastPlayer;
                    if (handler != null)
                    {
                        OnOpenPodcastPlayer(this, new EventArgs());
                    }
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
                CurrentlyPlayingEpisode = episode;
                play(episode, true);
            }
            else
            {
                Debug.WriteLine("Warning: Could not play episode: " + episodeId);
            }
        }

        public PodcastEpisodeModel updateCurrentlyPlayingEpisode()
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
                    PodcastEpisodeModel currentEpisode = db.Episodes.Where(ep => ep.EpisodeId == plItem.EpisodeId).FirstOrDefault();
                    CurrentlyPlayingEpisode = currentEpisode;
                    return currentEpisode;
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
                PlaylistItem itemToRemove = db.Playlist.FirstOrDefault(item => item.ItemId == itemId);
                if (itemToRemove != null) 
                {
                    PodcastEpisodeModel episode = null;
                    using (var episodeDb = new PodcastSqlModel())
                    {
                        episode = episodeDb.episodeForPlaylistItem(itemToRemove);
                        if (episode != null)
                        {
                            if (episode.isListened())
                            {
                                episode.markAsListened(episodeDb.settings().IsAutoDelete);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Warning: Could not get episode for item id: " + itemToRemove.ItemId);
                        }
                    }

                    db.Playlist.DeleteOnSubmit(itemToRemove);
                    db.SubmitChanges();
                    App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
                }
            }
        }

        public void removeFromPlayqueue(PodcastEpisodeModel episode)
        {
            using (var db = new PlaylistDBContext())
            {
                PlaylistItem plItem = db.Playlist.FirstOrDefault(item => item.EpisodeId == episode.EpisodeId);
                if (plItem != null) 
                {
                    removeFromPlayqueue(plItem);
                }
            }
        }

        private void removeFromPlayqueue(PlaylistItem plItem)
        {
            removeFromPlayqueue(plItem.ItemId);
        }

        public void clearPlayQueue()
        {
            List<int> itemsToRemove = new List<int>();
            using (var db = new PlaylistDBContext())
            {
                List<PlaylistItem> playlist = db.Playlist.ToList();
                foreach (PlaylistItem item in playlist)
                {
                    if (item.IsCurrent == false)
                    {
                        itemsToRemove.Add(item.ItemId);
                    }
                }
            }

            foreach (int id in itemsToRemove)
            {
                removeFromPlayqueue(id);
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

        public void addEpisodeToPlayHistory(PodcastEpisodeModel episode)
        {
            using (var db = new PodcastSqlModel())
            {
                db.addEpisodeToPlayHistory(episode);
            }

            App.mainViewModels.PlayHistoryListProperty = new ObservableCollection<PodcastEpisodeModel>();
        }

        public bool isCurrentlyPlaying()
        {
            return CurrentlyPlayingEpisode != null;
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

        private void initializeCurrentlyPlayingEpisode()
        {
            // If we have an episodeId stored in local cache, this means we returned to the app and 
            // have that episode playing. Hence, here we need to reload the episode data from the SQL. 
            CurrentlyPlayingEpisode = updateCurrentlyPlayingEpisode();
            if (CurrentlyPlayingEpisode != null)
            {
                CurrentlyPlayingEpisode.setPlaying();
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
            EventHandler handlerStoppedPlaying = null;
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    PodcastEpisodeModel currentEpisode = updateCurrentlyPlayingEpisode();
                    if (currentEpisode == null)
                    {
                        Debug.WriteLine("Error: No playing episode in DB.");
                        return;
                    }

                    if (CurrentlyPlayingEpisode == null)
                    {
                        CurrentlyPlayingEpisode = currentEpisode;
                    } else if (currentEpisode.EpisodeId != CurrentlyPlayingEpisode.EpisodeId)
                    {
                        CurrentlyPlayingEpisode = currentEpisode;
                        CurrentlyPlayingEpisode.setPlaying();
                    }

                    if (CurrentlyPlayingEpisode.TotalLengthTicks == 0)
                    {
                        CurrentlyPlayingEpisode.TotalLengthTicks = BackgroundAudioPlayer.Instance.Track.Duration.Ticks;
                        using (var db = new PodcastSqlModel())
                        {
                            PodcastEpisodeModel episode = db.episodeForEpisodeId(CurrentlyPlayingEpisode.EpisodeId);
                            if (episode == null)
                            {
                                Debug.WriteLine("Warning: Got NULL episode from DB when trying to update this episode.");
                                return;
                            }

                            episode.TotalLengthTicks = CurrentlyPlayingEpisode.TotalLengthTicks;
                            db.SubmitChanges();
                        }
                    }

                    break;

                case PlayState.Paused:
                    BackgroundAudioPlayer playerPaused = BackgroundAudioPlayer.Instance;
                    if (CurrentlyPlayingEpisode != null)
                    {
                        CurrentlyPlayingEpisode = App.refreshEpisodeFromAudioAgent(CurrentlyPlayingEpisode);
                        CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Paused;
                        using (var db = new PodcastSqlModel())
                        {
                            PodcastEpisodeModel updatedEpisode = db.Episodes.FirstOrDefault(ep => ep.EpisodeId == CurrentlyPlayingEpisode.EpisodeId);
                            updatedEpisode.EpisodePlayState = CurrentlyPlayingEpisode.EpisodePlayState;
                            db.SubmitChanges();
                        }

                        App.mainViewModels.PlayHistoryListProperty = new ObservableCollection<PodcastEpisodeModel>();

                        handlerStoppedPlaying = OnPodcastStoppedPlaying;
                        if (handlerStoppedPlaying != null)
                        {
                            OnPodcastStoppedPlaying(this, new EventArgs());
                        }

                    }
                    else
                    {
                        Debug.WriteLine("SHOULD NOT HAPPEND! Cannot save episode state to paused!");
                    }
                    break;

                case PlayState.Stopped:
                case PlayState.Shutdown:
                case PlayState.TrackEnded:
                    if (CurrentlyPlayingEpisode == null)
                    {
                        // We didn't have a track playing.
                        return;
                    }

                    addEpisodeToPlayHistory(CurrentlyPlayingEpisode);
                    long playpos = App.getPlayposFromAudioAgentForEpisode(CurrentlyPlayingEpisode);
                    CurrentlyPlayingEpisode.SavedPlayPos = playpos;
                    CurrentlyPlayingEpisode.setNoPlaying();

                    using (var db = new PodcastSqlModel())
                    {
                        PodcastEpisodeModel savingEpisode = db.Episodes.FirstOrDefault(ep => ep.EpisodeId == CurrentlyPlayingEpisode.EpisodeId);
                        if (savingEpisode != null)
                        {
                            savingEpisode.SavedPlayPos = CurrentlyPlayingEpisode.SavedPlayPos;
                            // Update play state to listened as appropriate.
                            if (savingEpisode.isListened())
                            {
                                savingEpisode.markAsListened(db.settings().IsAutoDelete);
                                removeFromPlayqueue(savingEpisode);
                            }
                            else
                            {
                                savingEpisode.EpisodePlayState = CurrentlyPlayingEpisode.EpisodePlayState;
                            }
                            db.SubmitChanges();
                        }
                    }

                    PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(CurrentlyPlayingEpisode.PodcastSubscriptionInstance);
                    handlerStoppedPlaying = OnPodcastStoppedPlaying;
                    if (handlerStoppedPlaying != null)
                    {
                        OnPodcastStoppedPlaying(this, new EventArgs());
                    }

                    // Cleanup
                    CurrentlyPlayingEpisode = null;
                    BackgroundAudioPlayer.Instance.Close();
                    break;

                case PlayState.TrackReady:
                    break;

                case PlayState.Unknown:
                    // Unknown? WTF.
                    break;

            }

            App.mainViewModels.PlayQueue = new System.Collections.ObjectModel.ObservableCollection<PlaylistItem>();

            if (CurrentlyPlayingEpisode != null)
            {
                PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(CurrentlyPlayingEpisode.PodcastSubscriptionInstance);
            }
        }

    }
}
