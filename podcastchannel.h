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
#ifndef PODCASTSTREAM_H
#define PODCASTSTREAM_H

#include <QString>
#include <QObject>

class PodcastChannel : public QObject
{
    Q_OBJECT
    Q_PROPERTY(int channelId READ channelId WRITE setId NOTIFY idChanged)
    Q_PROPERTY(QString logo READ logo WRITE setLogo NOTIFY logoChanged)
    Q_PROPERTY(QString title READ title WRITE setTitle NOTIFY titleChanged)
    Q_PROPERTY(QString description READ description WRITE setDescription NOTIFY descriptionChanged)
    Q_PROPERTY(bool isRefreshing READ isRefreshing WRITE setIsRefreshing NOTIFY refreshingChanged)
    Q_PROPERTY(int unplayedEpisodes READ unplayedEpisodes WRITE setUnplayedEpisodes NOTIFY unplayedEpisodesChanged)

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
    void setUnplayedEpisodes(int unplayed);

    void setXml(QByteArray xml);

    // Property getters.
    int channelId();
    QString title() const;
    QString logoUrl() const;
    QString logo() const;
    QString url() const;
    QString description() const;
    bool isRefreshing() const;
    int unplayedEpisodes() const;

    QByteArray xml() const;

    // Utility methods.
    void dumpInfo() const;

signals:
    void idChanged();
    void logoChanged();
    void titleChanged();
    void descriptionChanged();
    void refreshingChanged();
    void unplayedEpisodesChanged();

public slots:

private:
    int     m_id;
    QString m_title;
    QString m_logoUrl;
    QString m_logo;
    QString m_url;
    QString m_description;
    bool m_isRefreshing;
    int m_unplayedEpisodes;

    QByteArray m_xml;
};

#endif // PODCASTSTREAM_H
