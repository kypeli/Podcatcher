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
#include <QObject>

#include "podcastsqlmanager.h"
#include "podcastepisodesmodelfactory.h"

PodcastEpisodesModelFactory * PodcastEpisodesModelFactory::instance = 0;

PodcastEpisodesModelFactory::PodcastEpisodesModelFactory()
{
    m_sqlmanager = PodcastSQLManagerFactory::sqlmanager();
}

PodcastEpisodesModel * PodcastEpisodesModelFactory::episodesModel(int channelId)
{

    if (channelId < 1) {
       return 0;
    }

    // If the model is already fetched from the DB, just rturn it.
    // Otherwise fetch data from DB an create the model.
    if (m_modelCache.contains(channelId)) {
        return m_modelCache.value(channelId);
    }

    PodcastEpisodesModel *model = new PodcastEpisodesModel(channelId);
    QList<PodcastEpisode *> episodes = m_sqlmanager->episodesInDB(channelId);
    model->addEpisodes(episodes);

    // Cache the constructed model
    m_modelCache.insert(channelId, model);

    return model;
}

PodcastEpisodesModelFactory * PodcastEpisodesModelFactory::episodesFactory()
{
    if (instance == 0) {
        instance = new PodcastEpisodesModelFactory;
    }
    return instance;
}

void PodcastEpisodesModelFactory::removeFromCache(int channelId)
{
    if (m_modelCache.contains(channelId)) {
        m_modelCache.remove(channelId);
    }
}
