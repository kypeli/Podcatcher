/**
 * This file is part of Podcatcher for N9.
 * Author: Johan Paul (johan.paul@d-pointer.com)
 *
 * Podcatcher for N9 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Podcatcher for N9 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Podcatcher for N9.  If not, see <http://www.gnu.org/licenses/>.
 */
import QtQuick 1.1
import com.nokia.symbian 1.1
import com.nokia.extras 1.1
import "constants.js" as Constants

Page {
    id: episodesPage
    anchors.fill: parent
    orientationLock: PageOrientation.LockPortrait

    tools:
        ToolBarLayout {
        ToolButton {
            flat: true
            iconSource: "toolbar-back"
            onClicked: appWindow.pageStack.depth <= 1 ? Qt.quit() : appWindow.pageStack.pop()
        }

        ToolButton {
            flat: true
            iconSource: "toolbar-refresh"
            onClicked: {
                appWindow.refreshEpisodes(channel.channelId)
                refreshingBanner.open()
            }
        }

        ToolButton {
            iconSource: "toolbar-menu"
            onClicked: (myMenu.status == DialogStatus.Closed) ? myMenu.open() : myMenu.close()
        }

    }

    Column {
        id: episodesPageColumn
        anchors.fill: parent

        Rectangle {
            id: mainPageTitle
            width: parent.width
            height: 55

            color: Constants.color_background_page; // "#9501C5"

            Label {
                id: mainPageTitleText
                anchors.left: parent.left
                anchors.leftMargin: 15
                anchors.verticalCenter: parent.verticalCenter
                width:  parent.width - 20

                font.pixelSize: Constants.font_size_px_titles
                text: (channel.title.length > 30 ? channel.title.substr(0, 30).concat("...") : channel.title)
                color:  "white"
            }
        }

        PodcastEpisodesChannelInfo {
            id: episodeData
            width: parent.width
            height: 110
        }

        Rectangle {
            id: highlight1
            width: parent.width
            height: 1
            color: "#CDCECF"
            border.width: 0
        }

        Rectangle {
            id: highlight2
            width: parent.width
            height: 1
            color: "#FFFFFF"
            border.width: 0
        }

        PodcastEpisodesList {
            id: episodesList
            width: parent.width
            height: parent.height - episodeData.height + 20
            channelId: channel.channelId
        }
    }


    EpisodeDescriptionPage {
        id: episodeDescriptionPage
    }

    InfoBanner {
        id: refreshingBanner
        text:  "Refreshing episodes..."
    }

    Menu {
        id: myMenu
        MenuLayout {
            MenuItem {
                text: "Mark all podcasts as played"
                onClicked: {
                    appWindow.allListened(channel.channelId);
                }
            }
            MenuItem {
                text: "Delete all downloaded podcasts"
                onClicked: {
                    queryDeletePodcasts.open();
                }
            }
            MenuItem {
                text: "Remove subscription"
                onClicked: {
                    queryDeleteSubscription.open();
                }
            }
        }
    }

    QueryDialog {
        id: queryDeleteSubscription

        titleText: "Remove " + channel.title + "?"
        message: "Are you sure you want delete the subscription of this channel?\n\n" +
                 "If you proceed all downloaded podcast episodes from this channel will also be deleted.    \n\n";

        acceptButtonText: "Remove"
        rejectButtonText: "Cancel"

        onAccepted: {
            console.log("Go ahead and delete!")
            appWindow.deleteChannel(channel.channelId);
            queryDeleteSubscription.close();
            pageStack.pop(mainPage);
        }

        onRejected: {
            console.log("Cancel");
            queryDeleteSubscription.close();
        }

    }

    QueryDialog {
        id: queryDeletePodcasts

        titleText: "Delete all podcasts?"
        message: "Are you sure you want delete all downloaded podcasts from this subscription?    \n\n"

        acceptButtonText: "Delete"
        rejectButtonText: "Cancel"

        onAccepted: {
            console.log("Go ahead and delete!")
            ui.deletePodcasts(channel.channelId);
            queryDeleteSubscription.close();
        }

        onRejected: {
            console.log("Cancel");
            queryDeletePodcasts.close();
        }

    }

    Connections {
        target: ui
        onShowInfoBanner: {
            console.log("Showing banner: "+text);
            uiInfoBanner.text = text
            uiInfoBanner.show();
        }
    }

    InfoBanner {
        id: uiInfoBanner
    }

}

