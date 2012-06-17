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
    anchors.fill: parent
    orientationLock: PageOrientation.LockPortrait

    tools:
        ToolBarLayout {
        ToolButton {
            flat: true
            iconSource: "toolbar-back"
            onClicked: appWindow.pageStack.depth <= 1 ? Qt.quit() : appWindow.pageStack.pop()
        }

    }

    property string episodeDescriptionText
    property string episodePublished
    property string episodeName

    Column {
        id: episodesPageColumn
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
                width:  parent.width - 20

                font.pixelSize: Constants.font_size_px_titles
                text: (episodeDescriptionPage.episodeName.length > 45 ? episodeDescriptionPage.episodeName.substr(0, 45).concat("...") : episodeDescriptionPage.episodeName)
                color:  "white"
                wrapMode: Text.WordWrap
            }
        }

        Rectangle {
            id: podcastEpisodeRect
            smooth: true
//            color:  "#e4e5e6"
            width: parent.width
            height: channelLogo.height + 20
            color: "transparent"


            PodcastChannelLogo{
                id: channelLogo
                channelLogo: channel.logo
                anchors.left: podcastEpisodeRect.left
                anchors.top: podcastEpisodeRect.top
                anchors.leftMargin: 7
                anchors.topMargin: 7
                width: 90
                height: 90
            }

            Label {
                id: channelPublished
                anchors.left: channelLogo.right
                anchors.leftMargin: 20
                anchors.bottom: channelLogo.bottom
                text: "Published: " + episodeDescriptionPage.episodePublished
                width: episodeDescriptionPage.width - channelLogo.width - 30
            }

        }

        Flickable {
            id: episodeDescriptionFlickable
            width: parent.width - 30
            height: parent.height - podcastEpisodeRect.height - 80
            contentWidth: episodeDescription.width
            contentHeight: episodeDescription.height + 10
            clip: true
            flickableDirection: Flickable.VerticalFlick
            anchors.horizontalCenter: parent.horizontalCenter

            Label {
                id: episodeDescription
                wrapMode: Text.WordWrap
                width: 330
                height: Text.paintedHeight
                font.pixelSize: 21
                anchors.top:  parent.top
                anchors.topMargin: 10

                text: episodeDescriptionPage.episodeDescriptionText
            }
        }

        ScrollDecorator {
            flickableItem: episodeDescriptionFlickable
        }
    }


}
