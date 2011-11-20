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

    tools:
        ToolBarLayout {
        ToolIcon { iconId: "toolbar-back"; onClicked: { pageStack.pop(); } }
    }

    Image  {
        id: loadingIndicator
        anchors.centerIn: parent
        source: "qrc:///gfx/loading.gif"
        visible: (searchPodcastsModel.status == XmlListModel.Loading );
        NumberAnimation on rotation {
            from: 0; to: 360;
            running: searchPodcastsModel.status == XmlListModel.Loading;
            loops: Animation.Infinite; duration: 900
        }
    }

    Item {
        anchors.centerIn: parent;
        visible: (searchPodcastsModel.status != XmlListModel.Loading &&
                  searchWord.focus == false &
                  searchPodcastsList.count == 0);
        Label {
            anchors.centerIn: parent
            text: "No podcasts found."
            font.pixelSize: 35
            font.bold: true
        }
    }

    Column {
        id: searchPageColumn
        anchors.fill: parent
        spacing: 5

        Rectangle {
            id: searchPageTitle
            width: parent.width
            height: 72

            color: "#9501C5"

            Label {
                id: searchPageTitleText
                anchors.left: parent.left
                anchors.leftMargin: 15
                anchors.verticalCenter: parent.verticalCenter
                width:  parent.width

                font.pixelSize: 35
                text: "Search podcasts"
                color:  "white"
            }
        }

        TextField {
            id: searchWord
            placeholderText: "Keyword"

            width: parent.width

            Keys.onReturnPressed: {
                parent.focus = true;
                searchPodcastsModel.source = "http://gpodder.net/search.xml?q=\"" + searchWord.text+"\"";

            }
        }

        ListView {
            id: searchPodcastsList
            model: searchPodcastsModel
            width: parent.width
            height: parent.height - searchWord.height - searchPageTitle.height
            anchors.horizontalCenter: parent.horizontalCenter
            clip: true
            visible: false
            spacing: 10

            delegate:
                Item {
                id: searchItem
                height: 90
                width: parent.width

                Image {
                    id: channelLogo;
                    source: logo
                    anchors.left: parent.left
                    anchors.verticalCenter: parent.verticalCenter
                    width: parent.height;
                    height: parent.height;
                    visible: status == Image.Ready
                }

                Image  {
                    id: loadingIndicatorEpisode
                    anchors.left: parent.left
                    anchors.leftMargin: 10
                    anchors.verticalCenter: parent.verticalCenter
                    source: "qrc:///gfx/loading.gif"
                    visible: (channelLogo.status != Image.Ready);
                    NumberAnimation on rotation {
                        from: 0; to: 360;
                        running: channelLogo.state != Image.Ready
                        loops: Animation.Infinite; duration: 900
                    }
                }

                Item {
                    id: channelNameItem
                    anchors.left: channelLogo.right
                    anchors.leftMargin: 10;
                    width: searchItem.width - channelLogo.width - 10
                    height: parent.height

                    Column {
                        width: parent.width
                        spacing: 5

                        Row {
                            width: parent.width

                            Label {
                                id: channelName;
                                text: title
                                wrapMode: Text.WrapAtWordBoundaryOrAnywhere
                                font.pixelSize: 25
                                width: parent.width - subscribeButton.width
                            }

                            Button {
                                id: subscribeButton
                                anchors.leftMargin: 10
                                anchors.verticalCenter: parent.verticalCenter
                                text: "Subscribe"
                                width: 150

                                onClicked: {
                                    console.log("Subscribe to podcast with url: " + url)
                                    mainPage.addPodcast(url, logoUrl);
                                    pageStack.pop(mainPage);
                                }
                            }
                        }

                        Label {
                            id: channelUrl;
                            text: url
                            font.pixelSize: 17
                            color: "grey"
                            width: parent.width
                            elide: Text.ElideRight
                        }

                    }
                }
            }

            XmlListModel {
                id: searchPodcastsModel
                query: "/podcasts/podcast"

                XmlRole { name: "logo"; query: "logo_url/string()" }
                XmlRole { name: "title"; query: "title/string()" }
                XmlRole { name: "url"; query: "url/string()" }
                XmlRole { name: "logoUrl"; query: "logo_url/string()" }

                onStatusChanged: {
                    console.log("XML loading status changed: " + status);
                    console.log("XML error: " + searchPodcastsModel.errorString());
                }
            }

            ScrollBar{
                scrollArea: parent
                anchors.right: parent.right
                anchors.top: parent.top
                anchors.bottom: parent.bottom
                anchors.topMargin: 5
                anchors.bottomMargin: 5
                anchors.rightMargin: 5
            }
        }
    }

    Component.onCompleted: {
        searchWord.forceActiveFocus()
    }

    states: [
        State {
            name: 'searchLoaded'; when: searchPodcastsModel.status == XmlListModel.Ready
            PropertyChanges {
                target: searchPodcastsList
                visible: true
            }
        }
    ]

}
