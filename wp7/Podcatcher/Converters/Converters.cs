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

    public class DownloadButtonActiveConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PodcastEpisodeModel.EpisodeStateVal episodeState = (PodcastEpisodeModel.EpisodeStateVal)value;
            bool buttonEnabled = true;

            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeStateVal.Queued:
                    buttonEnabled = false;
                    break;
                case PodcastEpisodeModel.EpisodeStateVal.Downloading:
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
            PodcastEpisodeModel.EpisodeStateVal episodeState = (PodcastEpisodeModel.EpisodeStateVal)value;

            String buttonText = @"Error!";
            switch (episodeState)
            {
                case PodcastEpisodeModel.EpisodeStateVal.Idle:
                    buttonText = @"Download";
                    break;
                case PodcastEpisodeModel.EpisodeStateVal.Queued:
                    buttonText = @"Queued";
                    break;
                case PodcastEpisodeModel.EpisodeStateVal.Downloading:
                    buttonText = @"Downloading";
                    break;
                case PodcastEpisodeModel.EpisodeStateVal.Playable:
                    buttonText = @"Play";
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

            PodcastEpisodeModel.EpisodeStateVal episodeState = (PodcastEpisodeModel.EpisodeStateVal)value;
            if (episodeState == PodcastEpisodeModel.EpisodeStateVal.Playable)
            {
                episodeContextMenuVisible = Visibility.Visible;
            }

            return episodeContextMenuVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


}
