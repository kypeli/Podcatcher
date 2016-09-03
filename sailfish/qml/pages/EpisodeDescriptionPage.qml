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

Page {

    id: epDescPage
    //orientationLock: PageOrientation.LockPortrait

    property string episodeDescriptionText
    property string episodePublished
    property string episodeName

    SilicaFlickable{
        anchors.fill: parent

        Column {
            id: episodesPageColumn
            //width:parent.width
            anchors.fill: parent
            spacing: Theme.paddingMedium

            PageHeader{
                id: mainPageTitle
                title: episodeDescriptionPage.episodeName
                wrapMode: Text.WordWrap
                }

            Rectangle {
                id: podcastEpisodeRect
                smooth: true
                color:  "transparent"
                width: parent.width
                height: channelLogo.height + 2*Theme.paddingMedium


                PodcastChannelLogo{
                    id: channelLogo
                    channelLogo: channel.logo
                    anchors.left: podcastEpisodeRect.left
                    anchors.top: podcastEpisodeRect.top
                    anchors.leftMargin: Theme.horizontalPageMargin
                    anchors.topMargin: Theme.paddingMedium
                    width: 130
                    height: 130
                }

                Label {
                    id: channelPublished
                    anchors.left: channelLogo.right
                    anchors.leftMargin: Theme.paddingLarge
                    anchors.bottom: channelLogo.bottom
                    text: qsTr("Published: ") + episodeDescriptionPage.episodePublished
                    font.pixelSize: Theme.fontSizeSmall
                    width: episodeDescriptionPage.width - channelLogo.width - 2*Theme.horizontalPageMargin - Theme.paddingMedium
                }

            }

            Separator{
                width: parent.width
            }

            SilicaFlickable {
                id: episodeDescriptionFlickable
                width: parent.width - 2*Theme.horizontalPageMargin
                height: parent.height - podcastEpisodeRect.height
                contentWidth: episodeDescription.width
                contentHeight: episodeDescription.height + Theme.paddingMedium
                clip: true
                flickableDirection: Flickable.VerticalFlick
                anchors.horizontalCenter: parent.horizontalCenter

                Label {
                    id: episodeDescription
                    wrapMode: Text.WordWrap
                    width: parent.width
                    height: Text.paintedHeight
                    font.pixelSize: Theme.fontSizeSmall
                    anchors.top:  parent.top
                    anchors.topMargin: 10

                    text: episodeDescriptionPage.episodeDescriptionText

                    onLinkActivated: {
                        Qt.openUrlExternally(link);

                    }
                }

            VerticalScrollDecorator{
            }

            }

        }
    }

}
