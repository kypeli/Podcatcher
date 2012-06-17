#include "podcastmanagermeego.h"

PodcastManagerMeego::PodcastManagerMeego(QObject *parent) :
    PodcastManager(parent)
{
    // Get the current settings values.
    qDebug() << "Current settings: ";

    m_autoDlConf = new GConfItem("/apps/ControlPanel/Podcatcher/autodownload", this);
    m_autodownloadOnSettings = m_autoDlConf->value().toBool();
    qDebug() << "  * Autodownload: on" << m_autodownloadOnSettings;

    m_autoDlNumConf = new GConfItem("/apps/ControlPanel/Podcatcher/autodownload_num", this);
    m_autodownloadNumSettings = m_autoDlNumConf->value().toInt();
    qDebug() << "  * Automatically get episodes: " << m_autodownloadNumSettings;

    m_keepNumEpisodesConf = new GConfItem("/apps/ControlPanel/Podcatcher/keep_episodes", this);
    m_keepNumEpisodesSettings = m_keepNumEpisodesConf->value().toInt();
    qDebug() << "  * Autodelete episodes after num days:" << m_keepNumEpisodesSettings;

    m_autoDelUnplayedConf = new GConfItem("/apps/ControlPanel/Podcatcher/keep_unplayed", this);
    m_autoDelUnplayedSettings = m_autoDelUnplayedConf->value().toBool();
    qDebug() << "  * Keep unplayed episodes:" << m_autoDelUnplayedSettings;

    // Connect to the changed signals for each of the settings above.
    connect(m_autoDlConf, SIGNAL(valueChanged()),
            this, SLOT(onAutodownloadOnChanged()));
    connect(m_autoDlNumConf, SIGNAL(valueChanged()),
            this, SLOT(onAutodownloadNumChanged()));
    connect(m_keepNumEpisodesConf, SIGNAL(valueChanged()),
            this, SLOT(onAutodelDaysChanged()));
    connect(m_autoDelUnplayedConf, SIGNAL(valueChanged()),
            this, SLOT(onAutodelUnplayedChanged()));
}

void PodcastManagerMeego::onAutodownloadOnChanged()
{
    qDebug() << "Setting changed: autodl: " << QVariant(m_autoDlConf->value()).toBool();

    m_autodownloadOnSettings = QVariant(m_autoDlConf->value()).toBool();
}

void PodcastManagerMeego::onAutodownloadNumChanged()
{
    qDebug() << "Setting changed: autodownload number of episodes: " << QVariant(m_autoDlNumConf->value()).toInt();

    m_autodownloadNumSettings = QVariant(m_autoDlNumConf->value()).toInt();
}

void PodcastManagerMeego::onAutodelDaysChanged()
{
    qDebug() << "Setting changed: autodelete after days: " << QVariant(m_keepNumEpisodesConf->value()).toInt();

    m_keepNumEpisodesSettings = QVariant(m_keepNumEpisodesConf->value()).toInt();
}

void PodcastManagerMeego::onAutodelUnplayedChanged()
{
    qDebug() << "Setting changed: autodelete unplayed episodes: " << QVariant(m_autoDelUnplayedConf->value()).toBool();

    m_autoDelUnplayedSettings = QVariant(m_autoDelUnplayedConf->value()).toBool();
}


void PodcastManagerMeego::cleanupEpisodes()
{
    if (m_keepNumEpisodesSettings == 0) {
        // Keep all episodes.
        return;
    }

    m_cleanupChannels = m_channelsModel->channels();
    if (m_cleanupChannels.isEmpty()) {
        return;
    }

    PodcastEpisodesModel *episodesModel = m_episodeModelFactory->episodesModel((m_cleanupChannels.takeLast())->channelDbId());

    QFuture<void> future = QtConcurrent::run(episodesModel,
                                             &PodcastEpisodesModel::cleanOldEpisodes,
                                             m_keepNumEpisodesSettings,
                                             m_autoDelUnplayedSettings);

    m_futureWatcher.setFuture(future);
    connect(&m_futureWatcher, SIGNAL(finished()),
            this, SLOT(onCleanupEpisodeModelFinished()));
}

void PodcastManagerMeego::onCleanupEpisodeModelFinished()
{
    if (m_cleanupChannels.isEmpty()) {
        // All channels cleaned up.
        return;
    }

    PodcastEpisodesModel *episodesModel = m_episodeModelFactory->episodesModel((m_cleanupChannels.takeLast())->channelDbId());

    QFuture<void> future = QtConcurrent::run(episodesModel,
                                             &PodcastEpisodesModel::cleanOldEpisodes,
                                             m_keepNumEpisodesSettings,
                                             m_autoDelUnplayedSettings);

    m_futureWatcher.setFuture(future);
}
