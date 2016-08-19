import QtQuick 2.0
import Sailfish.Silica 1.0

DockedPanel {
    id: dockPanel
    open: false
    width: parent.width
    height: label.height + Theme.paddingLarge
    dock: Dock.Top

    property string text: value =""
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

