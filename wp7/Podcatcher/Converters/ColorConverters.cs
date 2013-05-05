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
using System.Globalization;

namespace Podcatcher.Converters
{
    public class PlayQueueItemBackgroundColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCurrent = (bool)value;

            Brush background = new SolidColorBrush(Colors.Transparent);
            if (isCurrent)
            {
                Color currentAccentColor = (Color)Application.Current.Resources["PhoneAccentColor"];
                background = new SolidColorBrush(currentAccentColor);
                background.Opacity = 0.4;
            }

            return background;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
