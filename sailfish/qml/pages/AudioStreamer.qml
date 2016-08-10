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
import QtMultimedia 5.2

import Sailfish.Silica 1.0

DockedPanel {
    id: streamerItem

    signal playStream(string url, string title)
    signal stopStream(string url)

    property string title

    height: Theme.itemSizeExtraLarge + 2* Theme.paddingLarge
    width: parent.width

    state: ""

    opacity: 1
    Rectangle{
        anchors.fill: parent
        color: Theme.highlightDimmerColor
        opacity: .9
    }


    function durationText(curPos) {
        var curPosSecs = Math.floor((parseInt(curPos) / 1000));

        var curPlayTimeSecs = (curPosSecs % 60);
        var curPlayTimeMins = Math.floor((curPosSecs / 60));
        var curPlayTimeHours = Math.floor((curPosSecs / 3600));

        if (curPlayTimeHours == 0) {
            curPlayTimeHours = ""
        } else if (curPlayTimeHours < 10) {
            curPlayTimeHours = "0" + curPlayTimeHours.toString();
        }

        if (curPlayTimeHours > 0) {
            curPlayTimeMins = curPlayTimeMins % 60;
        }

        if (curPlayTimeSecs < 10) {
            curPlayTimeSecs = "0" + curPlayTimeSecs;
        }

        if (curPlayTimeMins < 10) {
            curPlayTimeMins = "0" + curPlayTimeMins;
        }

        if (curPlayTimeHours.toString().length > 0) {
            curPlayTimeHours = curPlayTimeHours + " : ";
        }

        var playtimeString = curPlayTimeHours.toString() + curPlayTimeMins.toString() + " : " + curPlayTimeSecs.toString();

        return playtimeString;
    }

    Column {
        id: buttonGroup
        spacing: Theme.paddingSmall
        anchors.fill: parent
        anchors.margins: Theme.paddingMedium


        Label {
            id: streamTitleLabel
            text: title
            width: parent.width //- durationLabel.width
            anchors.horizontalCenter: parent.horizontalCenter
            elide: Text.ElideRight
            horizontalAlignment: Text.AlignHCenter
            font.pixelSize: Theme.fontSizeTiny
        }

        Row {
            spacing: 100
            anchors.horizontalCenter: parent.horizontalCenter

            IconButton {
                id: rew
                icon.source: "image://theme/icon-m-left?" + (pressed
                                                             ? Theme.highlightColor
                                                             : Theme.primaryColor)
                onClicked: {
                    console.log("Setting audio position to " + audioPlayer.position - 10000 + "s")
                    //audioPlayer.position = audioPlayer.position - 10000
                    audioPlayer.seek(audioPlayer.position - 10000);
                }
            }


            IconButton {
                id: play
                icon.source: "image://theme/icon-m-play?" + (pressed
                                                             ? Theme.highlightColor
                                                             : Theme.primaryColor)
                onClicked: {
                    streamerItem.state = "playing";
                    audioPlayer.play();
                }
            }


            IconButton {
                id: pause
                icon.source: "image://theme/icon-m-pause?" + (pressed
                                                              ? Theme.highlightColor
                                                              : Theme.primaryColor)
                onClicked: {
                    streamerItem.state = "paused";
                    audioPlayer.pause();
                }
            }



            IconButton {
                id:ff
                icon.source: "image://theme/icon-m-right?" + (pressed
                                                              ? Theme.highlightColor
                                                              : Theme.primaryColor)
                onClicked: {
                    console.log("Setting audio position to " + audioPlayer.position + 10000 + "s")
                    //audioPlayer.position = audioPlayer.position + 10000
                    audioPlayer.seek(audioPlayer.position + 10000);
                }
            }



            IconButton {
                id: stop
                icon.source: "image://theme/icon-m-close?" + (pressed
                                                              ? Theme.highlightColor
                                                              : Theme.primaryColor)
                onClicked: {
                    streamerItem.state = "stopped";
                    audioPlayer.stop();
                }
            }


        }

        Label {
            id: durationLabel
            text: durationText(audioPlayer.position);
            font.pixelSize: Theme.fontSizeTiny
            anchors.horizontalCenter: parent.horizontalCenter
        }
    }


    onPlayStream: {
        audioPlayer.source = url;
        streamerItem.title = title;
        audioPlayer.play();
        streamerItem.state = "playing";
    }

    onStateChanged: {
        console.log("State: " + state);
    }


    Audio {
        id: audioPlayer

        onPlaying: {
            console.log("Ok, we started playing...");
        }

        onStopped: {
            stopStream(audioPlayer.source);
            hide();
        }
    }

    states: [
        State {
            name: "playing"
            PropertyChanges {
                target: play
                visible: false
            }
            PropertyChanges {
                target: pause
                visible: true
            }
            PropertyChanges {
                target: buttonGroup
                opacity: 1
            }
        },
        State {
            name: "paused"
            PropertyChanges {
                target: play
                visible: true
            }
            PropertyChanges {
                target: pause
                visible: false
            }
            PropertyChanges {
                target: buttonGroup
                opacity: 1
            }
        },
        State {
            name: "stopped"
            PropertyChanges {
                target: play
                visible: true
            }
        }
    ]

}
