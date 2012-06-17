#ifndef PODCASTEPISODEMEEGO_H
#define PODCASTEPISODEMEEGO_H

#include "podcastepisode.h"

class PodcastEpisodeMeego : public PodcastEpisode
{
    Q_OBJECT
public:
    PodcastEpisodeMeego(QObject *parent);

    void downloadEpisode();

private slots:
    void onDownloadProgress(qint64 bytesReceived, qint64 bytesTotal);
    void onPodcastEpisodeDownloadCompleted();

};

#endif // PODCASTEPISODEMEEGO_H
