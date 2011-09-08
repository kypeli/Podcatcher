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
#include <QtDebug>

#include "podcastchannel.h"

PodcastChannel::PodcastChannel(QObject *parent) :
    QObject(parent)
{
    m_isRefreshing = false;
    m_unplayedEpisodes = 0;
}

void PodcastChannel::setId(int id)
{
    m_id = id;
}

int PodcastChannel::channelDbId()
{
    return m_id;
}

void PodcastChannel::setTitle(const QString &title)
{
    m_title = title;
}

void PodcastChannel::setLogoUrl(const QString &logo)
{
    m_logoUrl = logo;
}

QString PodcastChannel::title() const
{
    return m_title;
}

QString PodcastChannel::logoUrl() const
{
    return m_logoUrl;
}

QString PodcastChannel::url() const
{
    return m_url;
}

void PodcastChannel::setUrl(const QString &url)
{
    m_url = url;
}

void PodcastChannel::setLogo(const QString &logo)
{
    m_logo = logo;
}

QString PodcastChannel::logo() const
{
    return m_logo;
}

void PodcastChannel::dumpInfo() const
{
    qDebug() << "Channel info: "
             << m_id
             << m_title
             << m_logo
             << m_logoUrl;
}

void PodcastChannel::setDescription(const QString &desc)
{
    m_description = desc;
}

QString PodcastChannel::description() const
{
    return m_description;
}

void PodcastChannel::setXml(QByteArray xml)
{
    m_xml = xml;
}

QByteArray PodcastChannel::xml() const
{
    return m_xml;
}

void PodcastChannel::setIsRefreshing(bool refreshing)
{
    if (m_isRefreshing != refreshing) {
        m_isRefreshing = refreshing;
        emit channelChanged();
    }
}

bool PodcastChannel::isRefreshing() const
{
    return m_isRefreshing;
}

int PodcastChannel::unplayedEpisodes() const
{
    return m_unplayedEpisodes;
}

void PodcastChannel::setUnplayedEpisodes(int unplayed)
{
    if (unplayed != m_unplayedEpisodes) {
        m_unplayedEpisodes = unplayed;
        emit channelChanged();
    }
}

bool PodcastChannel::operator<(const PodcastChannel &other) const
{
    if (m_title < other.title() ) {
        return true;
    } else  {
        return false;
    }

}

