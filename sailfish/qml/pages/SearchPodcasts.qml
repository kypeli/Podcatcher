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
    SilicaFlickable{
        anchors.fill: parent
        //orientationLock: PageOrientation.LockPortrait


        BusyIndicator  {
            id: loadingIndicator
            anchors.centerIn: parent
            visible: (searchPodcastsModel.status == XmlListModel.Loading );
            running: (searchPodcastsModel.status == XmlListModel.Loading );
            size: BusyIndicatorSize.Large
        }

        Item {
            anchors.centerIn: parent;
            visible: (searchPodcastsModel.status != XmlListModel.Loading &&
                      searchWord.focus == false &
                      searchPodcastsList.count == 0);
            Label {
                anchors.centerIn: parent
                text: qsTr("No podcasts found.")
                font.pixelSize: 35
                font.bold: true
            }
        }

        Column {
            id: searchPageColumn
            anchors.fill: parent
            spacing: Theme.paddingSmall
                     anchors{
                         //leftMargin: Theme.horizontalPageMargin
                         rightMargin: Theme.horizontalPageMargin
                         bottomMargin: Theme.paddingMedium
                     }

            PageHeader{
                   id: searchPageTitle
                   title: qsTr("Search podcasts")
            }

            SearchField {
                id: searchWord
                placeholderText: qsTr("Keyword")

                width: parent.width

                Keys.onReturnPressed: {
                    parent.focus = true;
                    searchPodcastsModel.source = "http://gpodder.net/search.xml?q=\"" + searchWord.text+"\"";

                }
            }

            SilicaListView {
                id: searchPodcastsList
                model: searchPodcastsModel
                width: parent.width
                height: parent.height - searchWord.height - searchPageTitle.height - 2*Theme.paddingSmall - Theme.paddingMedium
                anchors.horizontalCenter: parent.horizontalCenter
                clip: true
                visible: false
                spacing: Theme.paddingMedium

                delegate:
                    ListItem {
                    id: searchItem
                    contentHeight: Theme.itemSizeLarge
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

                    BusyIndicator  {
                        id: loadingIndicatorEpisode
                        anchors.left: parent.left
                        anchors.leftMargin: Theme.paddingMedium
                        anchors.verticalCenter: parent.verticalCenter
                        visible: (channelLogo.status != Image.Ready);
                        running: (channelLogo.status != Image.Ready);
                    }

                    Item {
                        id: channelNameItem
                        anchors.left: channelLogo.right
                        anchors.leftMargin: Theme.paddingMedium;
                        width: searchItem.width - channelLogo.width - Theme.paddingMedium
                        height: parent.height

                        Column {
                            width: parent.width
                            spacing: Theme.paddingSmall

                            Row {
                                width: parent.width

                                Label {
                                    id: channelName;
                                    text: title
                                    //truncationMode: TruncationMode.Fade
                                    wrapMode: Text.Wrap
                                    verticalAlignment: Text.AlignVCenter
                                    font.pixelSize: Theme.fontSizeMedium
                                    color: channelNameItem.highlighted ? Theme.highlightColor : Theme.primaryColor
                                    width: parent.width - subscribeButton.width
                                    height: channelNameItem.height - channelUrl.height - Theme.paddingSmall


                                }

                                IconButton{

                                    id: subscribeButton
                                    anchors.leftMargin: Theme.paddingMedium
                                    anchors.verticalCenter: parent.verticalCenter

                                    icon.source: "image://theme/icon-m-add"

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
                                font.pixelSize: Theme.fontSizeTiny
                                color: channelNameItem.highlighted ? Theme.highlightColor : Theme.secondaryColor
                                width: parent.width
                                truncationMode: TruncationMode.Elide
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

                VerticalScrollDecorator{}
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
}
