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
    id: mainPage
    orientationLock: PageOrientation.LockPortrait
    anchors.fill: parent

    property variant audioStreamer : audioStreamerUi
    property variant infoBanner : uiInfoBanner

    property string contextMenuChannelName;
    property int contextUnplayedEpisodes;
    property variant searchPage;
    property list<Label> contentList;

    state: ""

    function openFile(file) {
        var component = Qt.createComponent(file)
        if (component.status == Component.Ready)
            appWindow.pageStack.push(component);
        else
            console.log("Error loading component:", component.errorString());
    }

    function addPodcast(url, logo) {
//        fetchingChannelBanner.show();
        ui.addPodcast(url, logo);
    }

    tools: mainToolbar

    ToolBarLayout {
        visible: true
        id: mainToolbar

        ToolButton {
            flat: true
            iconSource: "toolbar-back"
            onClicked: Qt.quit()
        }


        ToolButton {

            flat: true
            iconSource: "toolbar-add"
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
        }

        ToolButton {
            iconSource: "toolbar-menu"
            onClicked: myMenu.open()
        }

    }

    Menu {
        id: myMenu

        content:
            MenuLayout {
                MenuItem {
                    text: "Refresh all subscriptions"
                    onClicked: {
                        ui.refreshChannels();
                        uiInfoBanner.text = "Refreshing all subscriptions...";
                        uiInfoBanner.open();

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
            height: 55

            color: "#9501C5"

            Label {
                id: mainPageTitleText
                anchors.left: parent.left
                anchors.leftMargin: 15
                anchors.verticalCenter: parent.verticalCenter
                width:  parent.width - 100

                font.pixelSize: Constants.font_size_px_titles
                text: "Podmaster"
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
        Rectangle {
            id: podcastChannelsInfoRect

            smooth: true
            color: Constants.color_background_listitem
            width: parent.width
//            height:  parent.height - mainPageTitle.height
            height:  parent.height

            Column {
                width: parent.width
                height: parent.height

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
                        height: 80
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
                            height: 80
                            color: "transparent"
                            anchors.left: channelLogoId.right;
                        }

                        Label {
                            id: channelName;
                            anchors.left: channelLogoId.right
                            anchors.leftMargin: 10;
                            anchors.verticalCenter: parent.verticalCenter
                            text: title
                            width: listItem.width - channelLogoId.width - drilldownArrow.width - downloadingIcon.width - 10
                            wrapMode: Text.WordWrap
                            color: "black";
                        }

                        Image {
                            id: downloadingIcon
                            source: "qrc:/gfx/download.png"
                            anchors.right: drilldownArrow.left
                            anchors.rightMargin: 5
                            y: parent.height/2 - height/2  // We can't use anchors (animation would not work) so center vertically like this.
                            visible: isDownloading
                            width: 24
                            height: 24
                        }

                        /*                CountBubble{
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
    */
                        Image {
                            id: drilldownArrow
                            source: "qrc:/gfx/br_next.png"
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

                    /*            onCountChanged: {
                    console.log("And hiding the banner...");
                    fetchingChannelBanner.hide();
                }
    */
                }

                AudioStreamer {
                    id: audioStreamerUi
                    width: parent.width
                    height: 0
                    visible: false
                }
            }

        }

    }

    InfoBanner {
        id: uiInfoBanner
    }

    InfoBanner {
        id: fetchingChannelBanner
        text: "Fetching channel information..."
    }

    Connections {
        target: ui
        onShowInfoBanner: {
            uiInfoBanner.text = text
            uiInfoBanner.open();
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

    Menu { id: contextMenu;

        content: MenuLayout {
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


    QueryDialog {
        id: queryDelete

        titleText: "Remove " +mainPage.contextMenuChannelName + "?"
        message: "Are you sure you want delete this subscription?\n\n" +
        "If you proceed all downloaded podcast episodes from this channel will also be deleted.    \n\n";

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
        visualParent: mainPage

        titleText: "Symbian Podcatcher BETA"
        message: "I am sorry! You have reached the limit of subscriptions for this BETA version.    \n\n"

        acceptButtonText: "Continue"

        onAccepted: {
            liteDialog.close();
        }
    }

}
