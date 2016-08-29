/**
 * This file is part of Podcatcher for Sailfish OS.
 * Author: Johan Paul (johan.paul@gmail.com)
 *
 * Podcatcher for Sailfish OS is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Podcatcher for Sailfish OS is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Podcatcher for Sailfish OS.  If not, see <http://www.gnu.org/licenses/>.
 */
#include <QTimer>
#include <QtQuick>
#include <QtDebug>

//#include <contentaction5/contentaction.h>


#include "podcatcherui.h"
#include "podcastepisodesmodel.h"
#include "podcastepisodesmodelfactory.h"
#include "podcastglobals.h"

PodcatcherUI::PodcatcherUI()
{
    view = SailfishApp::createView();
    m_channelsModel = m_pManager.podcastChannelsModel();
    view->rootContext()->setContextProperty("channelsModel", m_channelsModel);
    view->rootContext()->setContextProperty("ui", this);

    view->setSource(SailfishApp::pathTo("qml/Podcatcher.qml"));


    modelFactory = PodcastEpisodesModelFactory::episodesFactory();

    connect(&m_pManager, SIGNAL(showInfoBanner(QString)),
            this, SIGNAL(showInfoBanner(QString)));

    connect(&m_pManager, SIGNAL(downloadingPodcasts(bool)),
            this, SIGNAL(downloadingPodcasts(bool)));

    connect(&m_pManager, SIGNAL(downloadingPodcasts(bool)),
            this, SLOT(onDownloadingPodcast(bool)));

    QObject *rootDeclarativeItem = view->rootObject();

    connect(rootDeclarativeItem, SIGNAL(showChannel(QString)),
            this, SLOT(onShowChannel(QString)));

    connect(rootDeclarativeItem, SIGNAL(deleteChannel(QString)),
            this, SLOT(onDeleteChannel(QString)));

    connect(rootDeclarativeItem, SIGNAL(downloadPodcast(int, int)),
            this, SLOT(onDownloadPodcast(int, int)));

    connect(rootDeclarativeItem, SIGNAL(playPodcast(int, int)),
            this, SLOT(onPlayPodcast(int, int)));

    connect(rootDeclarativeItem, SIGNAL(cancelDownload(int, int)),
            this, SLOT(onCancelDownload(int, int)));

    connect(rootDeclarativeItem, SIGNAL(cancelQueue(int, int)),
            this, SLOT(onCancelQueueing(int, int)));

    connect(rootDeclarativeItem, SIGNAL(allListened(QString)),
            this, SLOT(onAllListened(QString)));

    connect(rootDeclarativeItem, SIGNAL(deleteDownloaded(int, int)),
            this, SLOT(onDeletePodcast(int, int)));

    connect(rootDeclarativeItem, SIGNAL(startStreaming(int, int)),
            this, SLOT(onStartStreaming(int, int)));

    m_pManager.refreshAllChannels();   // Refresh all feeds and download new episodes.

    QTimer::singleShot(10000, &m_pManager, SLOT(cleanupEpisodes()));

    connect(rootDeclarativeItem, SIGNAL(autoDownloadChanged(int, bool)),
            this, SLOT(onAutoDownloadChanged(int, bool)));

    view->show();
    qDebug() << "Paths:\n" << PODCATCHER_PATH <<"\n" << PODCATCHER_PODCAST_DLDIR;
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

   view->rootContext()->setContextProperty("channel", channel);

   PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channel->channelDbId());   // FIXME: Do not expose DB id.
   view->rootContext()->setContextProperty("episodesModel", episodesModel);
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

    /*
    ContentAction::Action launchPlayerAction;
    launchPlayerAction = ContentAction::Action::defaultActionForFile(file);
    if (!launchPlayerAction.isValid()) {
        qDebug() << "Action for file is not valid!";
        emit showInfoBanner("I am sorry! Could not launch audio player for this podcast.");
    } else {
        launchPlayerAction.trigger();
    }*/

    if (! QDesktopServices::openUrl(file)){
         emit showInfoBanner("I am sorry! Could not launch audio player for this podcast.");
    }
}

void PodcatcherUI::onDownloadingPodcast(bool _isDownloading)
{
    qDebug() << "isDownloading changed" << _isDownloading;
    emit isDownloadingChanged(_isDownloading);
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

void PodcatcherUI::onStartStreaming(int channelId, int index)
{
    qDebug() << "Requested streaming of epsiode:" << channelId << index;

    PodcastEpisodesModel *episodesModel = modelFactory->episodesModel(channelId);
    PodcastEpisode *episode = episodesModel->episode(index);

    connect(episode, SIGNAL(streamingUrlResolved(QString, QString)),
            this, SLOT(onStreamingUrlResolved(QString, QString)));

    qDebug() << "Episode url:" << episode->downloadLink() << ", need to find a MP3 file for this link.";
    episode->getAudioUrl();
}

void PodcatcherUI::onStreamingUrlResolved(QString streamUrl, QString streamTitle)
{
    PodcastEpisode *episode = qobject_cast<PodcastEpisode *>(sender());
    disconnect(episode, SIGNAL(streamingUrlResolved(QString, QString)),
            this, SLOT(onStreamingUrlResolved(QString, QString)));

    if (streamUrl.isEmpty()) {
        emit showInfoBanner("Unable to stream podcast.");
    } else {
        emit streamingUrlResolved(streamUrl, streamTitle);
    }
}

void PodcatcherUI::onAutoDownloadChanged(int channelId, bool autoDownload)
{
    PodcastChannel *channel = m_channelsModel->podcastChannelById(channelId);
    channel->setAutoDownloadOn(autoDownload);
    m_channelsModel->updateChannel(channel);
}

void PodcatcherUI::importFromGPodder(QString username, QString password)
{
    m_pManager.fetchSubscriptionsFromGPodder(username, password);
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


