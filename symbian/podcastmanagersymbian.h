#ifndef PODCASTMANAGERSYMBIAN_H
#define PODCASTMANAGERSYMBIAN_H

#include "podcastmanager.h"

class PodcastManagerSymbian : public PodcastManager
{
    Q_OBJECT
public:
    PodcastManagerSymbian(QObject *parent);

public slots:
    void cleanupEpisodes();
};

#endif // PODCASTMANAGERSYMBIAN_H
