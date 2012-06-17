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

Page {
    id: mainPage
    anchors.fill: parent

    property string version

    orientationLock: PageOrientation.LockPortrait
    tools:
        ToolBarLayout {
        id: aboutToolbar
        ToolButton {
            flat: true
            iconSource: "toolbar-back"
            onClicked: appWindow.pageStack.depth <= 1 ? Qt.quit() : appWindow.pageStack.pop()
            }
        }

    Component.onCompleted: {
        if (ui.isLiteVersion()) {
            mainPage.version = "Podmaster BETA";
        } else {
            mainPage.version = "Podmaster";
        }
    }

    Item {
        id: item1
        width: parent.width; height: parent.height - 50

        Column {
            id: column1
            anchors.top: parent.top
            anchors.topMargin: 20
            anchors.horizontalCenter: parent.horizontalCenter
            width: parent.width;

            spacing: 10

            Label {
                width: 300; height: 50
                font.pointSize: 19;
                text: mainPage.version
//                style: Text.Raised;
                horizontalAlignment: Text.AlignHCenter;
                anchors.horizontalCenter: parent.horizontalCenter
            }

            Label {
                width: 300;
                font.pointSize: 15
                text: ui.versionString();
                horizontalAlignment: Text.AlignHCenter;
                anchors.horizontalCenter: parent.horizontalCenter
            }

            Image{
//                width: 100; height: 100
                source: "qrc:///gfx/d-pointer-logo-small.png"

                anchors.horizontalCenter: parent.horizontalCenter
            }

            Label {
                text: "w w w . d - p o i n t e r . c o m"
                font.pointSize: 14
                anchors.horizontalCenter: parent.horizontalCenter
                horizontalAlignment: Text.AlignHCenter;
            }

            Label {
                anchors.horizontalCenter: parent.horizontalCenter
                id: creditText
                font.pointSize: 6
                text: "<B>Johan Paul</B><br>" +
                      "johan.paul@d-pointer.com<BR>" +
                      "Twitter: @kypeli<BR><br>" +
                      "UX and icon by<br><b>Niklas Gustafsson</B><br>" +
                      "niklas@nikui.net<BR><BR>" +
                      "Special thanks to <B>gPodder.net</B><BR>for providing an awesome<BR>backend for finding podcasts!";

                horizontalAlignment: Text.AlignHCenter;
            }

            Label {
                id: lisenced
                anchors.top: column1.bottom
                anchors.topMargin: 10
                font.pointSize: 2
                width: 350; height: 200
                text: "Licensed and distributed under the <B>GPLv3 license</B>.<BR><center>http://www.gnu.org/copyleft/gpl.html</center>"
                wrapMode: Text.WordWrap
                horizontalAlignment: Text.AlignHCenter;
                anchors.horizontalCenter: parent.horizontalCenter

            }

        }


    }
}
