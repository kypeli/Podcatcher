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
#ifndef PODCASTSTREAM_H
#define PODCASTSTREAM_H

#include <QString>
#include <QObject>

class PodcastChannel : public QObject
{
    Q_OBJECT
    Q_PROPERTY(int channelId READ channelDbId WRITE setId)
    Q_PROPERTY(QString logo READ logo WRITE setLogo)
    Q_PROPERTY(QString title READ title WRITE setTitle)
    Q_PROPERTY(QString description READ description WRITE setDescription)
    Q_PROPERTY(bool isRefreshing READ isRefreshing WRITE setIsRefreshing)
    Q_PROPERTY(bool isDownloading READ isDownloading WRITE setIsDownloading NOTIFY downloadingChanged)
    Q_PROPERTY(bool isAutoDownloadOn READ isAutoDownloadOn WRITE setAutoDownloadOn NOTIFY autoDownloadOnChanged)

public:
    explicit PodcastChannel(QObject *parent = 0);

    // Property setters.
    void setId(int id);
    void setTitle(const QString &title);
    void setLogoUrl(const QString &logo);
    void setLogo(const QString &logo);
    void setUrl(const QString &url);
    void setDescription(const QString &desc);
    void setIsRefreshing(bool refreshing);
    void setIsDownloading(bool downloading);
    void setUnplayedEpisodes(int unplayed);
    void setAutoDownloadOn(bool autoDownloadOn);

    void setXml(QByteArray xml);

    // Property getters.
    int channelDbId();
    QString title() const;
    QString logoUrl() const;
    QString logo() const;
    QString url() const;
    QString description() const;
    bool isRefreshing() const;
    bool isDownloading() const;
    int unplayedEpisodes() const;
    bool isAutoDownloadOn() const;


    QByteArray xml() const;

    // Utility methods.
    void dumpInfo() const;

    bool operator<(const PodcastChannel &other) const;

signals:
    void channelChanged();
    void downloadingChanged();
    void autoDownloadOnChanged();

public slots:

private:
    int     m_id;
    QString m_title;
    QString m_logoUrl;
    QString m_logo;
    QString m_url;
    QString m_description;
    bool m_isRefreshing;
    bool m_isDownloading;
    int m_unplayedEpisodes;
    bool m_autoDownloadOn;

    QByteArray m_xml;
};

#endif // PODCASTSTREAM_H
