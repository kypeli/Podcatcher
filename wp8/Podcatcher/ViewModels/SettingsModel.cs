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
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;

namespace Podcatcher.ViewModels
{
    [Table]
    public class SettingsModel
    {
        public const int KEEP_ALL_EPISODES = 9999;

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

        private int m_playlistSortOrder = 0;
        [Column(DbType = "INT DEFAULT 0 NOT NULL")]
        public int PlaylistSortOrder
        {
            get
            {
                return m_playlistSortOrder;
            }

            set
            {
                if (m_playlistSortOrder != value)
                {
                    m_playlistSortOrder = value;
                }
            }
        }

        private bool m_addDownloadsToPlayQueue = false;
        [Column(DbType = "BIT DEFAULT 0 NOT NULL")]
        public bool IsAddDownloadsToPlayQueue
        {
            get
            {
                return m_addDownloadsToPlayQueue;
            }

            set
            {
                if (m_addDownloadsToPlayQueue != value)
                {
                    m_addDownloadsToPlayQueue = value;
                }
            }
        }

        public static int keepNumEpisodesForSelectedIndex(int index)
        {
            switch (index)
            {
                case 1:
                    return 3;
                case 2:
                    return 10;
                case 3:
                    return 20;
                case 4:
                    return 30;
                case 0:
                default:
                    return 9999;
            }
        }

        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary version;
    }
}
