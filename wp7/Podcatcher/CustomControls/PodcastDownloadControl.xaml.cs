using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Podcatcher
{
    public partial class PodcastDownloadControl : UserControl
    {
        /************************************* Public implementations *******************************/
        public PodcastDownloadControl()
        {
            InitializeComponent();
            this.DownloadProgressText.Text = "";
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.DownloadProgressText.Text = String.Format("{0} %", (sender as ProgressBar).Value);
        }
    }
}
