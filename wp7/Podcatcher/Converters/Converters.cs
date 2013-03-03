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
using System.Windows.Data;
using Podcatcher.ViewModels;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Phone.Shell;
using System.Linq;
using System.Collections.ObjectModel;

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
            ObservableCollection<PodcastEpisodeModel> model = value as ObservableCollection<PodcastEpisodeModel>;
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
            string playImageSource = "/Images/" + App.CurrentTheme + "/play_episode.png";

            PodcastEpisodeModel.EpisodePlayStateEnum episodeState = (PodcastEpisodeModel.EpisodePlayStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodePlayStateEnum.Playing:
                case PodcastEpisodeModel.EpisodePlayStateEnum.Streaming:
                    playImageSource = "/Images/" + App.CurrentTheme + "/play_episode_disabled.png";
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
            string downloadImageSource = "/Images/" + App.CurrentTheme + "/download_episode.png";

            PodcastEpisodeModel.EpisodeDownloadStateEnum episodeState = (PodcastEpisodeModel.EpisodeDownloadStateEnum)value;
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Downloading:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.Queued:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWiFi:
                case PodcastEpisodeModel.EpisodeDownloadStateEnum.WaitingForWifiAndPower:
                    downloadImageSource = "/Images/" + App.CurrentTheme + "/download_episode_disabled.png";
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

    public class NewEpisodesTextVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String newEpisodesText = value as String;
            if (String.IsNullOrEmpty(newEpisodesText) == false)
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

    public class ShowPinToStart : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int episodeId = (int)value;
            ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("podcastId=" + episodeId + "&"));
            if (TileToFind == null)
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

}

