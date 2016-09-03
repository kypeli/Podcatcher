/**
 * This file is part of Podcatcher for Sailfish OS.
 * Author: Johan Paul (johan.paul@gmail.com)
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
#ifndef PODCASTGLOBALS_H
#define PODCASTGLOBALS_H

#include <QString>
#include <QDesktopServices>

//const QString PODCATCHER_WORKDIR_NAME(".podcatcher");
const QString PODCATCHER_WORKDIR_NAME("harbour-podcatcher");
const QString PODCATCHER_PATH = QString("%1/%2/").arg(QStandardPaths::writableLocation(QStandardPaths::ConfigLocation))
                                          .arg(PODCATCHER_WORKDIR_NAME);
//const QString PODCATCHER_PODCAST_DLDIR = QString("%1/MyDocs/.sounds/podcasts/").arg(QStandardPaths::writableLocation(QStandardPaths::HomeLocation));
const QString PODCATCHER_PODCAST_DLDIR = QString("%1/podcasts/").arg(QStandardPaths::writableLocation(QStandardPaths::MusicLocation));

#endif // PODCASTGLOBALS_H
