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
import QtQuick 1.1
import com.nokia.meego 1.0
import com.nokia.extras 1.0

Sheet {
    acceptButtonText: "Import"
    rejectButtonText: "Cancel"

    content:
    Item {

        Label {
            id: header
            anchors.top: parent.top
            anchors.topMargin: 20
            anchors.left: parent.left
            anchors.leftMargin: 20

            font.pixelSize: 27
            font.bold: true
            text: "Import podcasts from gPodder.net"
        }

        TextField {
            id: usernameField
            anchors.top: header.bottom
            anchors.topMargin: 20
            anchors.left: header.left;
            placeholderText: "Username"
            width: 420

            Keys.onReturnPressed: {
                parent.focus = true;
            }
        }

        TextField {
            id: passwordField
            anchors.top: usernameField.bottom
            anchors.topMargin: 20
            anchors.left: header.left;
            placeholderText: "Password"
            width: 420
            echoMode: TextInput.Password

            Keys.onReturnPressed: {
                parent.focus = true;
            }
        }

        Image {
            id: gpodderLogo
            source: "qrc:///gfx/gpodder-logo.png"
            anchors.top: passwordField.bottom
            anchors.topMargin: 100
            anchors.horizontalCenter: header.horizontalCenter
            smooth: true
        }
    }

    onStatusChanged: {
        if (status == DialogStatus.Opening) {
            usernameField.text = ""
            passwordField.text = ""
        }

    }

    onAccepted: {
        mainPage.gpodderImport(usernameField.text, passwordField.text);
        pageStack.pop();
    }
}
