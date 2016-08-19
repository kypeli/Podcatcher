/**
 * This file is part of Podcatcher for Sailfish OS.
 * Authors: Moritz Carmesin (carolus@carmesinus.de)
 *
 * Podcatcher for Sailfish OS is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Podcatcher for Sailfish OS is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Podcatcher for Sailfish OS.  If not, see <http://www.gnu.org/licenses/>.
 */

import QtQuick 2.0
import Sailfish.Silica 1.0

import  org.nemomobile.configuration 1.0

Dialog {

    ConfigurationValue{
        id:autoDownloadConf
        key: "/apps/ControlPanel/Podcatcher/autodownload"
        defaultValue: true
    }

    ConfigurationValue{
        id:autoDownloadNumConf
        key: "/apps/ControlPanel/Podcatcher/autodownload_num"
        defaultValue: 1
    }

    ConfigurationValue{
        id: keepEpisodesConf
        key: "/apps/ControlPanel/Podcatcher/keep_episodes"
        defaultValue: 0
    }

    ConfigurationValue{
        id: keepUnplayedConf
        key: "/apps/ControlPanel/Podcatcher/keep_unplayed"
        defaultValue: 0
    }

    Column{
        anchors.fill: parent

    DialogHeader{
        id:header
        title: qsTr("Settings")
    }

    TextSwitch{
        id: autoDownload
        text: "Auto-download podcasts"
        description: "Should Podcatcher automatically download new episodes when the device is connected to the WiFi."
    }

    ComboBox{
        id: autoDownloadNum

        label: qsTr("How many podcasts to auto-download")
        description: qsTr("The number of podcast episodes that should be automatically downloaded.")

        menu: ContextMenu{
            MenuItem{
                text: "1"
            }

            MenuItem{
                text: "5"
            }

            MenuItem{
                text: "0"
            }
        }
    }

    ComboBox{
        id: keepEpisodes
        label: qsTr("Remove podcast episodes older than days")
        description: qsTr("Remove podcast episodes that are older than the number of days specified here. 0 means do not remove any.")

        menu: ContextMenu{
            MenuItem{
                text: "5"
            }

            MenuItem{
                text: "10"
            }

            MenuItem{
                text: "0"
            }
        }
    }



    TextSwitch{
        id: keepUnplayed
        text: qsTr("Keep unplayed episodes")
        description: qsTr("Remove podcast episodes that are older than the number of days specified here. 0 means do not remove any.")
    }
}

onOpened: {
    autoDownload.checked = autoDownloadConf.value;
    autoDownloadNum.value = autoDownloadNumConf.value;
    keepEpisodes.value = keepEpisodesConf.value;
    keepUnplayed.checked = keepUnplayedConf.value;
}

onAccepted: {
    autoDownloadConf.value = autoDownload.checked;
    autoDownloadNumConf.value = autoDownloadNum.value;
    keepEpisodesConf.value = keepEpisodes.value;
     keepUnplayedConf.value = keepUnplayed.checked;
}


}

