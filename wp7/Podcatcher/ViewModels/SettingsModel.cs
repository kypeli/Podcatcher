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
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;

namespace Podcatcher.ViewModels
{
    [Table]
    public class SettingsModel
    {
        private int m_settingsId;
        [Column(IsPrimaryKey = true, CanBeNull = false, IsDbGenerated = true)]
        public int SettingsId
        {
            get { return m_settingsId; }
            set { m_settingsId = value; }
        }

        private bool m_IsAutomaticContinuedPlayback = false;
        [Column]
        public Boolean IsAutomaticContinuedPlayback
        {
            get
            {
                return m_IsAutomaticContinuedPlayback;
            }

            set 
            {
                if (m_IsAutomaticContinuedPlayback != value)
                {
                    m_IsAutomaticContinuedPlayback = value;
                }
            }
        }

        private bool m_IsAutoDelete = false;
        [Column]
        public Boolean IsAutoDelete
        {
            get
            {
                return m_IsAutoDelete;
            }

            set
            {
                if (m_IsAutoDelete != value)
                {
                    m_IsAutoDelete = value;
                }
            }
        }

        private bool m_IsNeverUseCellularData = false;
        [Column]
        public Boolean IsNeverUseCellularData
        {
            get
            {
                return m_IsNeverUseCellularData;
            }

            set
            {
                if (m_IsNeverUseCellularData != value)
                {
                    m_IsNeverUseCellularData = value;
                }
            }
        }
    }
}
