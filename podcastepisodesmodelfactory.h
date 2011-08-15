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
#ifndef PODCASTEPISODESMODELFACTORY_H
#define PODCASTEPISODESMODELFACTORY_H

#include <QMap>
#include <QList>

#include "podcastepisodesmodel.h"
#include "podcastsqlmanager.h"

class PodcastEpisodesModelFactory
{
public:
    static PodcastEpisodesModelFactory* episodesFactory();
    PodcastEpisodesModel * episodesModel(int channelId);

    void removeFromCache(int channelId);

private:
    PodcastEpisodesModelFactory();

    static PodcastEpisodesModelFactory *instance;
    PodcastSQLManager *m_sqlmanager;
    QMap<int, PodcastEpisodesModel *> m_modelCache;

};

#endif // PODCASTEPISODESMODELFACTORY_H
