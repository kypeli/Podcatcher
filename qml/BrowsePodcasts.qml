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
    id: browsePage
    anchors.fill: parent
    orientationLock: PageOrientation.LockPortrait

    function openFile(file) {
        var component = Qt.createComponent(file)

        if (component.status == Component.Ready)
            pageStack.push(component);
        else
            console.log("Error loading component:", component.errorString());
    }

    tools:
        ToolBarLayout {
        ToolIcon { iconId: "toolbar-back"; onClicked: { myMenu.close(); pageStack.pop(); } }
        ToolIcon {
            iconId: "toolbar-search"
            onClicked: {
               myMenu.close();
               openFile("SearchPodcasts.qml");
            }
            anchors.horizontalCenter: parent.horizontalCenter
        }
        ToolIcon {
            iconId: "toolbar-view-menu"
            onClicked: (myMenu.status == DialogStatus.Closed) ? myMenu.open() : myMenu.close()
            anchors.right: parent==undefined ? undefined : parent.right
        }

    }

    Image  {
        id: loadingIndicator
        anchors.centerIn: parent
        source: "qrc:///gfx/loading.gif"
        visible: (popularPodcastsModel.status == XmlListModel.Loading );
        NumberAnimation on rotation {
            from: 0; to: 360;
            running: popularPodcastsModel.status == XmlListModel.Loading;
            loops: Animation.Infinite; duration: 900
        }
    }

    Item {
        anchors.centerIn: parent;
        visible: (popularPodcastsModel.status == XmlListModel.Error);
        Label {
            anchors.centerIn: parent
            text: "I am sorry!<BR><BR>"
            font.pixelSize: 35
            font.bold: true
        }

        Label {
            anchors.centerIn: parent
            text: "Seems gPodder.net is unavailable at the moment."
        }
    }

    Column {
        id: queryPageColumn
        anchors.fill: parent

        Rectangle {
            id: queryPageTitle
            width: parent.width
            height: 72

            color: "#9501C5"

            Label {
                id: queryPageTitleText
                anchors.left: parent.left
                anchors.leftMargin: 15
                anchors.verticalCenter: parent.verticalCenter
                width:  parent.width - 100

                font.pixelSize: 35
                text: "Popular podcasts"
                color:  "white"
            }
        }


        GridView {
            id: popularPodcastsGrid
            model: popularPodcastsModel
            cellWidth: 150; cellHeight: 250
            width: (cellWidth * 3) + 1
            height: parent.height - queryPageTitle.height
            anchors.horizontalCenter: parent.horizontalCenter
            clip: true

            delegate:
                Item {
                id: popularItem
                state: "notLoaded"

                Item {
                    id: loadedItem
                    width: 130
                    height: 220

                    Rectangle {
                        id: popularItemId
                        border.width: 1
                        border.color: "black"
                        color: "black"
                        smooth: true
                        width: 130
                        height: parent.height - 50
                        anchors.centerIn: loadedItem
                        opacity: 0.2

                        Image  {
                            id: loadingIndicatorEpisode
                            anchors.centerIn: parent
                            source: "qrc:///gfx/loading.gif"
                            visible: (popularItem.state == "notLoaded");
                            opacity: 1.0
                            NumberAnimation on rotation {
                                from: 0; to: 360;
                                running: popularItem.state == "notLoaded"
                                loops: Animation.Infinite; duration: 900
                            }
                        }

                        Image {
                            id: popularLogoId;
                            source: logo;
                            anchors.top: popularItemId.top
                            anchors.left: popularItemId.left
                            anchors.margins: 1
                            width: 129
                            height: 129
                            visible: (popularItem.state != "notLoaded");
                        }

                        Label {
                            id: browseTitle
                            text: title.substring(0, 25);
                            anchors.top: popularLogoId.bottom;
                            anchors.horizontalCenter: parent.horizontalCenter
                            anchors.topMargin: 5

                            font.pixelSize: 16
                            width: popularItemId.width - 20
                            wrapMode: Text.WordWrap
                        }

                    }
                    Button {
                        id: subscribeButton
                        text: "Subscribe"
                        anchors.horizontalCenter: popularItemId.horizontalCenter
                        anchors.top: popularItemId.bottom
                        anchors.topMargin: 10
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

    Sheet {
        id: addNewPodcastSheet
        acceptButtonText: "Add"
        rejectButtonText: "Cancel"

        content:
            Column {
            id: col
            anchors.top: parent.top
            spacing: 10

            Label {
                id: textLabel
                anchors.topMargin: 5
                anchors.left: col.left
                anchors.leftMargin:  10
                text: "Add new podcast:"
            }
            TextField {
                id: podcastUrl
                anchors.left:  textLabel.left;
                placeholderText: "Podcast RSS URL"
                width: 420

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
            pageStack.pop();
        }

    }

    Menu {
        id: myMenu
        visualParent: pageStack
        MenuLayout {
            MenuItem {
                text: "Add URL manually"
                onClicked: {
                    addNewPodcastSheet.open();
                }
            }
        }
    }

}

