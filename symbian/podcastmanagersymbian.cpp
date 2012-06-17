#include <QtDebug>

#include "podcastmanagersymbian.h"

PodcastManagerSymbian::PodcastManagerSymbian(QObject *parent) :
    PodcastManager(parent)
{
    m_autodownloadOnSettings = true;
    m_autodownloadNumSettings = 1;
    m_keepNumEpisodesSettings = 10;
    m_autoDelUnplayedSettings = false;

}

void PodcastManagerSymbian::cleanupEpisodes()
{
    qDebug() << "Cleaning not implemented in Symbian.";
}


