/**
 * This file is part of Podcatcher for Sailfish OS.
 * Authors: Johan Paul (johan.paul@gmail.com)
 *          Moritz Carmesin (carolus@carmesinus.de)
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
import QtQuick.XmlListModel 2.0

Page {
    id: browsePage
    //orientationLock: PageOrientation.LockPortrait

    function openFile(file) {
        var component = Qt.createComponent(file)

        if (component.status == Component.Ready)
            pageStack.push(component);
        else
            console.log("Error loading component:", component.errorString());
    }

    SilicaFlickable{
        anchors.fill: parent
        PullDownMenu {



            MenuItem {
                text: "Import podcasts from gPodder"
                onClicked: {
                    //pageStack.push(Qt.resolvedUrl("ImportFromGPodder.qml"))
                    //importFromGPodderSheet.open();
                    pageStack.push(importFromGPodderComponent)

                }
            }

            MenuItem {
                text: "Add URL manually"
                onClicked: {
                    pageStack.push(addNewPodcastComponent)
                }
            }

            MenuItem {
                text: "Search"
                onClicked: {
                    myMenu.close();
                    openFile("SearchPodcasts.qml");
                }

            }

        }

        BusyIndicator {
            id: loadingIndicator
            anchors.centerIn: parent
            visible: (popularPodcastsModel.status == XmlListModel.Loading );
            running: (popularPodcastsModel.status == XmlListModel.Loading );
            opacity: 1.0
            size: BusyIndicatorSize.Large
        }

        Item {
            anchors.centerIn: parent;
            width: parent.width
            visible: (popularPodcastsModel.status == XmlListModel.Error);
            Label {
                anchors.centerIn: parent
                text: "I am sorry!<BR><BR>"
                font.pixelSize: 25
                font.bold: true
                width: parent.width
                elide: Text.ElideRight
            }

            Label {
                anchors.centerIn: parent
                text: "Cannot get popular podcasts at this time."
                width: parent.width
                elide: Text.ElideRight
            }
        }

        PageHeader{
            id: queryPageTitle
            title: "Popular podcasts"
        }


        SilicaGridView {
            id: popularPodcastsGrid
            model: popularPodcastsModel
            cellWidth: (Screen.width-2*Theme.horizontalPageMargin)/3;
            cellHeight: cellWidth+3*Theme.paddingSmall + 2*Theme.fontSizeTiny + Theme.fontSizeHuge
            width: (cellWidth * 3) + 1
            height: parent.height - queryPageTitle.height - Theme.paddingLarge
            anchors.top: queryPageTitle.bottom
            anchors.horizontalCenter: parent.horizontalCenter
            clip: true

            VerticalScrollDecorator{}

            delegate:
                Item {
                id: popularItem
                state: "notLoaded"

                Item {
                    id: loadedItem
                    width: popularPodcastsGrid.cellWidth
                    height: popularPodcastsGrid.cellHeight

                    Rectangle {
                        id: popularItemId
                        border.width: 1
                        border.color: "black"
                        color: "black"
                        smooth: true
                        width: parent.width-Theme.paddingLarge
                        height: parent.height - subscribeButton.height-3*Theme.paddingSmall
                        anchors.centerIn: loadedItem
                        opacity: 0.2

                        BusyIndicator {
                            id: loadingIndicatorEpisode
                            anchors.centerIn: parent
                            visible: (popularItem.state == "notLoaded");
                            running: (popularItem.state == "notLoaded");
                            opacity: 1.0
                        }

                        Image {
                            id: popularLogoId;
                            source: logo;
                            anchors.top: popularItemId.top
                            anchors.left: popularItemId.left
                            anchors.margins: 1
                            width: parent.width-1
                            height: parent.width-1
                            visible: (popularItem.state != "notLoaded");
                        }

                        Label {
                            id: browseTitle
                            text: title.substring(0, 25);
                            anchors.top: popularLogoId.bottom;
                            anchors.horizontalCenter: parent.horizontalCenter
                            anchors.topMargin: 5

                            font.pixelSize: Theme.fontSizeTiny
                            color: "black"
                            width: popularItemId.width - 2*Theme.paddingSmall
                            wrapMode: Text.WordWrap
                        }

                    }
                    Button {
                        id: subscribeButton
                        text: "Subscribe"
                        anchors.horizontalCenter: popularItemId.horizontalCenter
                        anchors.top: popularItemId.bottom
                        anchors.topMargin: Theme.paddingSmall
                        width: popularItemId.width


                        opacity: 0.0

                        onClicked: {
                            console.log("Subscribe to podcast with url: " + url)
                            mainPage.addPodcast(url, logoUrl);
                            pageStack.pop();
                        }
                    }
                }

                states: [
                    State {
                        name: 'loaded'; when: popularLogoId.status == Image.Ready
                        PropertyChanges {
                            target: popularItemId
                            color: "white"
                            opacity: 1.0
                        }
                        PropertyChanges {
                            target: subscribeButton
                            opacity: 1.0
                        }
                    }
                ]


                transitions: Transition {
                    ParallelAnimation {
                        PropertyAnimation { property: "width"; duration: 500 }
                        PropertyAnimation { property: "height"; duration: 500 }
                        PropertyAnimation { property: "opacity"; duration: 500 }
                        PropertyAnimation { property: "color"; duration: 500 }
                    }
                }
            }

        }

        XmlListModel {
            id: popularPodcastsModel
            source: "http://gpodder.net/toplist/15.xml"
            query: "/podcasts/podcast"

            XmlRole { name: "logo"; query: "logo_url/string()" }
            XmlRole { name: "title"; query: "title/string()" }
            XmlRole { name: "url"; query: "url/string()" }
            XmlRole { name: "logoUrl"; query: "logo_url/string()" }

            onStatusChanged: {
                console.log("XML loading status changed: " + status);
                console.log("XML error: " + popularPodcastsModel.errorString());
            }
        }

        Component{
            id: importFromGPodderComponent
            ImportFromGPodder{

            }
        }

        Component{
            id: addNewPodcastComponent
            Dialog {
                id: addNewPodcastSheet


               Column {
                    id: col
                    anchors.fill: parent
                    spacing: Theme.paddingMedium

                    DialogHeader{
                        title: "Add new podcast"
                        acceptText: "Add"
                    }

                    TextField {
                        id: podcastUrl
                        placeholderText: "Podcast RSS URL"
                        width: parent.width

                        Keys.onReturnPressed: {
                            parent.focus = true;
                        }
                    }
                }

                onStatusChanged: {
                    if (status == DialogStatus.Opening) {
                        podcastUrl.text = ""
                    }

                }

                onAccepted: {
                    mainPage.addPodcast(podcastUrl.text, "");
                    pageStack.pop(mainPage);
                }
            }
        }

    }

}

