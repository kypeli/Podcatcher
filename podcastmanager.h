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
#ifndef PODCASTMANAGER_H
#define PODCASTMANAGER_H

#include <QObject>
#include <QNetworkAccessManager>
#include <QUrl>
#include <QMap>
#include <QVariant>

#include <gq/gconfitem.h>

#include "podcastchannel.h"
#include "podcastepisode.h"
#include "podcastepisodesmodel.h"
#include "podcastepisodesmodelfactory.h"

class PodcastSQLManager;
class QNetworkReply;
class PodcastManager : public QObject
{
    Q_OBJECT
public:
    explicit PodcastManager(QObject *parent = 0);

    /**
     * Request that the PodcastManager retrieves a new podcast stream
     * RSS file from the network. The method will return immediately, and
     * the resulting PodcastStream will be returned in the signal
     * podcastStreamRetrieved(PodcastStream *podcastStream).
     *
     * The PodcastManager will set itself as the parent for the returned
     * PodcastStream and hence will be deleted when the manager is deleted.
     */
    void requestPodcastChannel(const QUrl &rssUrl, const QMap<QString, QString> &logoCache = QMap<QString, QString>());

    void refreshPodcastChannelEpisodes(PodcastChannel *channel, bool forceNetworkUpdate = false);
    void downloadNewEpisodes(int channelId);

    QList<PodcastChannel *> podcastChannels();
    static QList<QObject *> toPodcastChannelsModel(QList<PodcastChannel *> list);
    PodcastChannel* podcastChannel(int id);
    void removePodcastChannel(int channelId);

    void downloadPodcast(PodcastEpisode *episode);
    void cancelDownloadPodcast(PodcastEpisode *episode);
    void cancelQueueingPodcast(PodcastEpisode *episode);
    void deleteAllDownloadedPodcasts(int channelId);

    static QString redirectedRequest(QNetworkReply *reply);

    static bool isConnectedToWiFi();

signals:
    void podcastChannelReady(PodcastChannel *podcast);
    void podcastChannelSaved();

    void podcastEpisodesRefreshed(QUrl podcastUrl);

    void parseChannelFailed();
    void parseEpisodesFailed();

    void podcastEpisodeDownloaded(PodcastEpisode *episode);
    void showInfoBanner(QString text);
    void downloadingPodcasts(bool downloading);

public slots:

private slots:
   void savePodcastChannel(PodcastChannel *channel);
   void onPodcastChannelCompleted();
   void onPodcastChannelLogoCompleted();

   void onPodcastEpisodesRequestCompleted();

   void onPodcastEpisodeDownloaded(PodcastEpisode *episode);
   void onPodcastEpisodeDownloadFailed(PodcastEpisode* episode);

   void onAutodownloadOnChanged();

private:
   void executeNextDownload();
   QNetworkReply * downloadChannelLogo(QString logoUrl);
   void insertChannelForNetworkReply(QNetworkReply *reply, PodcastChannel *channel);
   PodcastChannel * channelForNetworkReply(QNetworkReply *reply);
   bool savePodcastEpisodes(PodcastChannel *channel);

   // We need multiple QNAMs to be able to do concurrent downloads.   
   QNetworkAccessManager *m_networkManager;
   QNetworkAccessManager *m_dlNetworkManager;  // Share this between all the episodes;
   PodcastSQLManager     *sqlmanager;

   QMap<QNetworkReply*, PodcastChannel *> m_channelNetworkRequestCache;
   QMap<int, PodcastChannel *> m_channelsCache;

   PodcastEpisodesModelFactory *m_episodeModelFactory;
   QMap<QString, PodcastChannel *> channelRequestMap;

   QList<PodcastEpisode *> m_episodeDownloadQueue;
   bool m_isDownloading;
   QMap<QString, QString> m_logoCache;

   GConfItem *m_autodlSettingKey;
   bool m_autodownloadOn;

};

#endif // PODCASTMANAGER_H
