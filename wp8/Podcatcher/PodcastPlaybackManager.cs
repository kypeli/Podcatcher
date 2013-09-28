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
using System.Threading.Tasks;


namespace Podcatcher
{
    public class PodcastPlaybackManager
    {
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

                    if (value != null 
                        && m_currentlyPlayingEpisode != null 
                        && (value as PodcastEpisodeModel).EpisodeId == m_currentlyPlayingEpisode.EpisodeId)
                    {
                        return;
                    }

                    m_currentlyPlayingEpisode = value;
                    if (m_currentlyPlayingEpisode != null)
                    {
                        // We can't handle video podcasts' events so we don't handle them.
                        if (isAudioPodcast(value) == false)
                        {
                            Debug.WriteLine("Playing video podcast. Ignoring.");
                            return;
                        }

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

            // We have another episode currently playing, and we switch the episode that we are playing.
            if (CurrentlyPlayingEpisode != null
                && (episode.EpisodeId != CurrentlyPlayingEpisode.EpisodeId))
            {
                CurrentlyPlayingEpisode.setNoPlaying();

                try
                {
                    CurrentlyPlayingEpisode.SavedPlayPos = BackgroundAudioPlayer.Instance.Position.Ticks;
                }
                catch (Exception)
                {
                    Debug.WriteLine("Could not set saved play pos; not available.");
                    CurrentlyPlayingEpisode.SavedPlayPos = 0;
                } 

                using (var db = new PodcastSqlModel())
                {
                    PodcastEpisodeModel savingEpisode = db.Episodes.FirstOrDefault(ep => ep.EpisodeId == CurrentlyPlayingEpisode.EpisodeId);
                    savingEpisode.SavedPlayPos = CurrentlyPlayingEpisode.SavedPlayPos;
                    db.SubmitChanges();
                }
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
                CurrentlyPlayingEpisode = episode;
            
                // Clear play queue (yes) when we start playback from episode listing.
                // And we clear the queue after the current episode is being set, so that we don't delete the currently 
                // playing one.
                clearPlayQueue();
            }

            // Play locally from a downloaded file.
            if (CurrentlyPlayingEpisode != null
                && CurrentlyPlayingEpisode.EpisodeDownloadState == PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded)
            {
                PodcastPlayer player = PodcastPlayer.getIntance();
                CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Playing;
                player.playEpisode(CurrentlyPlayingEpisode);
            }
            else
            {
                // Stream it if not downloaded. 
                if (isAudioPodcast(CurrentlyPlayingEpisode))
                {
                    audioStreaming(CurrentlyPlayingEpisode);
                    CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
                }
                else
                {
                    PodcastPlayer player = PodcastPlayer.getIntance();
                    videoStreaming(episode);
                    CurrentlyPlayingEpisode.EpisodePlayState = PodcastEpisodeModel.EpisodePlayStateEnum.Streaming;
                }
            }

            var handlerStartedPlaying = OnPodcastStartedPlaying;
            if (handlerStartedPlaying != null)
            {
                if (isAudioPodcast(episode))
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

                episodeId = (int)db.Playlist.Where(item => item.ItemId == tappedPlaylistItemId).Select(item => item.EpisodeId).First();
            }

            PodcastEpisodeModel episode = null;
            using (var db = new PodcastSqlModel())
            {
                episode = db.Episodes.First(ep => ep.EpisodeId == episodeId);
            }

            if (episode != null)
            {
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

        public async void addToPlayqueue(Collection<PodcastEpisodeModel> episodes)
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
                await Task.Run(() => sortPlaylist(db.settings().PlaylistSortOrder));
            }

            App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
            showAddedNotification(episodes.Count);
        }

        public async void addToPlayqueue(PodcastEpisodeModel episode, bool showNotification = true)
        {
            using (var db = new PlaylistDBContext())
            {
                addToPlayqueue(episode, db);
                db.SubmitChanges();
            }

            using (var db = new PodcastSqlModel())
            {
                await Task.Run(() => sortPlaylist(db.settings().PlaylistSortOrder));
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
                case "audio/mpegaudio":
                case "audio/m4a":
                case "audio/x-mpeg":
                case "media/mpeg":
                case "x-audio/mp3":
                case "audio/x-mpegurl":
                    audio = true;
                    break;
            }

            return audio;
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
            using (var playlistDB = new PlaylistDBContext())
            {
                if (playlistDB.Playlist.Count() <= 1)
                {
                    return;
                }

                PodcastSqlModel sqlContext = new PodcastSqlModel();
                IEnumerable<PlaylistItem> newSortOrderQuery = null;
                List<PlaylistItem> newSortOrder = new List<PlaylistItem>();

                var query = playlistDB.Playlist.AsEnumerable().Join(episodes(sqlContext),
                                                                    item => item.EpisodeId,
                                                                    episode => episode.EpisodeId,
                                                                    (item, episode) => new { PlaylistItem = item, PodcastEpisodeModel = episode });
                switch (sortOrder)
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

            App.mainViewModels.PlayQueue = new ObservableCollection<PlaylistItem>();
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
            PodcastPlayer player = PodcastPlayer.getIntance();
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
                       //  CurrentlyPlayingEpisode.setPlaying();
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

                            PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(episode.PodcastSubscription);
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

                    // FUCK YOU WINDOWS PHONE!!
                    /**
                     * This is so horrible that I can't find other words to describe how bad the BackgroundAudioPlayer
                     * is in Windows Phone!
                     * 
                     * First of all the events are fired totally differently between Windows Phone 7 and Windows Phone 8. 
                     * Secondly the events related to when tracks end arae totally wrong and horrible! Let me explain:
                     * - We get here the PlayState.Stopped event when the track ends. 
                     * - We get the event here BEFORE the AudioAgent gets any events.
                     * - AudioAgent correctly gets the PlayState.TrackEnded event, but it gets it after we have received the 
                     *   event here.
                     * - We NEVER get the the PlayState.TrackEnded event here.
                     * - Which is not the case for Windows Phone 7, because it first fires PlayState.TrackEnded. 
                     *
                     * So this code here is a horrible kludge that just guesses that Windows Phone means "track did end" 
                     * when the state is stopped and the play position is still 0.
                     * 
                     * Johan, when you return from the future to this piece of code, don't even try to change it or "fix it".
                     **/
                    long playpos = 0;
                    if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.TrackEnded)
                    {
                        playpos = CurrentlyPlayingEpisode.TotalLengthTicks;
                    }
                    else if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Stopped
                      && BackgroundAudioPlayer.Instance.Position.Ticks == 0)
                    {
                        playpos = CurrentlyPlayingEpisode.TotalLengthTicks;
                    }
                    else
                    {
                        try
                        {
                            playpos = BackgroundAudioPlayer.Instance.Position.Ticks;
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine("Warning: Player didn't return us a play pos!");
                        }
                    }

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

                            PodcastSubscriptionsManager.getInstance().podcastPlaystateChanged(savingEpisode.PodcastSubscription);
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
