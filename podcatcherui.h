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
#ifndef PODCATCHERUI_H
#define PODCATCHERUI_H

#include <QObject>
#include <QList>
#include <QtDeclarative>

#include "podcastmanager.h"
#include "podcastchannelsmodel.h"

class PodcatcherUI : public QDeclarativeView
{
    Q_OBJECT
public:
    explicit PodcatcherUI();

    Q_INVOKABLE void addPodcast(QString rssUrl, QString logoUrl = QString());
    Q_INVOKABLE void deletePodcasts(int channelId);
    Q_INVOKABLE bool isDownloading();
    Q_INVOKABLE bool isLiteVersion();
    Q_INVOKABLE QString versionString();

    void downloadNewEpisodes(QString channelId);

signals:
    void showInfoBanner(QString text);
    void downloadedBytesUpdated(int bytes);
    void downloadingPodcasts(bool downloading);

public slots:

private slots:
    void onShowChannel(QString channelId);
    void onRefreshEpisodes(int channelId);
    void onDownloadPodcast(int channelId, int index);
    void onPlayPodcast(int channelId, int index);
    void onCancelDownload(int channelId, int index);
    void onCancelQueueing(int channelId, int index);
    void onDeleteChannel(QString channelId);
    void onAllListened(QString channelId);
    void onDeletePodcast(int channelId, int index);

private:
    PodcastManager m_pManager;
    PodcastChannelsModel *m_channelsModel;
    PodcastEpisodesModelFactory *modelFactory;
    QMap<QString, QString> logoCache;
};

#endif // PODCATCHERUI_H
