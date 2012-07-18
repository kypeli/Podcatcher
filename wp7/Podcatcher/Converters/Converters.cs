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

// TODO: We need to handle player "Stop" so we can release the episode and delete it. 
/*                case PodcastEpisodeModel.EpisodeStateVal.Paused:
                    episodeContextMenuVisible = Visibility.Visible;
                    break;
 */ 
            }

            return episodeContextMenuVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}