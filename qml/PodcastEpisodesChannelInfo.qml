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
import com.meego 1.0
import com.nokia.extras 1.0

Item {
    Rectangle {
        id: podcastInfoRect
        color: "#E4E5E6"
        width: parent.width
        height:  parent.height

        PodcastChannelLogo {
            id: channelLogo;
            channelLogo: channel.logo
            anchors.left: podcastInfoRect.left
            anchors.top: podcastInfoRect.top
            anchors.leftMargin: 10
            anchors.topMargin: 10
            width: 130
            height: 130
        }

        Label {
            id: channelDescription
            width: podcastInfoRect.width - channelLogo.width - 50

            text: (channel.description.length > 180 ? channel.description.substr(0, 180).concat("...") : channel.description)
            font.pointSize: 10
            wrapMode: Text.WordWrap

            anchors.left: channelLogo.right
            anchors.leftMargin: 15
            anchors.top:  channelLogo.top
        }
    }

}
