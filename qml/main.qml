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
import com.nokia.meego 1.0

PageStackWindow {
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

    showToolBar: false
    showStatusBar: false
    initialPage: intro

    DPointerIntro {
        id: intro
    }

    MainPage{
        id: mainPage
    }


}
