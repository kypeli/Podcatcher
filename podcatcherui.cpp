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
#include <QTimer>

#include <QtDebug>

#include "contentaction/contentaction.h"

#include "podcatcherui.h"
#include "podcastepisodesmodel.h"
#include "podcastepisodesmodelfactory.h"
#include "podcastglobals.h"

PodcatcherUI::PodcatcherUI()
{
    m_channelsModel = m_pManager.podcastChannelsModel();
    rootContext()->setContextProperty("channelsModel", m_channelsModel);
    rootContext()->setContextProperty("ui", this);
    setSource(QUrl("qrc:/qml/main.qml"));
//    setSource(QUrl("/opt/Podcatcher/bin/qml/main.qml"));   // TODO: Take .qrc into use again! Remember to fix the .pro file too!

    modelFactory = PodcastEpisodesModelFactory::episodesFactory();

    connect(&m_pManager, SIGNAL(showInfoBanner(QString)),
            this, SIGNAL(showInfoBanner(QString)));

    connect(&m_pManager, SIGNAL(downloadingPodcasts(bool)),
            this, SIGNAL(downloadingPodcasts(bool)));

    QObject *rootDeclarativeItem = rootObject();

    connect(rootDeclarativeItem, SIGNAL(showChannel(QString)),
            this, SLOT(onShowChannel(QString)));

    connect(rootDeclarativeItem, SIGNAL(deleteChannel(QString)),
            this, SLOT(onDeleteChannel(QString)));

    connect(rootDeclarativeItem, SIGNAL(downloadPodcast(int, int)),
            this, SLOT(onDownloadPodcast(int, int)));

    connect(rootDeclarativeItem, SIGNAL(playPodcast(int, int)),
            this, SLOT(onPlayPodcast(int, int)));

    connect(rootDeclarativeItem, SIGNAL(refreshAllEpisodes(int)),
            this, SLOT(onRefreshEpisodes(int)));

    connect(rootDeclarativeItem, SIGNAL(cancelDownload(int, int)),
            this, SLOT(onCancelDownload(int, int)));

    connect(rootDeclarativeItem, SIGNAL(cancelQueue(int, int)),
            this, SLOT(onCancelQueueing(int, int)));

    connect(rootDeclarativeItem, SIGNAL(allListened(QString)),
            this, SLOT(onAllListened(QString)));

    connect(rootDeclarativeItem, SIGNAL(deleteDownloaded(int, int)),
            this, SLOT(onDeletePodcast(int, int)));

    m_pManager.refreshAllChannels();   // Refresh all feeds and download new episodes.

    QTimer::singleShot(10000, &m_pManager, SLOT(cleanupEpisodes()));
}

void PodcatcherUI::addPodcast(QString rssUrl, QString logoUrl)
{
    if (!logoUrl.isEmpty()) {
        qDebug() << "Got logo from the subscription feed:" << logoUrl;
        logoCache.insert(rssUrl, logoUrl);
    }

    QString newPodcast = rssUrl.toLower();
    if (newPodcast.indexOf(QString("http://")) != 0) {
        newPodcast.prepend("http://");
    }
    qDebug() << "User entered podcast to fetch: " << rssUrl << " - fetching " << newPodcast;

    m_pManager.requestPodcastChannel(QUrl(newPodcast), logoCache);

    emit showInfoBanner("Fetching channel information...");
}

bool PodcatcherUI::isDownloading()
{
    return m_pManager.isDownloading();
}

void PodcatcherUI::onShowChannel(QString channelId)
{
    qDebug() << "Opening channel" << channelId;

    PodcastChannel *channel = m_pManager.podcastChannel(channelId.toInt());
    if (channel == 0) {
        qWarning() << "Got NULL channel pointer!";
        return;
    }
    if (channel->description().length() > 270) {
        QString oldDesc = channel->description();
        oldDesc.truncate(270);
        QString newDesc = QString("%1%2").arg(oldDesc).arg("...");

        channel->setDescription(newDesc);
    }

   rootContext()->setContextProperty("channel", channel);

   PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channel->channelDbId());   // FIXME: Do not expose DB id.
   rootContext()->setContextProperty("episodesModel", episodesModel);
}


void PodcatcherUI::onDownloadPodcast(int channelId, int index)
{
    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId);
    PodcastEpisode *episode = episodesModel->episode(index);
    m_pManager.downloadPodcast(episode);
}

void PodcatcherUI::onPlayPodcast(int channelId, int index)
{
    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId);
    if (episodesModel == 0) {
        qWarning() << "Could not get episodes model. Cannot play episode.";
        return;
    }

    PodcastEpisode *episode = episodesModel->episode(index);

    QUrl file = QUrl::fromLocalFile(episode->playFilename());

    // If the file doens't exist, update the state in the DB
    // and do nothing more.
    QFile checkFile(file.toLocalFile());
    if (!checkFile.exists()) {
        qDebug() << "Original file " << file.toLocalFile() << " doesn't exist anymore.";
        episode->setPlayFilename("");
        episode->setState(PodcastEpisode::GetState);
        episode->setLastPlayed(QDateTime());
        episodesModel->refreshEpisode(episode);

        emit showInfoBanner("Podcast episode not found.");
        return;
    }

    episode->setLastPlayed(QDateTime::currentDateTime());
    episodesModel->refreshEpisode(episode);
    m_channelsModel->refreshChannel(channelId);

    qDebug() << "Launching the music player for file" << file;

    ContentAction::Action launchPlayerAction;
    launchPlayerAction = ContentAction::Action::defaultActionForFile(file);
    if (!launchPlayerAction.isValid()) {
        qDebug() << "Action for file is not valid!";
        emit showInfoBanner("I am sorry! Could not launch audio player for this podcast.");
    } else {
        launchPlayerAction.trigger();
    }
}

void PodcatcherUI::onRefreshEpisodes(int channelId)
{
    PodcastChannel *channel = m_pManager.podcastChannel(channelId);
    qDebug() << "Refreshing channel: " << channelId << channel->title();
    if (channel == 0) {
        qWarning() << "Got NULL episode!";
        return;
    }
    m_pManager.refreshPodcastChannelEpisodes(channel, true);
}

void PodcatcherUI::onCancelQueueing(int channelId, int index)
{
    qDebug() << "Cancel queueing at " << channelId << index;
    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId);
    PodcastEpisode *episode = episodesModel->episode(index);
    episode->setState(PodcastEpisode::GetState);

    m_pManager.cancelQueueingPodcast(episode);

    episodesModel->refreshEpisode(episode);
}

void PodcatcherUI::onCancelDownload(int channelId, int index)
{
    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId);
    PodcastEpisode *episode = episodesModel->episode(index);
    m_pManager.cancelDownloadPodcast(episode);
    episodesModel->refreshEpisode(episode);
}

void PodcatcherUI::onDeleteChannel(QString channelId)
{
    qDebug() << "Yep, lets delete the channel and some episodes from channel" << channelId;
    m_pManager.removePodcastChannel(channelId.toInt());

}

void PodcatcherUI::onAllListened(QString channelId)
{
    qDebug() << "Yep, mark all listened on channel: " << channelId;

    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId.toInt());
    QList<PodcastEpisode *> unplayed = episodesModel->unplayedEpisodes();
    foreach(PodcastEpisode *episode, unplayed) {
        episode->setAsPlayed();
        episodesModel->refreshEpisode(episode);
    }

    m_channelsModel->refreshChannel(channelId.toInt());
}

void PodcatcherUI::onDeletePodcast(int channelId, int index)
{
    qDebug() << "Deleting the locally downloaded podcast:" << channelId << index;

    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId);
    PodcastEpisode *episode = episodesModel->episode(index);
    qDebug() << "Episode name:" << episode->title() << episode->playFilename();
    episode->deleteDownload();
    episodesModel->refreshEpisode(episode);

    m_channelsModel->refreshChannel(channelId);
}

void PodcatcherUI::deletePodcasts(int channelId)
{
    m_pManager.deleteAllDownloadedPodcasts(channelId);
    m_channelsModel->refreshChannel(channelId);
}

bool PodcatcherUI::isLiteVersion()
{
#ifdef LITE
    return true;
#else
    return false;
#endif
}

QString PodcatcherUI::versionString()
{
    QString tmpVersion(QString::number(PODCATCHER_VERSION));
    QString version = tmpVersion.at(0);
    version.append(".").append(tmpVersion.at(1));
    version.append(".");
    for (int i=2; i<tmpVersion.length(); i++) {
        version.append(tmpVersion.at(i));
    }

    return version;
}

void PodcatcherUI::refreshChannels()
{
    m_pManager.refreshAllChannels();
}

