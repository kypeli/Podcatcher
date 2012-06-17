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

Item {
    id: item1
    width: parent.width
    anchors.verticalCenter: parent.verticalCenter
    anchors.horizontalCenter: parent.horizontalCenter
    z: 10

    Text {
        id: topText
        width: parent.width - 5
        anchors.horizontalCenter: parent.horizontalCenter
        anchors.bottomMargin: 35
        font.pointSize: 14
        font.bold: true
        color: "grey"
        text: "No podcast subscriptions yet"
        anchors.bottom: parent.bottom
        horizontalAlignment: Text.AlignHCenter;
        wrapMode: Text.WordWrap
    }

    Text {
        width: parent.width
        anchors.top: topText.bottom
        anchors.topMargin: 25
        anchors.horizontalCenter: parent.horizontalCenter
        font.pointSize: 10
        color: "grey"
        text: "Why don't you add some..."
        horizontalAlignment: Text.AlignHCenter;
        wrapMode: Text.WordWrap
    }

}



