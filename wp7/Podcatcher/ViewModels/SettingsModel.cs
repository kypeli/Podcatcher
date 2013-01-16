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
        public enum ExportMode
        {
            ExportToSkyDrive,
            ExportViaEmail
        };

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

        private bool m_TryUseCellularData = true;
        [Column]
        public Boolean IsUseCellularData
        {
            get
            {
                return m_TryUseCellularData;
            }

            set
            {
                if (m_TryUseCellularData != value)
                {
                    m_TryUseCellularData = value;
                }
            }
        }

        private int m_SelectedExportIndex = 0;
        [Column(DbType = "INT DEFAULT 0 NOT NULL")]
        public int SelectedExportIndex
        {
            get
            {
                return m_SelectedExportIndex;
            }

            set
            {
                if (m_SelectedExportIndex != value)
                {
                    m_SelectedExportIndex = value;
                }
            }
        }

        private int m_listenedThreashold = 90;
        [Column(DbType = "INT DEFAULT 90 NOT NULL")]
        public int ListenedThreashold 
        {
            get
            {
                return m_listenedThreashold;
            }

            set
            {
                if (m_listenedThreashold != value)
                {
                    m_listenedThreashold = value;
                }
            }
        }

        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary version;
    }
}
