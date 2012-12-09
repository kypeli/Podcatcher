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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using Podcatcher.ViewModels;
using System.Globalization;
using System.Collections.Generic;

namespace Podcatcher.Converters
{
    public class NoSubscriptionsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<PodcastSubscriptionModel> model = value as List<PodcastSubscriptionModel>;
            if (model == null
                || model.Count < 1)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class NoDownloadedEpisodesVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<PodcastEpisodeModel> model = value as List<PodcastEpisodeModel>;
            if (model == null
                || model.Count < 1)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class EpisodeButtonActiveConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PodcastEpisodeModel.EpisodeStateEnum episodeState = (PodcastEpisodeModel.EpisodeStateEnum)value;
            bool buttonEnabled = true;

            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeStateEnum.Queued:
                    buttonEnabled = false;
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Downloading:
                    buttonEnabled = false;
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Saving:
                    buttonEnabled = false;
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Playing:
                    buttonEnabled = false;
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Paused:
                    buttonEnabled = false;
                    break;
            }

            return buttonEnabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DownloadButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PodcastEpisodeModel.EpisodeStateEnum episodeState = (PodcastEpisodeModel.EpisodeStateEnum)value;

            String buttonText = @"Error!";
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeStateEnum.Idle:
                    buttonText = @"Download";
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Queued:
                    buttonText = @"Queued";
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Downloading:
                    buttonText = @"Downloading";
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Saving:
                    buttonText = @"Saving...";
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Playable:
                    buttonText = @"Play";
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Playing:
                    buttonText = @"Playing";
                    break;
                case PodcastEpisodeModel.EpisodeStateEnum.Paused:
                    buttonText = @"Paused";
                    break;
            }

            return buttonText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ContextMenuEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility episodeContextMenuVisible = Visibility.Collapsed;

            PodcastEpisodeModel.EpisodeStateEnum episodeState = (PodcastEpisodeModel.EpisodeStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeStateEnum.Playable:
                    episodeContextMenuVisible = Visibility.Visible;
                    break;
            }

            return episodeContextMenuVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ProgressBarVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility progressBarVisible = Visibility.Collapsed;

            PodcastEpisodeModel.EpisodeStateEnum episodeState = (PodcastEpisodeModel.EpisodeStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeStateEnum.Downloading:
                case PodcastEpisodeModel.EpisodeStateEnum.Paused:
                case PodcastEpisodeModel.EpisodeStateEnum.Playable:
                    progressBarVisible = Visibility.Visible;
                    break;
            }

            return progressBarVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DownloadEpisodeVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility episodeDLVisible = Visibility.Collapsed;

            String episodeMimeType = (String)value;
            if (playableMimeType(episodeMimeType))
            {
                episodeDLVisible = Visibility.Visible;
            }

            return episodeDLVisible;
        }

        private bool playableMimeType(string episodeMimeType)
        {
            if (episodeMimeType == "-ERROR-")
            {
                return false;
            }

            // Since we added the MIME type in version 2 of DB, we have to assume that if the 
            // value is empty, we show the button.
            if (String.IsNullOrEmpty(episodeMimeType))
            {
                return true;
            }

            bool playable = false;
            switch (episodeMimeType)
            {
                case "audio/mpeg":
                case "audio/mp3":
                case "audio/x-mp3":
                case "audio/mpeg3":
                case "audio/x-mpeg3":
                case "audio/mpg":
                case "audio/x-mpg":
                case "audio/x-mpegaudio":
                    playable = true;
                    break;
                
                case "video/mp4":
                case "video/x-mp4":
                    playable = true;
                    break;
            }

            return playable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}