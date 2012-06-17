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
import com.nokia.extras 1.1

Page {
    id: introPage
    orientationLock: PageOrientation.LockPortrait

    function openFile(file) {
        console.log("Loading file: " + file);
        var component = Qt.createComponent(file)

        if (component.status == Component.Ready)
            appWindow.pageStack.push(component);
        else
            console.log("Error loading component:", component.errorString());
    }

    Image {
        source: "qrc:///gfx/d-pointer-logo-symbian.png"
        anchors.centerIn: parent
    }


    Timer {
          interval: 2000; running: true; repeat: false
          onTriggered: {
              openFile("MainPage.qml");
          }
    }
}
