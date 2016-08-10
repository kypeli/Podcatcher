/**
 * This file is part of Podcatcher for N9.
 * Author: Johan Paul (johan.paul@gmail.com)
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

import QtQuick 2.0
import Sailfish.Silica 1.0


Page {
    id: mainPage
    //orientationLock: PageOrientation.LockPortrait

    property string contextMenuChannelName;
    property int contextUnplayedEpisodes;

    property variant audioStreamer : audioStreamerUi
    //property variant infoBanner : uiInfoBanner

    state: ""

    function openFile(file) {
        var component = Qt.createComponent(Qt.resolvedUrl(file))

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

    SilicaFlickable{

        //contentHeight: mainPageColumn.height
        anchors.fill: parent


        EmptyChannelPage {
            id: emptyText
            visible: (podcastChannelsList.count == 0);
        }

        Column {
            id: mainPageColumn
            anchors.fill: parent
            PageHeader {
                id: mainPageTitle

                title: "Podcatcher"


                Image  {
                    id: loadingIndicator
                    parent: mainPageTitle.extraContent
                    anchors.left: parent.left
                    anchors.leftMargin: Theme.horizontalPageMargin
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


            SilicaListView {
                id: podcastChannelsList
                model: channelsModel
                visible: (podcastChannelsList.count > 0);
                spacing: 1
                width: parent.width
                height: parent.height - mainPageTitle.height/* - audioStreamerUi.height*/ - Theme.paddingMedium
                clip: true

                delegate: ListItem {
                    id: listItem
                    contentHeight: Theme.itemSizeLarge

                    menu: ContextMenu {
                        id: contextMenu;
                        MenuItem {
                            text: "Mark all podcasts as played"
                            visible: unplayedEpisodes > 0
                            onClicked: {
                                appWindow.allListened(model.channelId);
                            }
                        }
                        MenuItem {
                            text: "Remove subscription";
                            onClicked: {
                                channelRemorse.execute(listItem,"Removing",
                                                       function(){
                                                           console.log("Going to delete Channel "+model.channelId+"!");
                                                           appWindow.deleteChannel(model.channelId);
                                                       });
                            }
                        }
                    }

                    RemorseItem{
                        id: channelRemorse
                    }

                    PodcastChannelLogo {
                        id: channelLogoId;
                        channelLogo: logo
                        anchors.left: parent.left
                        anchors.verticalCenter: parent.verticalCenter
                        width: parent.height;
                        height: parent.height;
                    }

                    Label {
                        id: channelName;
                        anchors.left: channelLogoId.right
                        anchors.leftMargin: Theme.paddingMedium;
                        anchors.verticalCenter: parent.verticalCenter
                        text: title
                        width: parent.width - Theme.horizontalPageMargin - Theme.paddingMedium - unplayedNumber.width
                        wrapMode: Text.WordWrap
                        color: listItem.highlighted ? Theme.highlightColor : Theme.primaryColor
                    }

                    Label{
                        id: unplayedNumber
                        text: unplayedEpisodes
                        anchors.right: parent.right
                        anchors.rightMargin: Theme.horizontalPageMargin
                        //anchors.verticalCenter: parent.verticalCenter

                        y: parent.height/2 - height/2
                        height: Text.paintedHeight

                        font.pixelSize: Theme.fontSizeSmall
                        visible: ((unplayedEpisodes > 0) || model.isDownloading);
                        color: (model.isDownloading)?Theme.secondaryHighlightColor:Theme.secondaryColor;

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


                    onClicked: {
                        appWindow.showChannel(channelId);
                        openFile("PodcastEpisodes.qml");
                    }

                }

                onCountChanged: {
                    console.log("And hiding the banner...");
                    //fetchingChannelBanner.hide();
                }

                VerticalScrollDecorator{}
            }


        }



        /*
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
*/
        Connections {
            /*
        target: ui
        onShowInfoBanner: {
            uiInfoBanner.text = text
            uiInfoBanner.show();
        }
        */

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
                audioStreamerUi.show();
            }
        }

        Component.onCompleted: {
            console.log("Is downloading: " + ui.isDownloading);
            if (ui.isDownloading) {
                mainPage.state = "downloading";
            }
            else {
                mainPage.state = "";
            }
        }



        PullDownMenu {
            id: myMenu

            MenuItem {
                text: "About"
                onClicked: {
                    openFile("About.qml");
                }
            }

            MenuItem {
                text: "Refresh all subscriptions"
                onClicked: {
                    ui.refreshChannels();
                    uiInfoBanner.text = "Refreshing all subscriptions...";
                    uiInfoBanner.show();
                }
            }

            MenuItem {
                text: qsTr("Add Podcast")
                onClicked: {
                    openFile("BrowsePodcasts.qml");
                }
            }

        }

    }


    AudioStreamer {
        id: audioStreamerUi
    }




}
