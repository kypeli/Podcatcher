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

Page {
    id: mainPage

    property string version

    //orientationLock: PageOrientation.LockPortrait


    Component.onCompleted: {

        mainPage.version = qsTr("Podcatcher for SailfishOS");

    }

    Item {
        id: item1
        width: parent.width; height: parent.height

        Column {
            id: column1
            width: parent.width
            anchors.top: parent.top
            anchors.topMargin: Theme.paddingLarge
            anchors.horizontalCenter: parent.horizontalCenter

            spacing:Theme.paddingMedium

            PageHeader {
                id: header
                title: mainPage.version

            }

            Label {
                id: versionLabel
                //width: 300;
                font.pointSize: Theme.fontSizeLarge
                text: ui.versionString();
                horizontalAlignment: Text.AlignHCenter;
                anchors.horizontalCenter: parent.horizontalCenter
            }

            Flickable {
                id: creditsScroll
                //anchors.horizontalCenter: parent.horizontalCenter
                anchors.left: parent.left
                anchors.right: parent.right
                width: parent.width;
                height: item1.height - header.height - versionLabel.height - lisenced.height - Theme.paddingMedium *5 - Theme.paddingLarge
                //contentWidth: creditText.width
                contentHeight: creditText.height
                clip: true
                flickableDirection: Flickable.VerticalFlick
                anchors.topMargin: 20

                Label {
                    id: creditText
                    font.pixelSize: Theme.fontSizeMedium
                    text:
                        "SailfishOS Version<BR><b>Moritz Carmesin</B><BR>" +
                        "carolus@carmesinus.de<BR><BR>" +
                        "Original Author<BR>"+
                        "<B>Johan Paul</B><br>" +
                        "johan@paul.fi<BR>" +
                        "Twitter: @kypeli<BR><br>" +
                        "UX and icon by<br><b>Niklas Gustafsson</B><br>" +
                        "niklas@nikui.net<BR><BR>" +
                        "Translations<br>"+
                        "German: <b>Moritz Carmesin</b><br>"+
                        "French: <b>Quent-in</b><br>"+
                        "<BR><BR>" +
                        "Tested by<BR><b>Mats Sjöberg</B><BR>" +
                        "mats@sjoberg.fi<BR><BR>" +
                        "Special thanks to <B>gPodder.net</B> for<BR>providing an awesome<BR>backend for finding podcasts!<BR><BR><BR>" +
                        "Greetings go to<BR><BR>@zchydem and @jan_ekholm!<BR><BR>" +
                        "Team Qontactors and<BR>team CoCo of Harmattan<BR><BR><BR>" +
                        "Don't forget to read<BR>my blog at<BR><B>http://www.johanpaul.com/blog</B><BR><BR><BR>" +
                        "See you all next time!<BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR<BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR>" +
                        "...also greets to Fopaman and BCG<BR>of Sorcerers!<BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR><BR>" +
                        "Now on a <B>Jolla</B>!"
                    horizontalAlignment: Text.AlignHCenter;
                    textFormat: Text.RichText
                    width: parent.width
                }

            }

        }

        Label {
            id: lisenced
            anchors.top: column1.bottom
            anchors.topMargin: Theme.paddingMedium
            anchors.left: parent.left
            anchors.right: parent.right
            text: qsTr("Licensed and distributed under the <B>GPLv3 license</B>.<BR><center>http://www.gnu.org/copyleft/gpl.html</center>")
            textFormat: Text.RichText
            wrapMode: Text.WordWrap
            horizontalAlignment: Text.AlignHCenter;

        }

    }
}
