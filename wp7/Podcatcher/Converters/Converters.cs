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
            PodcastEpisodeModel.EpisodeDownloadStateEnum episodeState = (PodcastEpisodeModel.EpisodeDownloadStateEnum)value;
            bool buttonEnabled = true;

            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWiFi:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWifiAndPower:
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

    public class ContextMenuEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility episodeContextMenuVisible = Visibility.Collapsed;

            PodcastEpisodeModel.EpisodeDownloadStateEnum episodeState = (PodcastEpisodeModel.EpisodeDownloadStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloaded:
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

    public class PlayButtonImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string playImageSource = "/Podcatcher;component/Images/play_episode.png";

            PodcastEpisodeModel.EpisodePlayStateEnum episodeState = (PodcastEpisodeModel.EpisodePlayStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodePlayStateEnum.Paused:
                case PodcastEpisodeModel.EpisodePlayStateEnum.Playing:
                case PodcastEpisodeModel.EpisodePlayStateEnum.Streaming:
                    playImageSource = "/Podcatcher;component/Images/play_episode_disabled.png";
                    break;                    
            }

            return playImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DownloadButtonImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string downloadImageSource = "/Podcatcher;component/Images/download_episode.png";

            PodcastEpisodeModel.EpisodeDownloadStateEnum episodeState = (PodcastEpisodeModel.EpisodeDownloadStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWiFi:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWifiAndPower:
                    downloadImageSource = "/Podcatcher;component/Images/download_episode_disabled.png";
                    break;
            }

            return downloadImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class EpisodePlayButtonActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = true;

            PodcastEpisodeModel.EpisodePlayStateEnum episodeState = (PodcastEpisodeModel.EpisodePlayStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodePlayStateEnum.Playing:
                case PodcastEpisodeModel.EpisodePlayStateEnum.Paused:
                case PodcastEpisodeModel.EpisodePlayStateEnum.Streaming:
                    isActive = false;
                    break;
            }

            return isActive;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }



}