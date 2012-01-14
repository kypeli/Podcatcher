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
#include <QDomDocument>
#include <QDomElement>
#include <QDomNodeList>
#include <QDomNamedNodeMap>
#include <QDateTime>

#include <QtDebug>

#include "podcastrssparser.h"

PodcastRSSParser::PodcastRSSParser(QObject *parent) :
    QObject(parent)
{
}

bool PodcastRSSParser::populateChannelFromChannelXML(PodcastChannel *channel, QByteArray xmlReply)
{
    qDebug() << "Parsing XML for channel URL" << channel->url();

    if (channel == 0) {
        return false;
    }

    if (xmlReply.size() < 10) {
        return false;
    }

    QDomDocument xmlDocument;
    if (xmlDocument.setContent(xmlReply) == false) {        // Construct the XML document and parse it.
        qWarning() << "Could not parse channel XML content! Data: " << QString(xmlDocument.toByteArray());
        return false;
    }

    QDomElement docElement = xmlDocument.documentElement();

    QDomNode channelNode = docElement.elementsByTagName("channel").at(0);    // Get the only channel element we have.
    channel->setTitle(channelNode.firstChildElement("title").text());        // Find the title.

    channel->setDescription(channelNode.firstChildElement("description").text());

    if (channel->logoUrl().isEmpty()) {
        QDomNode imageNode = channelNode.toElement().elementsByTagName("image").at(0); // Find the logo.
        channel->setLogoUrl(imageNode.firstChildElement("url").text());                // And the url to it.
    }

    channel->dumpInfo();

    return true;
}

bool PodcastRSSParser::populateEpisodesFromChannelXML(QList<PodcastEpisode *> *episodes, QByteArray xmlReply)
{
    qDebug() << "Parsing XML for episodes";

    if (xmlReply.size() < 10) {
        return false;
    }

    QDomDocument xmlDocument;
    if (xmlDocument.setContent(xmlReply) == false) {        // Construct the XML document and parse it.
        return false;
    }

    QDomElement docElement = xmlDocument.documentElement();

    QDomNodeList channelNodes = docElement.elementsByTagName("item");  // Find all the "item nodes from the feed XML.
    qDebug() << "I have" << channelNodes.size() << "episode elements";

    for (uint i=0; i<channelNodes.length(); i++) {
        QDomNode node = channelNodes.at(i);

        PodcastEpisode *episode = new PodcastEpisode;
        episode->setTitle(node.firstChildElement("title").text());
        episode->setDescription(node.firstChildElement("description").text());
        episode->setDuration(node.firstChildElement("itunes:duration").text());

        QDateTime pubDate = parsePubDate(node);

        if (!pubDate.isValid()) {
            qWarning() << "Could not parse pubDate for podcast episode!";
            return false;
        } else {
            episode->setPubTime(pubDate);
        }

        QDomNamedNodeMap attrMap = node.firstChildElement("enclosure").attributes();
        episode->setDownloadLink(attrMap.namedItem("url").toAttr().value());
        episode->setDownloadSize(attrMap.namedItem("length").toAttr().value().toInt());

        episodes->append(episode);
    }

    return true;
}

bool PodcastRSSParser::isValidPodcastFeed(QByteArray xmlReply)
{
    qDebug() << "Checking is podcast feed is valid.";
    if (xmlReply.size() < 10) {
        qDebug() << "Not valid!";
        return false;
    }

    QDomDocument xmlDocument;
    if (xmlDocument.setContent(xmlReply) == false) {        // Construct the XML document and parse it.
        return false;
    }

    QDomElement docElement = xmlDocument.documentElement();

    QDomNodeList itemNodes = docElement.elementsByTagName("item");  // Find all the "item nodes from the feed XML.

    for (uint i=0; i<itemNodes.length(); i++) {
        QDomNode node = itemNodes.at(i);

        QDateTime pubDate = parsePubDate(node);

        if (!pubDate.isValid()) {
            qDebug() << "INVALID FEED: Pubdate not valid!";
            return false;
        }

        if (!containsEnclosure(itemNodes)) {
            qDebug() << "INVALID FEED: Does not contain media.";
            return false;
        }
    }

    qDebug() << "Is valid.";
    return true;
}

QDateTime PodcastRSSParser::parsePubDate(const QDomNode &node)
{
    // Set the publication timestamp by parsing the RFC 822 time format from the XML
    // feed and then truncating the parsed string so that it does not include the timezone
    // information as QDateTime cannot parse that. Create a QDateTime from this string
    // and store it in the ChannelEpisode.
    QString pubDateString = node.firstChildElement("pubDate").text();
    qDebug() << "Feed pubdate: " << pubDateString;

    // Some feeds use just "date". Let's go for that as well is we didn't find it in "pubDate".
    if (pubDateString.isEmpty()) {
        pubDateString = node.firstChildElement("date").text();
        qDebug() << "Empty <pubDate> tag. Feed pubdate: " << pubDateString;
    }

    if (pubDateString.isEmpty()) {
        qDebug() << "Could not find pubDate attribute. Giving up...";
        return QDateTime();
    }

    pubDateString = trimPubDate(pubDateString);

    QLocale loc(QLocale::C);
    QString tryParseDate = pubDateString.left(QString("dd MMM yyyy HH:mm:ss").length());  // QDateTime cannot parse RFC 822 time format, so remove the timezone information from it.
    QDateTime pubDate = loc.toDateTime(tryParseDate,
                                       "dd MMM yyyy HH:mm:ss");
    if (!pubDate.isValid()) {
        // We probably could not parse the date which in some broken podcast feeds is
        // defined only by one integer instead of two (like "2 Jul" instead of "02 Jul")
        // I am looking at you Skeptics Guide to the Universe!

        qDebug() << "Could not parse pubDate. Trying with just one date integer.";

        tryParseDate = pubDateString.left(QString("d MMM yyyy HH:mm:ss").length());
        pubDate = loc.toDateTime(tryParseDate,
                                        "d MMM yyyy HH:mm:ss");
    }

    if (!pubDate.isValid()) {
        // Let's try just once more just to please Hacker Public Radio
        qDebug() << "Could not parse pubDate. Trying with an odd format from Hacker Public Radio";

        tryParseDate = pubDateString.left(QString("yyyy-MM-dd").length());
        pubDate = loc.toDateTime(tryParseDate,
                                        "yyyy-MM-dd");
    }

    return pubDate;
}

QString PodcastRSSParser::trimPubDate(const QString &pubdate) {
    QString parsedString = pubdate;

    // Remove optional day field.
    // Input is <pubDate>Wed, 6 Jul 2005 13:00:00 PDT</pubDate>
    // So we remove the start with "Wed, "
    if (parsedString.indexOf(',') > 0) {
        parsedString = parsedString.mid(pubdate.indexOf(',') + 2);
    }

    qDebug() << "Trimmed feed URL: " << parsedString;
    return parsedString;
}

bool PodcastRSSParser::containsEnclosure(const QDomNodeList &itemNodes) {
    QDomNode node = itemNodes.at(0);
    QDomElement enclosure = node.firstChildElement("enclosure");
    if (enclosure.isNull()) {
        qDebug() << "Podcast feed does not contain the <enclosure> tag.";
        return false;
    }

    return true;
}

