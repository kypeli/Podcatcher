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
#include <QHash>
#include <QVariant>
#include <QDateTime>


#include <QtDebug>

#include "podcastmanager.h"
#include "podcastepisodesmodel.h"

PodcastEpisodesModel::PodcastEpisodesModel(int channelId, QObject *parent) :
    QAbstractListModel(parent),
    m_channelId(channelId)
{
    m_roles[DbidRole] = "dbid";
    m_roles[TitleRole] = "title";
    m_roles[PubRole] = "published";
    m_roles[DescriptionRole] = "description";
    m_roles[StateRole] = "episodeState";
    m_roles[TotalDownloadRole] = "totalDownloadSize";
    m_roles[AlreadyDownloaded] = "alreadyDownloadedSize";
    m_roles[LastTimePlayedRole] = "lastTimePlayed";
    //setRoleNames(roles);

    m_sqlmanager = PodcastSQLManagerFactory::sqlmanager();
}

PodcastEpisodesModel::~PodcastEpisodesModel() {
}

int PodcastEpisodesModel::rowCount(const QModelIndex &) const
{
    return m_episodes.size();
}

QVariant PodcastEpisodesModel::data(const QModelIndex &index, int role) const
{
    if (index.row() < 0 || index.row() > m_episodes.count())
        return QVariant();

    PodcastEpisode *episode = m_episodes.at(index.row());

    switch(role) {
    case TitleRole:
        return episode->title();
        break;
    case PubRole:
        return episode->pubTime().toString("dd.MM.yyyy");
        break;
    case DbidRole:
        return episode->dbid();
        break;
    case DescriptionRole:
        return episode->description();
        break;
    case StateRole:
        return episode->episodeState();
        break;
    case TotalDownloadRole:
        return episode->downloadSize();
        break;
    case AlreadyDownloaded:
        return episode->alreadyDownloaded();
        break;
    case LastTimePlayedRole:
        if (episode->episodeState() == "played") {
            return QString("Last played: %1").arg(episode->lastPlayed().toString("dd.MM.yyyy hh:mm"));
        } else  {
            return QString();
        }
    default:
        return QVariant();
    }

}

void PodcastEpisodesModel::addEpisode(PodcastEpisode *episode)
{
    QList<PodcastEpisode *> episodes;
    episodes << episode;
    addEpisodes(episodes);
}

void PodcastEpisodesModel::addEpisodes(QList<PodcastEpisode *> episodes)
{
    if (episodes.isEmpty()) {
        return;
    }

    QDateTime modelsLatestEpisode;
    if (m_episodes.isEmpty()) {
        modelsLatestEpisode = QDateTime();
    } else {
        modelsLatestEpisode = m_episodes.at(0)->pubTime();
    }

    QDateTime dbsLatestEpisode = m_sqlmanager->latestEpisodeTimestampInDB(m_channelId);
    PodcastEpisode *episode;
    QList<PodcastEpisode *> newEpisodesToAdd;

    // Add the episode to the UI model if its timestamp is > modelsLatestEpisode. By default the modelsLatestEpisode
    // is the result of the most recent model population - with or without episodes added to the DB.
    // If the episode also has a timestmap > dbsLatestEpisode, then also add it to the DB.
    // Last query the latest timestamp of all channel's episodes after the operation.
    for(int i=episodes.size()-1; i>=0; i--) {
        episode = episodes.at(i);                           // Take the last episode in the new model.
        if (episode->pubTime() > modelsLatestEpisode) {     // If this episodes has a more recent timestamp, add to model.            qDebug() << "Adding to UI...";
            beginInsertRows(QModelIndex(), 0, 0);
            m_episodes.prepend(episode);                    // Since we took that last item from the new episodes model, we add this item first to the view.
            endInsertRows();                                // When we do this for new episodes not yet in the model, all new episodes (episode->pubTime() > modelsLatestEpisode)
                                                            // ends up on top of the list. Note that the QModelIndex is also updated accordingly.
            episode->setChannelId(m_channelId);

            connect(episode, SIGNAL(episodeChanged()),
                    this, SLOT(onEpisodeChanged()));

            if (episode->pubTime() > dbsLatestEpisode) {
                newEpisodesToAdd << episode;
            }
        }
    }

    if (!newEpisodesToAdd.isEmpty()) {
        qDebug() << "Adding new episodes to DB: " << newEpisodesToAdd.size();
        m_sqlmanager->podcastEpisodesToDB(newEpisodesToAdd,
                                          m_channelId);
        m_latestEpisodeTimestamp = m_sqlmanager->latestEpisodeTimestampInDB(m_channelId);
    } else {
        qDebug() << "No new episodes to be added to the DB.";
    }

}

void PodcastEpisodesModel::delEpisode(PodcastEpisode *episode)
{
    qDebug() << "Deleting episode with name: " << episode->title() << ", pub date:" << episode->pubTime();
    for (int i=0; i<m_episodes.length(); i++) {
        if (episode == m_episodes.at(i)) {
            qDebug() << "Remove from model, index: " << i;
            beginRemoveRows(QModelIndex(), i, i);
            m_episodes.removeAt(i);
            endRemoveRows();

            m_sqlmanager->removePodcastFromDB(episode);
        }
    }

    delete episode;
}

void PodcastEpisodesModel::delEpisode(int index, PodcastEpisode *episode)
{
    qDebug() << "Deleting episode with name: " << episode->title() << ", pub date:" << episode->pubTime();

    beginRemoveRows(QModelIndex(), index, index);
    m_episodes.removeAt(index);
    endRemoveRows();

    m_sqlmanager->removePodcastFromDB(episode);
    delete episode;
}

PodcastEpisode * PodcastEpisodesModel::episode(int index)
{
    PodcastEpisode *episode = 0;
    if (index < 0 || index > m_episodes.count())
        return episode;

    episode = m_episodes.at(index);
    return episode;
}

void PodcastEpisodesModel::onEpisodeChanged()
{
    PodcastEpisode *episode  = qobject_cast<PodcastEpisode *>(sender());
    if (episode == 0) {
        return;
    }

    int episodeIndex = m_episodes.indexOf(episode);
    if (episodeIndex != -1) {
        QModelIndex modelIndex = createIndex(episodeIndex, 0);
        emit dataChanged(modelIndex, modelIndex);
    }
}

QList<PodcastEpisode *> PodcastEpisodesModel::undownloadedEpisodes(int max)
{
    QList<PodcastEpisode *> episodes;

    if (m_episodes.isEmpty()) {
        return episodes;
    }

    if (max > m_episodes.length()) {
        max = m_episodes.length();
    }

    for (int i=0; i<max; i++) {
        PodcastEpisode *episode = m_episodes.at(i);
        if (!episode->downloadLink().isEmpty() &&
            episode->playFilename().isEmpty() &&
            episode->hasBeenCanceled() == false) {
            episodes << episode;
        }
    }

    return episodes;
}

void PodcastEpisodesModel::refreshModel()
{
   //  PodcastEpisode *latestEpisode = m_episodes.at(0);
}

void PodcastEpisodesModel::setChannelId(int id)
{
    m_channelId = id;
}

int PodcastEpisodesModel::channelId()
{
    return m_channelId;
}

void PodcastEpisodesModel::refreshEpisode(PodcastEpisode *episode)
{
    qDebug() << "Saving episode to DB. Play filename:" << episode->playFilename();
    m_sqlmanager->updatePodcastInDB(episode);
}

void PodcastEpisodesModel::removeAll()
{
    qDebug()  << "Removing all episodes from the model.";
    foreach(PodcastEpisode *episode, m_episodes) {
        episode->deleteDownload();
        delete episode;
    }

    beginRemoveRows(QModelIndex(), 0, m_episodes.size()-1);
    m_episodes.clear();
    endRemoveRows();
}

QHash<int, QByteArray> PodcastEpisodesModel::roleNames() const
{
    return m_roles;
}

QList<PodcastEpisode *> PodcastEpisodesModel::unplayedEpisodes()
{
    QList<PodcastEpisode *> episodes;

    if (m_episodes.isEmpty()) {
        return episodes;
    }

    for (int i=0; i<m_episodes.length(); i++) {
        PodcastEpisode *episode = m_episodes.at(i);
        if (!episode->playFilename().isEmpty() &&
            episode->episodeState() == "downloaded") {
            episodes << episode;
        }
    }

    return episodes;
}

QList<PodcastEpisode *> PodcastEpisodesModel::episodes()
{
    return m_episodes;
}

void PodcastEpisodesModel::cleanOldEpisodes(int keepNumEpisodes, bool keepUnplayed)
{
    if (keepNumEpisodes == 0) {
        return;
    }

    QList<PodcastEpisode *> episodesToDel;
    for (int i=keepNumEpisodes; i<m_episodes.length(); i++) {
        PodcastEpisode *episode = m_episodes.at(i);

        // If we want to keep unplayed episodes...
        if (keepUnplayed) {
            // Check if the episode filename is not empty (we have an episode)
            // and that the last played is not set, then we know we have an unplayed and
            // downloaded episode that we want to keep.
            if (!episode->playFilename().isEmpty() &&
                !episode->lastPlayed().isValid()) {
                continue;
            }
        }

        // Otherwise, delete the episode and the download
        if (!episode->playFilename().isEmpty()) {
            episode->deleteDownload();
        }

        episodesToDel << episode;
    }

    foreach(PodcastEpisode *episode, episodesToDel) {
        delEpisode(episode);
    }
}

