#ifndef PODCASTMANAGERMEEGO_H
#define PODCASTMANAGERMEEGO_H

#include "podcastmanager.h"

#include <QFutureWatcher>

#include <qg/gconfitem.h>

class PodcastManagerMeego : public PodcastManager
{
    Q_OBJECT
public:
    PodcastManagerMeego(QObject *parent);

public slots:
    void cleanupEpisodes();     // This is a slot, since it is called from Podcatcher UI constructor with single shot timer.

private slots:
    void onAutodownloadOnChanged();
    void onAutodownloadNumChanged();
    void onAutodelDaysChanged();
    void onAutodelUnplayedChanged();
    void onCleanupEpisodeModelFinished();

private:
    GConfItem *m_autoDlConf;
    GConfItem *m_autoDlNumConf;
    GConfItem *m_keepNumEpisodesConf;
    GConfItem *m_autoDelUnplayedConf;

    QFutureWatcher<void> m_futureWatcher;
};

#endif // PODCASTMANAGERMEEGO_H
