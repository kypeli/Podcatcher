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
import com.nokia.meego 1.0
import com.nokia.extras 1.0

Page {
    id: mainPage
    orientationLock: PageOrientation.LockPortrait

    property string contextMenuChannelName;
    property int contextUnplayedEpisodes;

    property variant audioStreamer : audioStreamerUi
    property variant infoBanner : uiInfoBanner

    state: ""

    function openFile(file) {
        var component = Qt.createComponent(file)

        if (component.status == Component.Ready)
            pageStack.push(component);
        else
            console.log("Error loading component:", component.errorString());
    }

    function addPodcast(url, logo) {
//        fetchingChannelBanner.show();
        ui.addPodcast(url, logo);        
    }

    function gpodderImport(username, password) {
        ui.importFromGPodder(username, password);
    }

    tools:
        ToolBarLayout {
        id: mainToolbar

        ToolIcon {
            iconId: "toolbar-add"
            onClicked: {
                if (ui.isLiteVersion()) {
                    console.log("Lite version");
                    if (podcastChannelsList.count >= 3) {
                        liteDialog.open()
                    } else {
                        openFile("BrowsePodcasts.qml");
                    }
                } else {
                    console.log("Pro version");
                    openFile("BrowsePodcasts.qml");
                }


            }
            anchors.horizontalCenter: parent.horizontalCenter
        }
        ToolIcon {
            iconId: "toolbar-view-menu"
            onClicked: (myMenu.status == DialogStatus.Closed) ? myMenu.open() : myMenu.close()
            anchors.right: parent==undefined ? undefined : parent.right
        }

    }

    EmptyChannelPage {
        id: emptyText
        visible: (podcastChannelsList.count == 0);
    }

    Column {
        id: mainPageColumn
        anchors.fill: parent

        Rectangle {
            id: mainPageTitle
            width: parent.width
            height: 72

            color: "#9501C5"

            Label {
                id: mainPageTitleText
                anchors.left: parent.left
                anchors.leftMargin: 15
                anchors.verticalCenter: parent.verticalCenter
                width:  parent.width - 100

                font.pixelSize: 35
                text: "Podcatcher"
                color:  "white"
            }

            Image  {
                id: loadingIndicator
                anchors.right: parent.right
                anchors.rightMargin: 10
                anchors.verticalCenter: parent.verticalCenter
                source: "qrc:///gfx/refresh.png"
                visible: (mainPage.state == "downloading");
                NumberAnimation on rotation {
                    from: 360; to: 0;
                    running: mainPage.state == "downloading"
                    loops: Animation.Infinite; duration: 1400
                }
                smooth: true
            }
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

        ListView {
            id: podcastChannelsList
            model: channelsModel
            visible: (podcastChannelsList.count > 0);
            spacing: 1
            width: parent.width
            height: parent.height - mainPageTitle.height - audioStreamerUi.height
            clip: true

            delegate: Item {
                id: listItem
                height: 90
                width: parent.width

                PodcastChannelLogo {
                    id: channelLogoId;
                    channelLogo: logo
                    anchors.left: parent.left
                    anchors.verticalCenter: parent.verticalCenter
                    width: parent.height;
                    height: parent.height;
                }

                Rectangle {
                    id: listItemBackground
                    width: parent.width
                    height: 90
                    color: "transparent"
                    anchors.left: channelLogoId.right;
                }

                Label {
                    id: channelName;
                    anchors.left: channelLogoId.right
                    anchors.leftMargin: 10;
                    anchors.verticalCenter: parent.verticalCenter
                    text: title
                    width: 300
                    wrapMode: Text.WordWrap
                }

                CountBubble{
                    id: unplayedNumber
                    value: unplayedEpisodes
                    largeSized: true
                    anchors.right: drilldownArrow.left
                    anchors.margins: 10
                    visible: ((value > 0) || isDownloading)
                    y: parent.height/2 - height/2  // We can't use anchors (animation would not work) so center vertically like this.

                    SequentialAnimation on y {
                        running: isDownloading
                        loops: Animation.Infinite
                        PropertyAnimation { to: unplayedNumber.y + unplayedNumber.height / 3; duration: 500; easing.type: Easing.InOutQuad }
                        PropertyAnimation { to: unplayedNumber.y - unplayedNumber.height / 3; duration: 500; easing.type: Easing.InOutQuad }

                        onRunningChanged: {
                            if (isDownloading === false) {
                                unplayedNumber.y = unplayedNumber.parent.height/2 - unplayedNumber.height/2;
                            }
                        }
                    }
               }

               Image {
                   id: drilldownArrow
                    source: "image://theme/icon-m-common-drilldown-arrow" + (theme.inverted ? "-inverse" : "")
                    anchors.right: parent.right;

                    anchors.verticalCenter: parent.verticalCenter
                }

                MouseArea {
                    id: mouseArea
                    anchors.fill: parent
                    onClicked: {
                        appWindow.showChannel(channelId);
                        openFile("PodcastEpisodes.qml");
                    }

                    onPressAndHold:{
                        console.log("Press and hold, index: " + index);
                        podcastChannelsList.currentIndex = channelId;
                        mainPage.contextMenuChannelName = title;
                        mainPage.contextUnplayedEpisodes = unplayedEpisodes;
                        contextMenu.open();
                    }

                }
            }

            onCountChanged: {
                console.log("And hiding the banner...");
                fetchingChannelBanner.hide();
            }
        }

        AudioStreamer {
            id: audioStreamerUi
            width: parent.width
            height: 0
            visible: false
        }
    }




    InfoBanner {
        id: uiInfoBanner
        topMargin: 10
        leftMargin: 10
    }

    InfoBanner {
        id: fetchingChannelBanner
        timerEnabled: false
        text: "Fetching channel information..."
        topMargin: 10
        leftMargin: 10
    }

    Connections {
        target: ui
        onShowInfoBanner: {
            uiInfoBanner.text = text
            uiInfoBanner.show();
        }

        onDownloadingPodcasts: {
            console.log("Downloading changed:" + downloading)
            if (downloading) {
                mainPage.state = "downloading"
            } else {
                mainPage.state = ""
            }
        }
    }

    Connections {
        target: audioStreamer

        onPlayStream: {
            console.log("Showing audio streamer.");
            audioStreamerUi.height = 130
            audioStreamerUi.visible = true
        }
    }

    Component.onCompleted: {
        console.log("Is downloading: " + ui.isDownloading());
        if (ui.isDownloading()) {
            mainPage.state = "downloading";
        }
        else {
            mainPage.state = "";
        }
    }

    Menu { id: contextMenu; visualParent: pageStack;
        MenuLayout {
            MenuItem {
                text: "Mark all podcasts as played"
                visible: mainPage.contextUnplayedEpisodes > 0
                onClicked: {
                    appWindow.allListened(podcastChannelsList.currentIndex);
                }
            }
            MenuItem {
                text: "Remove subscription";
                onClicked: {
                    queryDelete.open();
                }
            }
         }
    }

    Menu {
        id: myMenu
        visualParent: pageStack
        MenuLayout {
            MenuItem {
                text: "Refresh all subscriptions"
                onClicked: {
                    ui.refreshChannels();
                    uiInfoBanner.text = "Refreshing all subscriptions...";
                    uiInfoBanner.show();
                }
            }
            MenuItem {
                text: "About"
                onClicked: {
                    openFile("About.qml");
                }
            }
        }
    }

    QueryDialog {
        id: queryDelete

        titleText: "Remove " +mainPage.contextMenuChannelName + "?"
        message: "Are you sure you want delete this subscription?\n\n" +
        "If you proceed all downloaded podcast episodes from this channel will also be deleted.";

        acceptButtonText: "Remove"
        rejectButtonText: "Cancel"

        onAccepted: {
            console.log("Go ahead and delete!")
            appWindow.deleteChannel(podcastChannelsList.currentIndex);
            queryDelete.close();
        }

        onRejected: {
            console.log("Cancel");
            queryDelete.close();
        }

    }

    QueryDialog {
        id: liteDialog

        titleText: "Podcatcher Lite"
        message: "I am sorry!\n\n" +
        "You have reached the limit of subscriptions for this free version of Podcatcher.\n\nPlease purchase the full version of Podcatcher from Nokia Store to get unlimited number of subscriptions."

        acceptButtonText: "Continue"

        onAccepted: {
            liteDialog.close();
        }
    }

}
