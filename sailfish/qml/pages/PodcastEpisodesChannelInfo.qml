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
Column{
    width: parent.width
    height: parent.height

    spacing: Theme.paddingMedium

    Row{
        width: parent.width
        height: parent.height - autoDownloadSwitch.height - Theme.paddingMedium

        spacing: Theme.paddingMedium

        PodcastChannelLogo {
            id: channelLogo;
            channelLogo: channel.logo
            width: 130
            height: 130
        }

        Label {
            id: channelDescription
            height: parent.height
            width: parent.width- channelLogo.width - Theme.paddingMedium
            text: channel.description
            truncationMode: TruncationMode.Elide
            font.pixelSize: Theme.fontSizeTiny
            wrapMode: Text.WordWrap
        }
    }

    TextSwitch{
        id: autoDownloadSwitch
        text: "Auto-download"

        onCheckedChanged: {
            appWindow.autoDownloadChanged(channel.channelId, checked);
        }
    }

}
}
