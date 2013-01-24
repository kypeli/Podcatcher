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
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;
using System.Diagnostics;

namespace Podcatcher.Views
{
    public partial class SettingsView : PhoneApplicationPage
    {
        private static bool initialized = false;
        private SettingsModel m_settings = null;

        public SettingsView()
        {
            InitializeComponent();
            initialized = true;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            m_settings = PodcastSqlModel.getInstance().settings();
            this.DataContext = m_settings;
            this.DeleteEpisodeThreshold.Value = m_settings.ListenedThreashold;
            this.DeleteThresholdPercent.Text = String.Format("{0} %", this.DeleteEpisodeThreshold.Value.ToString());
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            PodcastSqlModel.getInstance().SubmitChanges();
        }
        
        private void DeleteEpisodeThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!initialized
                || this.DeleteEpisodeThreshold == null)
            {
                return;
            }

            this.DeleteThresholdPercent.Text = String.Format("{0} %", ((int)e.NewValue).ToString());
            m_settings.ListenedThreashold = (int)e.NewValue;
        }
    }
}