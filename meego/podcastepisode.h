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
#ifndef PODCASTEPISODE_H
#define PODCASTEPISODE_H

#include <QObject>
#include <QString>
#include <QDateTime>
#include <QNetworkAccessManager>

class PodcastEpisode : public QObject
{
    Q_OBJECT
public:
    enum EpisodeStates {
        GetState = 0,
        QueuedState,
        DownloadingState,
        DownloadedState,
        CanceledState,
        PlayedState
    };

    explicit PodcastEpisode(QObject *parent = 0);

    void downloadEpisode();

    void setDbId(int id);
    void setChannelId(int id);
    void setTitle(const QString &title);
    void setDownloadLink(const QString &downloadLink);
    void setPlayFilename(const QString &playFilename);
    void setDescription(const QString &desc);
    void setPubTime(const QDateTime &pubDate);
    void setDuration(const QString &duration);
    void setDownloadSize(qint64 downloadSize);
    void setState(EpisodeStates newState);
    void setDownloadManager(QNetworkAccessManager *qnam);
    void setLastPlayed(const QDateTime &lastPlayed);
    void setHasBeenCanceled(bool canceled);

    int dbid() const;
    int channelid() const;
    QString title() const;
    QString downloadLink() const;
    QString playFilename() const;
    QString description() const;
    QDateTime pubTime() const;
    QString duration() const;
    qint64 downloadSize() const;
    qint64 alreadyDownloaded();
    QString episodeState() const;
    QDateTime lastPlayed() const;
    bool hasBeenCanceled() const;
    void getAudioUrl();

    void cancelCurrentDownload();
    void deleteDownload();
    void setAsPlayed();

signals:
    void episodeChanged();
    void podcastEpisodeDownloaded(PodcastEpisode *episode);
    void podcastEpisodeDownloadFailed(PodcastEpisode *episode);
    void downloadedBytesUpdated(int bytes);
    void streamingUrlResolved(QString url, QString title);

public slots:

private slots:
    void onDownloadProgress(qint64 bytesReceived, qint64 bytesTotal);
    void onPodcastEpisodeDownloadCompleted();
    void onAudioUrlMetadataChanged();

private:
    bool isValidAudiofile(QNetworkReply *reply) const;
//    bool isOnlyWebsiteUrl() const;

    int m_dbid;
    int m_channelid;
    QString m_title;
    QString m_downloadLink;
    QString m_playFilename;
    QString m_description;
    QDateTime m_pubDateTime;
    QString m_duration;
    EpisodeStates m_state;
    qint64 m_bytesDownloaded;
    qint64 m_downloadSize;
    QDateTime m_lastPlayed;
    bool m_hasBeenCanceled;

    QNetworkAccessManager *m_dlNetworkManager;
    QNetworkReply *m_currentDownload;

    QNetworkAccessManager *m_streamResolverManager;
    int m_streamResolverTries;
};

#endif // PODCASTEPISODE_H
