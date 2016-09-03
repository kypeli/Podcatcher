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
#ifndef PODCASTEPISODESMODEL_H
#define PODCASTEPISODESMODEL_H

#include <QAbstractListModel>
#include <QList>

#include "podcastepisode.h"
#include "podcastsqlmanager.h"

class QDateTime;
class PodcastEpisodesModel : public QAbstractListModel
{
    Q_OBJECT
public:
    enum EpisodeRoles {
        TitleRole = Qt::UserRole + 1,
        DbidRole,
        PubRole,
        DescriptionRole,
        StateRole,
        TotalDownloadRole,
        AlreadyDownloaded,
        LastTimePlayedRole
    };

    PodcastEpisodesModel(int channelId, QObject *parent = 0);
    ~PodcastEpisodesModel();

    void setChannelId(int id);
    int channelId();

    int rowCount(const QModelIndex & parent = QModelIndex()) const;
    QVariant data(const QModelIndex & index, int role = Qt::DisplayRole) const;

    void addEpisode(PodcastEpisode *episode);
    void addEpisodes(QList<PodcastEpisode *> episode);

    void delEpisode(PodcastEpisode *episode);
    void delEpisode(int index, PodcastEpisode *episode);

    PodcastEpisode *episode(int index);

    PodcastEpisodesModel * episodesModel(int channelId);
    void refreshModel();

    void refreshEpisode(PodcastEpisode *episode);

    QList<PodcastEpisode *> episodes();
    QList<PodcastEpisode *> undownloadedEpisodes(int maxUndownloadedEpisodes);
    QList<PodcastEpisode *> unplayedEpisodes();

    void cleanOldEpisodes(int keepNumEpisodes, bool keepUnplayed);
    void removeAll();
    QHash<int, QByteArray> roleNames() const;

signals:
    void episodeBeingDeleted(PodcastEpisode *episode);

public slots:

private slots:
    void onEpisodeChanged();

private:
    PodcastSQLManager      *m_sqlmanager;
    QList<PodcastEpisode *> m_episodes;
    int m_channelId;
    QDateTime               m_latestEpisodeTimestamp;
    QHash<int, QByteArray>  m_roles;

};

#endif // PODCASTEPISODESMODEL_H
