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

Dialog {

    Column {
        anchors.fill: parent
        spacing: Theme.paddingMedium

        DialogHeader{
            id: header
            title: qsTr("Import podcasts from gPodder.net")
            acceptText: qsTr("Import")
        }



        TextField {
            id: usernameField
            width: parent.width
            placeholderText: qsTr("Username")

            EnterKey.iconSource: "image://theme/icon-m-enter-next"
            EnterKey.onClicked: passwordField.focus = true

        }

        TextField {
            id: passwordField
            width: parent.width
            placeholderText: qsTr("Password")
            echoMode: TextInput.Password

            Keys.onReturnPressed: {
                parent.focus = true;
            }
        }

        Image {
            id: gpodderLogo
            source: "qrc:///gfx/gpodder-logo.png"
            //anchors.top: passwordField.bottom
            //anchors.topMargin: 100
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
