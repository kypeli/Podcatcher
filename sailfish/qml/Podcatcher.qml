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
import "pages"

ApplicationWindow
{
    id: appWindow
    signal showChannel(string id)
    signal downloadPodcast(int channelid, int index)
    signal playPodcast(int channelId, int index)
    signal openWeb(int channelId, int index)
    signal refreshEpisodes(int channelId)
    signal cancelDownload(int channelId, int index)
    signal cancelQueue(int channelId, int index)
    signal deleteChannel(string channelId)
    signal allListened(string channelId)
    signal deleteDownloaded(int channelId, int index)
    signal startStreaming(int channelId, int index)
    signal autoDownloadChanged(int channelId, bool autoDownload)

    initialPage: Component { MainPage { } }
    cover: Qt.resolvedUrl("cover/CoverPage.qml")
    allowedOrientations:Orientation.Portrait
    _defaultPageOrientations: Orientation.Portrait
}


