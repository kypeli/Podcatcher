#include <QFile>
#include <QFileInfo>
#include <QtDebug>

#include "podcastepisodemeego.h"
#include "podcastglobals.h"

PodcastEpisodeMeego::PodcastEpisodeMeego(QObject *parent) :
    PodcastEpisode(parent)
{
}

void PodcastEpisodeMeego::downloadEpisode()
{
    qDebug() << "Downloading podcast:" << m_downloadLink;

    if (m_dlNetworkManager == 0) {
        qWarning() << "No QNetworkAccessManager specified for this episode. Cannot proceed.";
        return;
    }


    QUrl downloadUrl(m_downloadLink);
    if (!downloadUrl.isValid()) {
        qWarning() << "Provided podcast download URL is not valid.";
        return;
    }

    QNetworkRequest request;
    request.setUrl(downloadUrl);

    m_currentDownload = m_dlNetworkManager->get(request);

    connect(m_currentDownload, SIGNAL(finished()),
            this, SLOT(onPodcastEpisodeDownloadCompleted()));
    connect(m_currentDownload, SIGNAL(downloadProgress(qint64,qint64)),
            this, SLOT(onDownloadProgress(qint64, qint64)));

}

void PodcastEpisodeMeego::onDownloadProgress(qint64 bytesReceived, qint64 bytesTotal)
{
    Q_UNUSED(bytesTotal)

    m_bytesDownloaded = bytesReceived;
    m_downloadSize = bytesTotal;
    emit episodeChanged();
}

void PodcastEpisodeMeego::onPodcastEpisodeDownloadCompleted()
{
    QNetworkReply *reply = qobject_cast<QNetworkReply *>(sender());

    QString redirectedUrl = PodcastManager::redirectedRequest(reply);
    if (!redirectedUrl.isEmpty()) {
        m_downloadLink = redirectedUrl;
        reply->deleteLater();
        downloadEpisode();
        return;
    }

    if (reply->error() != QNetworkReply::NoError) {
        qWarning() << "Download of podcast was not succesfull: " << reply->errorString();
        reply->deleteLater();
        emit podcastEpisodeDownloadFailed(this);
        return;
    }

    QString downloadPath;
    downloadPath = PODCATCHER_PODCAST_DLDIR;

    // Store downloaded podcasts in "$HOME/.sounds/podcasts"
    QDir dirpath(downloadPath);
    if (!dirpath.exists()) {
        dirpath.mkpath(downloadPath);
    }

    QString path = reply->url().path();
    QString filename = QFileInfo(path).fileName();

    QFile file(downloadPath + filename);
    if (!file.open(QIODevice::WriteOnly)) {
        fprintf(stderr, "Could not open %s for writing: %s\n",
                qPrintable(filename),
                qPrintable(file.errorString()));
        return;
    }

    file.write(reply->readAll());
    file.close();

    QFileInfo fileInfo(file);
    m_playFilename = fileInfo.absoluteFilePath();

    qDebug() << "Podcast downloaded: " << m_playFilename;

    emit podcastEpisodeDownloaded(this);
    reply->deleteLater();
    m_dlNetworkManager = 0;
}
