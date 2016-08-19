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

Item {
    property int downloadedBytes
    property int totalBytes;

    width: downloadLabel.width + cancelButton.width
    height: podcastItem.height

    /*Label {
        id: downloadLabel
        anchors.left: parent.left
        anchors.right: cancelButton.left
        anchors.verticalCenter: parent.verticalCenter
        font.pixelSize: Theme.fontSizeExtraSmall
        text: "Downloading"
        width: parent.width - cancelButton.width
        truncationMode: TruncationMode.Fade
    }*/

    ProgressBar {
        id: progressBar
        //width: downloadLabel.width
        //width: parent.width
        maximumValue: model.totalDownloadSize
        value: model.alreadyDownloadedSize
        onValueChanged: console.log("Downloaded: "+ value)
        anchors.left: podcastItem.left
        anchors.right: podcastItem.right
//        anchors.left: downloadLabel.left
//        anchors.top: downloadLabel.bottom
//        anchors.right: parent.right
//        //anchors.topMargin: Theme.paddingSmall
//        //anchors.right: cancelButton.right
    }

    Image {
        id: cancelButton
        source: "image://theme/icon-m-clear"
        anchors.right:  parent.right
        anchors.verticalCenter: downloadLabel.verticalCenter
        width: Theme.fontSizeSmall
        height: Theme.fontSizeSmall
    }

    MouseArea {
        anchors.fill: parent
        onClicked: {
            console.log("Cancel download of: " + channelId + "index: "+index);
            appWindow.cancelDownload(channelId, index);
        }
    }
}
