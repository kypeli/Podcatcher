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
using Microsoft.Phone.BackgroundAudio;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace PodcastAudioAgent
{
    public class AudioPlayer : AudioPlayerAgent
    {
        private const string LSKEY_PODCAST_EPISODE_PLAYING_ID = "playing_episodeId";
        private const string LSKEY_AA_EPISODE_PLAY_TITLE = "aa_episode_title";
        private const string LSKEY_AA_EPISODE_LAST_KNOWN_POS = "aa_episode_play_lastknownpos";
        private const string LSKEY_AA_EPISODE_LAST_KNOWN_TIMESTAMP = "aa_episode_play_starttime";
        private const string LSKEY_AA_EPISODE_STOP_TIMESTAMP = "aa_episode_play_stoptime";
        private IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        private const string LSKEY_PODCATCHER_MUTEX = "PodcatcherGlobalMutex";

        private static volatile bool _classInitialized;

        /// <remarks>
        /// AudioPlayer instances can share the same process. 
        /// Static fields can be used to share state between AudioPlayer instances
        /// or to communicate with the Audio Streaming agent.
        /// </remarks>
        public AudioPlayer()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += AudioPlayer_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void AudioPlayer_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Called when the playstate changes, except for the Error state (see OnError)
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time the playstate changed</param>
        /// <param name="playState">The new playstate of the player</param>
        /// <remarks>
        /// Play State changes cannot be cancelled. They are raised even if the application
        /// caused the state change itself, assuming the application has opted-in to the callback.
        /// 
        /// Notable playstate events: 
        /// (a) TrackEnded: invoked when the player has no current track. The agent can set the next track.
        /// (b) TrackReady: an audio track has been set and it is now ready for playack.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnPlayStateChanged(BackgroundAudioPlayer player, AudioTrack track, PlayState playState)
        {
            switch (playState)
            {
                case PlayState.TrackEnded:
                    saveEpisodeStoptime();
                    clearPlayHistory();
                    break;
                case PlayState.TrackReady:
                    break;
                case PlayState.Shutdown:
                    saveEpisodeStoptime();
                    clearPlayHistory();
                    break;
                case PlayState.Unknown:
                    saveEpisodeStoptime();
                    clearPlayHistory();
                    Debug.WriteLine("Play state: Unkown");
                    break;
                case PlayState.Stopped:
                    saveEpisodeStoptime();
                    clearPrimaryTile();
                    clearPlayHistory();
                    Debug.WriteLine("Play state: Stopped");
                    break;
                case PlayState.Paused:
                    saveEpisodeStoptime();
                    clearPlayHistory();
                    Debug.WriteLine("Play state: Paused");
                    break;
                case PlayState.Playing:
                    saveEpisodeStartinfo(player);
                    updateLastKnownPos(player);
                    Debug.WriteLine("Play state: Playing");
                    break;
                case PlayState.BufferingStarted:
                    break;
                case PlayState.BufferingStopped:
                    break;
                case PlayState.Rewinding:
                    break;
                case PlayState.FastForwarding:
                    break;
            }

            NotifyComplete();
        }

        private void clearPlayHistory()
        {
            using (var mutex = new Mutex(false, LSKEY_PODCATCHER_MUTEX))
            {
                mutex.WaitOne();
                settings.Remove(LSKEY_PODCAST_EPISODE_PLAYING_ID);
                settings.Save();
                mutex.ReleaseMutex();
            }
        }

        private void clearPrimaryTile()
        {
            ShellTile PrimaryTile = ShellTile.ActiveTiles.First();

            if (PrimaryTile != null)
            {
                StandardTileData tile = new StandardTileData();
                tile.BackBackgroundImage = new Uri("appdata:Background.png");
                tile.BackTitle = string.Empty;
                PrimaryTile.Update(tile);
            }
        }

        private void updateLastKnownPos(BackgroundAudioPlayer player)
        {
            using (var mutex = new Mutex(false, LSKEY_PODCATCHER_MUTEX))
            {
                mutex.WaitOne();
                if (settings.Contains(LSKEY_AA_EPISODE_LAST_KNOWN_POS))
                {
                    settings.Remove(LSKEY_AA_EPISODE_LAST_KNOWN_POS);
                }

                if (settings.Contains(LSKEY_AA_EPISODE_LAST_KNOWN_TIMESTAMP))
                {
                    settings.Remove(LSKEY_AA_EPISODE_LAST_KNOWN_TIMESTAMP);
                }

                try
                {
                    settings.Add(LSKEY_AA_EPISODE_LAST_KNOWN_TIMESTAMP, DateTime.Now.ToString());
                    settings.Add(LSKEY_AA_EPISODE_LAST_KNOWN_POS, player.Position.Ticks);
                    settings.Save();
                }
                catch (InvalidOperationException e)
                {
                    Debug.WriteLine("AudioPlayer:updateLastKnownPos - Player no longer available. Error:  " + e.Message);
                    return;
                }
                mutex.ReleaseMutex();
            }
        }

        private void saveEpisodeStoptime()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            using (var mutex = new Mutex(false, LSKEY_PODCATCHER_MUTEX))
            {
                mutex.WaitOne();
                if (settings.Contains(LSKEY_AA_EPISODE_STOP_TIMESTAMP))
                {
                    settings.Remove(LSKEY_AA_EPISODE_STOP_TIMESTAMP);
                }

                settings.Add(LSKEY_AA_EPISODE_STOP_TIMESTAMP, DateTime.Now.ToString());
                settings.Save();
                mutex.ReleaseMutex();
            }
        }

        private void saveEpisodeStartinfo(BackgroundAudioPlayer player)
        {
            if (player.Error != null)
            {
                Debug.WriteLine("AudioPlayer:saveEpisodeStartinfo - Player no longer available.");
                return;
            }

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            using (var mutex = new Mutex(false, LSKEY_PODCATCHER_MUTEX))
            {
                mutex.WaitOne();

                if (settings.Contains(LSKEY_AA_EPISODE_PLAY_TITLE))
                {
                    settings.Remove(LSKEY_AA_EPISODE_PLAY_TITLE);
                }

                try
                {
                    settings.Add(LSKEY_AA_EPISODE_PLAY_TITLE, player.Track.Title);
                }
                catch (InvalidOperationException e)
                {
                    Debug.WriteLine("AudioPlayer:saveEpisodeStartinfo - Player no longer available. Error:  " + e.Message);
                    return;
                }

                settings.Save();
                mutex.ReleaseMutex();
            }
        }


        /// <summary>
        /// Called when the user requests an action using application/system provided UI
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time of the user action</param>
        /// <param name="action">The action the user has requested</param>
        /// <param name="param">The data associated with the requested action.
        /// In the current version this parameter is only for use with the Seek action,
        /// to indicate the requested position of an audio track</param>
        /// <remarks>
        /// User actions do not automatically make any changes in system state; the agent is responsible
        /// for carrying out the user actions if they are supported.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnUserAction(BackgroundAudioPlayer player, AudioTrack track, UserAction action, object param)
        {
            switch (action)
            {
                case UserAction.Play:
                    try
                    {
                        if (player.PlayerState != PlayState.Playing)
                        {
                            Debug.WriteLine("User.Action: Play");
                            player.Play();
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine("Exception: " + e.Message);
                    }
                    catch (SystemException syse)
                    {
                        Debug.WriteLine("Exception: " + syse.Message);
                    }
                    break;
                case UserAction.Stop:
                    try
                    {
                        if (player.PlayerState != PlayState.Stopped) {    
                            player.Stop();
                            Debug.WriteLine("User.Action: Stop");
                        }
                    } 
                    catch (Exception e)
                    {
                       Debug.WriteLine("Exception: " + e.Message);
                    }
                    break;
                
                case UserAction.Pause:
                    Debug.WriteLine("User.Action: Pause");
                    try
                    {
                        if (player.PlayerState == PlayState.Playing)
                        {
                            player.Pause();
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine("Exception: " + e.Message);
                    }
                    break;
                
                case UserAction.Seek:
                    try
                    {
                        if (player.PlayerState == PlayState.Playing)
                        {
                            player.Position = (TimeSpan)param;
                            updateLastKnownPos(player);
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine("Exception: " + e.Message);
                    }
                    break;

                case UserAction.SkipNext:
                    Debug.WriteLine("Skip next.");
                    AudioTrack nextTrack = getNextPlaylistTrack();
                    if (nextTrack != null)
                    {
                        player.Track = nextTrack;
                        player.Play();
                    }
                    break;

                case UserAction.FastForward:
                    try
                    {
                        player.Position = TimeSpan.FromSeconds(player.Position.TotalSeconds + 30);
                        Debug.WriteLine("Player fast forward. New position: " + player.Position);
                    } catch(Exception) {
                        Debug.WriteLine("Error seeking. Probably seeked passed the end.");
                    }
                    break;

                case UserAction.SkipPrevious:
                    break;
                case UserAction.Rewind:
                    try
                    {
                        player.Position = TimeSpan.FromSeconds(player.Position.TotalSeconds - 30);
                        Debug.WriteLine("Player fast forward. New position: " + player.Position);
                    } catch(Exception) {
                        Debug.WriteLine("Error seeking. Probably seeked passed the start.");
                    }
                    break;
            }

            NotifyComplete();
        }

        private AudioTrack getNextPlaylistTrack()
        {
            AudioTrack track = null;
            using (var db = new Podcatcher.PlaylistDBContext())
            {
                Podcatcher.ViewModels.PlaylistItem currentTrack = (from item in db.Playlist
                                                                   orderby item.OrderNumber
                                                                   select item).FirstOrDefault();

                db.Playlist.DeleteOnSubmit(currentTrack);
                db.SubmitChanges();

                Podcatcher.ViewModels.PlaylistItem nextTrack = (from item in db.Playlist
                                                                orderby item.OrderNumber
                                                                select item).FirstOrDefault();

                if (nextTrack == null)
                {
                    return null;
                }

                track = new AudioTrack(new Uri(nextTrack.EpisodeLocation, UriKind.RelativeOrAbsolute),
                                               nextTrack.EpisodeName,
                                               nextTrack.PodcastName,
                                               "",
                                               new Uri(nextTrack.PodcastLogoLocation, UriKind.RelativeOrAbsolute));

                nextTrack.IsCurrent = true;

                db.SubmitChanges();
            }

            return track;
        }

        /// <summary>
        /// Called whenever there is an error with playback, such as an AudioTrack not downloading correctly
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track that had the error</param>
        /// <param name="error">The error that occured</param>
        /// <param name="isFatal">If true, playback cannot continue and playback of the track will stop</param>
        /// <remarks>
        /// This method is not guaranteed to be called in all cases. For example, if the background agent 
        /// itself has an unhandled exception, it won't get called back to handle its own errors.
        /// </remarks>
        protected override void OnError(BackgroundAudioPlayer player, AudioTrack track, Exception error, bool isFatal)
        {
            if (isFatal)
            {
                Abort();
            }
            else
            {
                NotifyComplete();
            }

        }

        /// <summary>
        /// Called when the agent request is getting cancelled
        /// </summary>
        /// <remarks>
        /// Once the request is Cancelled, the agent gets 5 seconds to finish its work,
        /// by calling NotifyComplete()/Abort().
        /// </remarks>
        protected override void OnCancel()
        {

        }
    }
}
