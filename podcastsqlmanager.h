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
#ifndef PODCASTSQLMANAGER_H
#define PODCASTSQLMANAGER_H

#include <QObject>
#include <QList>
#include <QSqlDatabase>
#include <QMutex>

#include "podcastchannel.h"
#include "podcastepisode.h"

class PodcastSQLManager : public QObject
{
    Q_OBJECT
public:
    QList<PodcastChannel *> channelsInDB();
    PodcastChannel* channelInDB(int channelId, PodcastChannel *channel = 0);

    QList<PodcastEpisode *> episodesInDB(int channelId);

    int podcastChannelToDB(PodcastChannel *channel);
    bool isChannelInDB(PodcastChannel *channel);

    bool podcastEpisodeToDB(PodcastEpisode *episode,
                            int channel_id);

    int podcastEpisodesToDB(QList<PodcastEpisode *> parsedEpisodes,
                            int channel_id);

    bool removePodcastFromDB(PodcastEpisode *episode);

    void updatePodcastInDB(PodcastEpisode *episode);

//    void updateEpisodeData(QString key, QString newValue, int id);

    QDateTime latestEpisodeTimestampInDB(int channelId);

    void removeChannelFromDB(int channelId);

signals:

public slots:

private:
    PodcastSQLManager(QObject *parent = 0);
    //    bool episodeInDB(PodcastEpisode *episode);

    QSqlDatabase m_connection;
    friend class PodcastSQLManagerFactory;

    // Disable instatiation.
    PodcastSQLManager(PodcastSQLManager const&);              // Don't Implement
    void operator=(PodcastSQLManager const&);                 // Don't implement

    QMutex mutex;
};

class PodcastSQLManagerFactory
{
public:
    PodcastSQLManagerFactory();
    static PodcastSQLManager* sqlmanager();

private:
    static PodcastSQLManager* m_instance;
};


#endif // PODCASTSQLMANAGER_H
