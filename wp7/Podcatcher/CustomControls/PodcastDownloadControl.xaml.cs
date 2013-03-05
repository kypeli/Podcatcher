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
using System.Windows.Controls;
using System.Windows.Input;
using Podcatcher.ViewModels;
using System.Windows.Media.Imaging;

namespace Podcatcher
{
    public partial class PodcastDownloadControl : UserControl
    {
        private PodcastEpisodeModel m_episodeModel;

        /************************************* Public implementations *******************************/
        public PodcastDownloadControl()
        {
            InitializeComponent();
            this.DownloadProgressText.Text = "";
            this.CancelDownloadButton.Source = new BitmapImage(new Uri("/Images/" + App.CurrentTheme + "/minus.png", UriKind.Relative));

            Loaded += new RoutedEventHandler(PodcastDownloadControl_Loaded);
        }

        void PodcastDownloadControl_Loaded(object sender, RoutedEventArgs e)
        {
            m_episodeModel = this.DataContext as PodcastEpisodeModel;
            this.PodcastLogo.Source = m_episodeModel.PodcastLogo;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.DownloadProgressText.Text = String.Format("{0} %", (sender as ProgressBar).Value);
        }

        private void StopDownloadImage_Tap(object sender, GestureEventArgs e)
        {
            PodcastEpisodesDownloadManager.getInstance().cancelEpisodeDownload(m_episodeModel);            
        }
    }
}
