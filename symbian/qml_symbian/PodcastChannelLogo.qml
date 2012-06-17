import QtQuick 1.1

Image {
    property string channelLogo
    source: (channelLogo.length == 0 ? "qrc:///gfx/Podcatcher_generic_podcast_cover.png" : channelLogo)
    smooth: true
}
