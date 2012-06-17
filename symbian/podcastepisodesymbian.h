#ifndef PODCASTEPISODESYMBIAN_H
#define PODCASTEPISODESYMBIAN_H

#include <QFile>

#include "podcastepisode.h"

class PodcastEpisodeSymbian : public PodcastEpisode
{
    Q_OBJECT
public:
    PodcastEpisodeSymbian(QObject *parent);

    void downloadEpisode();

signals:
    void showInfoBanner(QString text);

private slots:
    void onDownloadProgress(qint64 bytesReceived, qint64 bytesTotal);
    void onPodcastEpisodeDownloadCompleted();
    void onReadyRead();

private:
    bool isMassStorageAvailable();

    QFile *m_downloadFile;
};

#endif // PODCASTEPISODESYMBIAN_H
