import QtQuick 1.0
import QtMultimediaKit 1.1
import com.nokia.meego 1.0

Item {
    id: streamerItem

    signal playStream(string url, string title)
    signal stopStream(string url)

    property string title

    height: parent.height
    width: parent.width

    state: ""

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
        width: parent.width
        height: parent.height

        Rectangle {
            id: highlight2
            width: parent.width
            height: 1
            color: "#FFFFFF"
            border.width: 0
        }

        Rectangle {
            id: highlight1
            width: parent.width
            height: 1
            color: "#CDCECF"
            border.width: 0
        }

        Rectangle {
            id: audioStreamerUI
            width: parent.width
            height: parent.height
            anchors.margins: 10
            color: "#E4E5E6"

            Column {
                id: buttonGroup
                anchors.centerIn: parent
                spacing: 10
                width: parent.width

                Label {
                    id: streamTitleLabel
                    text: title
                    width: parent.width - durationLabel.width
                    anchors.horizontalCenter: parent.horizontalCenter
                    elide: Text.ElideRight
                    horizontalAlignment: Text.AlignHCenter
                }

                Row {
                    spacing: 100
                    anchors.horizontalCenter: parent.horizontalCenter

                    Image {
                        id: rew;
                        source: "qrc:/gfx/playback_rew.png"
                        MouseArea {
                            anchors.fill: parent
                            onClicked: {
                                console.log("Setting audio position to " + audioPlayer.position - 10000 + "s")
                                audioPlayer.position = audioPlayer.position - 10000
                            }
                        }
                    }

                    Image {
                        id: play;
                        source: "qrc:/gfx/playback_play.png"

                        MouseArea {
                            anchors.fill: parent
                            onClicked: {
                                streamerItem.state = "playing";
                                audioPlayer.play();
                            }
                        }
                    }

                    Image {
                        id: pause;
                        source: "qrc:/gfx/playback_pause.png"
                        visible: false

                        MouseArea {
                            anchors.fill: parent
                            onClicked: {
                                streamerItem.state = "paused";
                                audioPlayer.pause();
                            }
                        }
                    }

                    Image {
                        id: ff;
                        source: "qrc:/gfx/playback_ff.png"
                        MouseArea {
                            anchors.fill: parent
                            onClicked: {
                                console.log("Setting audio position to " + audioPlayer.position + 10000 + "s")
                                audioPlayer.position = audioPlayer.position + 10000
                            }
                        }
                    }

                    Image {
                        id: stop;
                        source: "qrc:/gfx/playback_stop.png"

                        MouseArea {
                            anchors.fill: parent
                            onClicked: {
                                streamerItem.state = "stopped";
                                audioPlayer.stop();
                            }
                        }
                    }
                }

                Label {
                    id: durationLabel
                    text: durationText(audioPlayer.position);
                    font.pointSize: 10
                    anchors.horizontalCenter: parent.horizontalCenter
                }
            }
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

        onStarted: {
            console.log("Ok, we started playing...");
        }

        onStopped: {
            stopStream(audioPlayer.source);
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

    transitions: [
        Transition {
            from: "*"
            to: "stopped"
            ParallelAnimation {
                NumberAnimation { target: streamerItem; property: "height"; to: 0; duration: 500 }
                NumberAnimation { target: buttonGroup; property: "opacity"; to: 0; duration: 400 }
            }
        }
    ]

}
