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
#ifndef PODCASTGLOBALS_H
#define PODCASTGLOBALS_H

#include <QString>
#include <QDesktopServices>


// const QString PODCATCHER_WORKDIR_NAME("podcatcher");

#ifdef Q_OS_LINUX
    const QString PODCATCHER_WORKDIR_NAME(".podcatcher");

    const QString PODCATCHER_PATH = QString("%1/%2/").arg(QDesktopServices::storageLocation(QDesktopServices::HomeLocation).arg(PODCATCHER_WORKDIR_NAME);
    const QString PODCATCHER_PODCAST_DLDIR = QString("%1/MyDocs/.sounds/podcasts/").arg(QDesktopServices::storageLocation(QDesktopServices::HomeLocation));
#endif

#ifdef Q_OS_SYMBIAN
    const QString PODCATCHER_ROOT = QString("E:\\Podcatcher");

    const QString PODCATCHER_PATH = QString("%1\\").arg(PODCATCHER_ROOT);
    const QString PODCATCHER_PODCAST_DLDIR = QString("%1\\podcasts\\").arg(PODCATCHER_ROOT);
#endif

#endif // PODCASTGLOBALS_H
