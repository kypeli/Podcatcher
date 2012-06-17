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
#ifndef PODCASTRSSPARSER_H
#define PODCASTRSSPARSER_H

#include <QObject>


#include "podcastchannel.h"
#include "podcastepisode.h"

class QDomNode;
class QDomNodeList;
class PodcastRSSParser : public QObject
{
    Q_OBJECT
public:
    explicit PodcastRSSParser(QObject *parent = 0);

    static bool populateChannelFromChannelXML(PodcastChannel *channel,
                                              QByteArray xmlReply);

    static bool populateEpisodesFromChannelXML(QList<PodcastEpisode *> *episodes,
                                               QByteArray xmlReply);

    static bool isValidPodcastFeed(QByteArray xmlReply);

    static QList<QString> parseGPodderSubscription(QByteArray gpodderXml);

signals:

public slots:

private:
    static QDateTime parsePubDate(const QDomNode &node);
    static QString trimPubDate(const QString &pubdate);
    static bool containsEnclosure(const QDomNodeList &itemNodes);
    static bool isEmptyItem(const QDomNode &node);

};

#endif // PODCASTRSSPARSER_H
