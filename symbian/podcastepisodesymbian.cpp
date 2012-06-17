#include <QUrl>

#include <QtDebug>
#include <QDir>
#include <QNetworkReply>
#include <QNetworkRequest>
#include <QFile>

#include <QSystemStorageInfo>

#include "podcastmanager.h"
#include "podcastepisodesymbian.h"
#include "podcastglobals.h"

using namespace QtMobility;

PodcastEpisodeSymbian::PodcastEpisodeSymbian(QObject *parent = 0) :
    PodcastEpisode(parent)
{
}

void PodcastEpisodeSymbian::downloadEpisode()
{
    qDebug() << "Downloading episode for Symbian: " + m_downloadLink;

    if (isMassStorageAvailable() == false) {
        qWarning() << "No memory card inserted. Cannot proceed.";
        emit showInfoBanner("Please insert memory card to download episodes.");
        return;
    }

    if (m_dlNetworkManager == 0) {
        qWarning() << "No QNetworkAccessManager specified for this episode. Cannot proceed.";
        return;
    }


    QUrl downloadUrl(m_downloadLink);
    if (!downloadUrl.isValid()) {
        qWarning() << "Provided podcast download URL is not valid.";
        return;
    }


    QString downloadPath;
    downloadPath = PODCATCHER_PODCAST_DLDIR;

    QDir dirpath(downloadPath);
    if (!dirpath.exists()) {
        dirpath.mkpath(downloadPath);
    }

    QNetworkRequest request;
    request.setUrl(downloadUrl);

    m_currentDownload = m_dlNetworkManager->get(request);

    QString path = m_currentDownload->url().path();
    QString filename = QFileInfo(path).fileName();
    m_downloadFile = new QFile(downloadPath + filename);

    // Overwrite any file with the same name
    // We are opening the wile in Append mode which will not
    // overwrite the file.
    if (m_downloadFile->exists()) {
        m_downloadFile->remove();
    }

    if (!m_downloadFile->open(QIODevice::Append)) {
        fprintf(stderr, "Could not open %s for writing: %s\n",
                qPrintable(filename),
                qPrintable(m_downloadFile->errorString()));
        return;
    }

    connect(m_currentDownload, SIGNAL(finished()),
            this, SLOT(onPodcastEpisodeDownloadCompleted()));
    connect(m_currentDownload, SIGNAL(downloadProgress(qint64,qint64)),
            this, SLOT(onDownloadProgress(qint64, qint64)));
    connect(m_currentDownload, SIGNAL(readyRead()),
            this, SLOT(onReadyRead()));
}

void PodcastEpisodeSymbian::onDownloadProgress(qint64 bytesReceived, qint64 bytesTotal)
{
    Q_UNUSED(bytesTotal)

    m_bytesDownloaded = bytesReceived;
    m_downloadSize = bytesTotal;
    emit episodeChanged();
}


void PodcastEpisodeSymbian::onReadyRead()
{
    QString downloadPath;
    downloadPath = PODCATCHER_PODCAST_DLDIR;

    QNetworkReply *reply = m_currentDownload;

    qint64 bytesToWrite = reply->bytesAvailable();
    while (bytesToWrite > 0) {
        m_downloadFile->write(reply->readAll());
        bytesToWrite = reply->bytesAvailable();
    }
}

void PodcastEpisodeSymbian::onPodcastEpisodeDownloadCompleted()
{
    QNetworkReply *reply = qobject_cast<QNetworkReply *>(sender());

    QString redirectedUrl = PodcastManager::redirectedRequest(reply);
    if (!redirectedUrl.isEmpty()) {
        m_downloadFile->remove();
        m_downloadLink = redirectedUrl;
        reply->deleteLater();
        downloadEpisode();
        return;
    }

    if (reply->error() != QNetworkReply::NoError) {
        qWarning() << "Download of podcast was not succesfull: " << reply->errorString();
        reply->deleteLater();
        m_downloadFile->close();
        m_downloadFile->deleteLater();
        emit podcastEpisodeDownloadFailed(this);
        return;
    }

    qDebug() << "Symbian download finished!";

    m_playFilename = QFileInfo(*m_downloadFile).absoluteFilePath();

    reply->deleteLater();
    m_downloadFile->close();
    m_downloadFile->deleteLater();
    emit podcastEpisodeDownloaded(this);
}

bool PodcastEpisodeSymbian::isMassStorageAvailable() {
    QSystemStorageInfo systemInfo;
    QStringList drivesFound;
    QStringList drives;
    bool found = false;

    drivesFound = systemInfo.logicalDrives();

    //Never show RAM (D:) and ROM (Z:)
    for (int d=0; d < drivesFound.count(); d++) {

        //In case Memory card slot is detected, check if space of it is (-1)
        //If it is -1 = there is no Card inserted ;)
        if (systemInfo.availableDiskSpace(drivesFound.at(d)) != -1)
        {
            if (drivesFound.at(d) != "D")
            {
                if (drivesFound.at(d) != "Z")
                {
                    found = true;
                    break;
                }
            }
        }
    }

    return found;
}

/*
//I'm sure this can be optimized but so far it works good for me...

void Hio::showDrives()
{
    //Using Mobility QSystemStorageInfo
    QSystemStorageInfo systemInfo;
    QStringList drivesFound;
    QStringList drives;

    drivesFound = systemInfo.logicalDrives();

    //Never show RAM (D:) and ROM (Z:)
    for (int d=0;d<drivesFound.count();d++)
    {
        //In case Memory card slot is detected, check if space of it is (-1)
        //If it is -1 = there is no Card inserted ;)
        if (systemInfo.availableDiskSpace(drivesFound.at(d)) != -1)
        {
            if (drivesFound.at(d) != "D")
            {
                if (drivesFound.at(d) != "Z")
                {
                    drives.append(drivesFound.at(d));

                    //Disregard this line it's specific to my code.
                    //this->drives->appendRow(new DirItem(uri, uri, "drive", "", ""));
                    //Valid drives are in "drives" QStringList
                }
            }
        }
    }
}
*/

