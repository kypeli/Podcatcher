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

Page {
    id: mainPage
    anchors.fill: parent

    property string version

    orientationLock: PageOrientation.LockPortrait
    tools:
        ToolBarLayout {
        ToolIcon { iconId: "toolbar-back"; onClicked: { pageStack.pop(); } }
    }

    Component.onCompleted: {
        if (ui.isLiteVersion()) {
            mainPage.version = "Podcatcher Lite for N9";
        } else {
            mainPage.version = "Podcatcher for N9";
        }
    }

    Item {
        id: item1
        width: parent.width; height: parent.height

        Column {
            id: column1
            anchors.top: parent.top
            anchors.topMargin: 20
            anchors.horizontalCenter: parent.horizontalCenter

            spacing: 10

            Text {
                width: 300; height: 50
                font.pointSize: 22;
                text: mainPage.version
                style: Text.Raised;
                horizontalAlignment: Text.AlignHCenter;
                anchors.horizontalCenter: parent.horizontalCenter
            }

            Text {
                width: 300;
                font.pointSize: 18
                text: "1.0.0";
                horizontalAlignment: Text.AlignHCenter;
                anchors.horizontalCenter: parent.horizontalCenter
            }

            Image{
//                width: 100; height: 100
                source: "qrc:///gfx/d-pointer-logo-small.png"

                anchors.horizontalCenter: parent.horizontalCenter
            }

            Text {
                text: "w w w . d - p o i n t e r . c o m"
                font.pointSize: 18
                anchors.horizontalCenter: parent.horizontalCenter
                horizontalAlignment: Text.AlignHCenter;
            }

            Flickable {
                id: creditsScroll
                anchors.horizontalCenter: parent.horizontalCenter
                width: 350; height: 400
                contentWidth: creditText.width
                contentHeight: creditText.height
                clip: true
                flickableDirection: Flickable.VerticalFlick

                Text {
                    id: creditText
                    font.pointSize: 16
                    text: "<B>Johan Paul</B><br>" +
                          "johan.paul@d-pointer.com<BR>" +
                          "Twitter: @kypeli<BR><br>" +
                          "UX and icon by<br><b>Niklas Gustafsson</B><br>" +
                          "niklas@nikui.net<BR><BR>" +
                          "Tested by<BR><b>Mats Sjöberg</B><BR>" +
                          "mats@sjoberg.fi<BR><BR>" +
                          "Special thanks to <B>gPodder.net</B> for<BR>providing an awesome<BR>backend for finding podcasts!<BR><BR><BR>" +
                          "Greetings go to<BR><BR>@zchydem and @jan_ekholm<BR>of <B>D-Pointer</B>!<BR><BR>" +
                          "Team Qontactors and<BR>team CoCo of Harmattan<BR><BR><BR>" +
                          "Don't forget to read<BR>my blog at<BR><B>http://www.johanpaul.com/blog</B><BR><BR><BR>" +
                          "See you all next time!<BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR<BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR>" +
                          "...also greets to fopaman and BCG<BR>of Sorcerers!";

                    horizontalAlignment: Text.AlignHCenter;
                }

            }

        }

        Text {
            id: lisenced
            anchors.top: column1.bottom
            anchors.topMargin: 10
            font.pointSize: 16
            width: 350; height: 200
            text: "Licensed and distributed under the <B>GPLv3 license</B>.<BR><center>http://www.gnu.org/copyleft/gpl.html</center>"
            wrapMode: Text.WordWrap
            horizontalAlignment: Text.AlignHCenter;
            anchors.horizontalCenter: parent.horizontalCenter

        }

    }
}
