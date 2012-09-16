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
#include <gq/gconfitem.h>

#include "podcastsqlmanager.h"
#include "dbhelper.h"

DBHelper::DBHelper()
{
}

void DBHelper::createAutoDownloadFieldChannels()
{

    GConfItem *autoDlConf        = new GConfItem("/apps/ControlPanel/Podcatcher/autodownload", this);
    bool autodownloadOnSettings = autoDlConf->value().toBool();
    delete autoDlConf;

    PodcastSQLManager *sqlmanager = PodcastSQLManagerFactory::sqlmanager();
    sqlmanager->checkAndCreateAutoDownload(autodownloadOnSettings);
}
