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

