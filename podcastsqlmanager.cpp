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
#include <QDir>
#include <QSqlQuery>
#include <QSqlError>
#include <QString>
#include <QVariant>

#include <QtDebug>

#include "podcastglobals.h"
#include "podcastsqlmanager.h"

PodcastSQLManager* PodcastSQLManagerFactory::m_instance = 0;
PodcastSQLManagerFactory::PodcastSQLManagerFactory()
{
}

PodcastSQLManager* PodcastSQLManagerFactory::sqlmanager() {
    // Database connection is already open, so just return it.
    if (m_instance == 0) {
        m_instance = new PodcastSQLManager;
    }
    return m_instance;
}


PodcastSQLManager::PodcastSQLManager(QObject *parent) :
    QObject(parent)
{
    QString databasePath;
    databasePath = PODCATCHER_PATH;

    // Create the database in user's homedir, subdir ".podcatcher/".
    QDir dirpath(databasePath);
    if (!dirpath.exists()) {
        dirpath.mkpath(databasePath);
    }

    m_connection = QSqlDatabase::addDatabase("QSQLITE");
    m_connection.setDatabaseName(databasePath + "/" + "podcatcher.sql");

    if (!m_connection.open()) {
        qWarning() << "Could not open database with path " << databasePath;
        return;
    }

    // If database does not contain table "channels", so create it.
    if (!m_connection.tables().contains("channels")) {
        QSqlQuery q(m_connection);
        m_connection.transaction();

        qDebug() << "Creating table 'channels'";

        q.exec("CREATE TABLE channels (id INTEGER PRIMARY KEY, "
                                      "rssurl TEXT, "
                                      "title TEXT, "
                                      "description TEXT, "
                                      "logo TEXT)");
   //     q.exec("INSERT INTO channels(description, logo) VALUES ('This is a test channel', 'test.jpg')");

        if (!m_connection.commit()) {
            qDebug() << "SQL error: " << m_connection.lastError().text();
        }

        if (!q.isValid()) {
            qDebug() << q.lastError().text();
        }

    }

    // If database does not contain the table "episodes", so crate it.
    if (!m_connection.tables().contains("episodes")) {
        QSqlQuery q(m_connection);
        m_connection.transaction();

        qDebug() << "Creating table 'episodes'";

        q.exec("CREATE TABLE episodes (id INTEGER PRIMARY KEY, "
                                      "title TEXT, "
                                      "channelid INTEGER, "
                                      "downloadLink TEXT, "
                                      "lastPlayed INTEGER, "
                                      "playLocation TEXT, "
                                      "description TEXT, "
                                      "published INTEGER, "
                                      "duration TEXT, "
                                      "downloadSize INTEGER, "
                                       "hasBeenCanceled BOOLEAN, "
                                      "FOREIGN KEY(channelid) REFERENCES channels(id))");
//        q.exec("INSERT INTO episodes(description, channelid) VALUES ('This is a test episode', 1)");

        if (!m_connection.commit()) {
            qDebug() << "SQL error: " <<  m_connection.lastError().text();
        }

        if (!q.isValid()) {
            qDebug() << q.lastError().text();
        }
    }
}

int PodcastSQLManager::podcastChannelToDB(PodcastChannel *channel)
{
    if (channel == 0) {
        return 0;
    }

    if (!m_connection.isOpen()) {
        qWarning() << "SQL connection not open. Returning.";
        return 0;
    }

    if (isChannelInDB(channel)) {
        return 0;
    }

    QSqlQuery q(m_connection);

    // Item not found in database. Go ahead and insert it.
    m_connection.transaction();

    q.prepare("INSERT INTO channels(rssurl, title, description, logo) VALUES (:url, :title, :desc, :logo)");
    q.bindValue(":url", channel->url());
    q.bindValue(":desc", channel->description());
    q.bindValue(":title", channel->title());
    q.bindValue(":logo", channel->logo());
    q.exec();

    m_connection.commit();

    // Update the channel with the id it got in DB
    channel->setId(q.lastInsertId().toInt());

    return q.numRowsAffected();
}

bool PodcastSQLManager::isChannelInDB(PodcastChannel *channel)
{
    QSqlQuery q(m_connection);

    // Find out if the channel is already in our DB.
    // Do not add if the channel is already there.
    q.prepare("SELECT COUNT(id) FROM channels WHERE rssurl=:url");
    q.bindValue(":url", channel->url());
    if (!q.exec()) {
        qDebug() << Q_FUNC_INFO << q.lastError().text();
    }

    if (!q.next()) {
        return false; // DB is probably empty.
    }

    if (q.value(0).toInt() > 0) {   // Channel already exists in the DB - do nothing.
        return true;
    }

    return false;
}

QList<PodcastChannel *> PodcastSQLManager::channelsInDB()
{
    QSqlQuery q(m_connection);
    QList<PodcastChannel *> channels;

    qDebug() << "Returning Podcast channels from DB:";

    q.prepare("SELECT id, title, description, logo, rssurl, "
              "(SELECT COUNT(id) FROM episodes WHERE episodes.channelid = channels.id AND episodes.lastPlayed = 0 AND episodes.playLocation <> '') "
              "FROM channels ORDER BY channels.title");

    if (q.exec() == false) {
        qWarning() << "SQL error:" << q.lastError();
        qWarning() << "Last query: " << q.lastQuery();
    }

    while (q.next()) {
        PodcastChannel *channel = new PodcastChannel;
        channel->setId(q.value(0).toInt());
        channel->setTitle(q.value(1).toString());
        channel->setDescription(q.value(2).toString());
        channel->setLogo(q.value(3).toString());
        channel->setUrl(q.value(4).toString());
        channel->setUnplayedEpisodes(q.value(5).toInt());


        qDebug() << "Unplayed: " << channel->unplayedEpisodes();

        qDebug() << " * Id:" << channel->channelId() << "Deescription: " << channel->description() << "Title:" << channel->title() << "Logo:" << channel->logo();

        channels.append(channel);
    }

    return channels;
}

PodcastChannel * PodcastSQLManager::channelInDB(int channelId)
{
    QSqlQuery q(m_connection);

    qDebug() << "Returning Podcast channel from DB with id" << channelId;

    q.prepare("SELECT title, description, logo, rssurl FROM channels WHERE channels.id = :id");
    q.bindValue(":id", channelId);
    q.exec();
    if (!q.next()) {
        qWarning() << "SQL error: " << q.lastError();
        qWarning() << "Last query:" << q.lastQuery();
        return 0;
    }

    PodcastChannel *channel = new PodcastChannel;
    channel->setId(channelId);
    channel->setTitle(q.value(0).toString());
    channel->setDescription(q.value(1).toString());
    channel->setLogo(q.value(2).toString());
    channel->setUrl(q.value(3).toString());

    qDebug() << " * Id:" << channel->channelId() << "Title:" << channel->title() << "Logo:" << channel->logo();

    return channel;
}



int PodcastSQLManager::podcastEpisodesToDB(QList<PodcastEpisode *> parsedEpisodes, int channelid)
{
    qDebug() << "Got" << parsedEpisodes.length() << "episodes for channel" << channelid;

    if (!m_connection.isOpen()) {
        qWarning() << "SQL connection not open. Returning.";
        return 0;
    }

    QSqlQuery q(m_connection);
    m_connection.transaction();

    foreach(PodcastEpisode* episode, parsedEpisodes) {
        q.prepare("INSERT INTO episodes (title, channelid, downloadLink, playLocation, description, published, duration, downloadSize, lastPlayed, hasBeenCanceled) VALUES "
                  "(:title, :channelid, :downloadLink, :playLocation, :description, :published, :duration, :downloadSize, :lastPlayed, :hasBeenCanceled)");
        q.bindValue(":title", episode->title());
        q.bindValue(":channelid", channelid);
        q.bindValue(":downloadLink", episode->downloadLink());
        q.bindValue(":playLocation", episode->playFilename());
        q.bindValue(":description", episode->description());
        q.bindValue(":published", episode->pubTime().toTime_t());  // NOTE: We save the seconds since EPOC for easier handling.
        q.bindValue(":duration", episode->duration());
        q.bindValue(":downloadSize", episode->downloadSize());
        q.bindValue(":lastPlayed", episode->lastPlayed().isValid() ? episode->lastPlayed().toTime_t() : 0);  // NOTE: We save the seconds since EPOC for easier handling.
        q.bindValue(":hasBeenCanceled", episode->hasBeenCanceled());

        if (!q.exec()) {
            qDebug() << "Last query: " << q.lastQuery();
            qDebug() << "Error: " << q.lastError();
        }

        episode->setDbId(q.lastInsertId().toInt());
        qDebug() << "Giving episode a DB ID:" << episode->dbid();
    }

    m_connection.commit();

    return q.numRowsAffected();
}

bool PodcastSQLManager::podcastEpisodeToDB(PodcastEpisode *episode, int channelid)
{
    if (episode == 0) {
        return false;
    }

    if (!m_connection.isOpen()) {
        qWarning() << "SQL connection not open. Returning.";
        return false;
    }

    QSqlQuery q(m_connection);
    q.prepare("INSERT INTO episodes(title, channelid, downloadLink, playLocation, description, published, duration, downloadSize, lastPlayed, hasBeenCanceled) VALUES "
              "(:title, :channelid, :downloadLink, :playLocation, :description, :published, :duration, :downloadSize, :lastPlayed, :hasBeenCanceled)");
    q.bindValue(":title", episode->title());
    q.bindValue(":channelid", channelid);
    q.bindValue(":downloadLink", episode->downloadLink());
    q.bindValue(":playLocation", episode->playFilename());
    q.bindValue(":description", episode->description());
    q.bindValue(":published", episode->pubTime().toTime_t());  // NOTE: We save the seconds since EPOC for easier handling.
    q.bindValue(":duration", episode->duration());
    q.bindValue(":downloadSize", episode->downloadSize());

    QDateTime lastPlayed = episode->lastPlayed();
    if (lastPlayed.isValid()) {
        q.bindValue(":lastPlayed", episode->lastPlayed().toTime_t());
    } else {
        q.bindValue(":lastPlayed", 0);
    }

    q.bindValue(":hasBeenCanceled", episode->hasBeenCanceled());

    if (!q.exec()) {
        qDebug() << "Last query: " << q.lastQuery();
        qDebug() << "Error: " << q.lastError();
        return false;
    }

    return true;
}

QList<PodcastEpisode *> PodcastSQLManager::episodesInDB(int channelId)
{
    QSqlQuery q(m_connection);
    QList<PodcastEpisode *> episodes;

    qDebug() << "Returning Podcast episodes from DB for channel:" << channelId;

    q.prepare("SELECT id, title, downloadLink, playLocation, description, published, duration, downloadSize, channelid, lastPlayed, hasBeenCanceled "
              "FROM episodes WHERE episodes.channelid = :chanId ORDER BY episodes.published DESC LIMIT 50");
    q.bindValue(":chanId", channelId);

    if (!q.exec()) {
        qWarning() << "Fetching episodes, SQL error: " << q.lastError();
        qWarning() << "Last query:" << q.lastQuery();
        return episodes;
    }

    while (q.next()) {
        PodcastEpisode *episode = new PodcastEpisode;
        episode->setDbId(q.value(0).toInt());
        episode->setTitle(q.value(1).toString());
        episode->setDownloadLink(q.value(2).toString());
        episode->setPlayFilename(q.value(3).toString());
        episode->setDescription(q.value(4).toString());
        episode->setPubTime(QDateTime::fromTime_t(q.value(5).toInt()));
        episode->setDuration(q.value(6).toString());
        episode->setDownloadSize(q.value(7).toInt());
        if (q.value(9).toInt() > 0) {
            episode->setLastPlayed(QDateTime::fromTime_t(q.value(9).toInt()));
        }
        episode->setHasBeenCanceled(q.value(10).toBool());

        // Since we requested channels for this channel, we might as well be sure the value is what we requested as parameter.
        episode->setChannelId(channelId);

        qDebug() << " * id" << episode->dbid() << "Title:" << episode->title() << "Last played:" << episode->lastPlayed();

        episodes.append(episode);
    }

    qDebug() << "Fetched" << episodes.size() << "episodes.";
    return episodes;
}

QDateTime PodcastSQLManager::latestEpisodeTimestampInDB(int channelId)
{
    QDateTime latestDate = QDateTime();
    QSqlQuery q(m_connection);

    q.prepare("SELECT published FROM episodes WHERE episodes.channelid = :chanId ORDER BY episodes.published DESC LIMIT 1");
    q.bindValue(":chanId", channelId);

    if (q.exec()) {
        q.next();
        int timestamp = q.value(0).toInt();
        latestDate = QDateTime::fromTime_t(timestamp);

        qDebug() << "Got latest episode timestamp: " << latestDate;

    } else {
        qWarning() << "SQL error: " << q.lastError();
        qWarning() << "SQL query: " << q.lastQuery();
    }

    return latestDate;
}

void PodcastSQLManager::updatePodcastInDB(PodcastEpisode *episode)
{
    qDebug() << "Updating episode data to DB";
    if (!m_connection.isOpen()) {
        qWarning() << "SQL connection not open. Returning.";
        return;
    }

    QSqlQuery q(m_connection);
    q.prepare("UPDATE episodes SET title=:title, downloadLink=:downloadLink, playLocation=:playLocation, description=:description, "
              "published=:published, duration=:duration, downloadSize=:downloadSize, lastPlayed=:lastPlayed, hasBeenCanceled=:hasBeenCanceled "
              "WHERE id=:id");
    q.bindValue(":title", episode->title());
    q.bindValue(":downloadLink", episode->downloadLink());
    q.bindValue(":playLocation", episode->playFilename());
    q.bindValue(":description", episode->description());
    q.bindValue(":published", episode->pubTime().toTime_t());  // NOTE: We save the seconds since EPOC for easier handling.
    q.bindValue(":duration", episode->duration());
    q.bindValue(":downloadSize", episode->downloadSize());
    q.bindValue(":id", episode->dbid());
    q.bindValue(":lastPlayed", episode->lastPlayed().isValid() ? episode->lastPlayed().toTime_t() : 0);  // NOTE: We save the seconds since EPOC for easier handling.
    q.bindValue(":hasBeenCanceled", episode->hasBeenCanceled());

    if (!q.exec()) {
        qDebug() << "Last query: " << q.lastQuery();
        qDebug() << "Error: " << q.lastError();
        return;
    }

    qDebug() << "Updated episode:" << episode->dbid() << "last played:" << episode->lastPlayed();
}

void PodcastSQLManager::removeChannelFromDB(int channelId)
{
    QSqlQuery q(m_connection);

    qDebug() << "Deleting all episodes from DB with channel: " << channelId;

    q.prepare("DELETE FROM episodes WHERE episodes.channelId = :chanId");
    q.bindValue(":chanId", channelId);
    if (!q.exec()) {
        qWarning() << "SQL error:" << q.lastError();
        qWarning() << "SQL query:" << q.lastQuery();
    }

    qDebug() << "Deleting the channel from DB with channel: " << channelId;

    q.clear();

    q.prepare("DELETE FROM channels WHERE channels.id = :chanId");
    q.bindValue(":chanId", channelId);
    if (!q.exec()) {
        qWarning() << "SQL error:" << q.lastError();
        qWarning() << "SQL query:" << q.lastQuery();
    }

}

