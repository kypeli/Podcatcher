/**
 * This file is part of Podcatcher for Sailfish OS.
 * Author: Moritz Carmesin (carolus@carmesinus.de)
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

DockedPanel {
    id: dockPanel
    open: false
    width: parent.width
    height: label.height + Theme.paddingLarge
    dock: Dock.Top

    property string text: ""
    property bool timerEnabled: true
    property alias timerShowTime: autoClose.interval

    Rectangle{
        anchors.fill: parent
        color: Theme.highlightBackgroundColor
        opacity: Theme.highlightBackgroundOpacity
    }
    MouseArea{
        anchors.fill: parent

        onClicked: {
            autoClose.stop();
            dockPanel.hide();
        }
    }

    Label{
        id: label
        width:parent.width
        height: Text.paintedHeight
        anchors.verticalCenter: parent.verticalCenter
        text: dockPanel.text
        font.pixelSize: Theme.fontSizeSmall
        wrapMode: Text.WordWrap
        horizontalAlignment: Text.AlignHCenter
    }

    Timer {
        id: autoClose
        interval: 5000
        running: false
        onTriggered: {
            dockPanel.hide()
            stop()
        }
    }

    onOpenChanged: {
        if(open && timerEnabled)
            autoClose.start()
    }
}

