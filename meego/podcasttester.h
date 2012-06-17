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
#ifndef PODCASTTESTER_H
#define PODCASTTESTER_H

#include <QObject>
#include <QtDebug>

#include "podcastchannel.h"
#include "podcastmanager.h"

class PodcastTester {
public:
    PodcastTester() {
        qDebug() << "PODCATCHER TESTER:";
    }

    void testChannels() {
        qDebug() << "  Testing channels!";
        podcastManager.requestPodcastChannel(QUrl("http://leoville.tv/podcasts/kfi.xml"));

        QList<PodcastChannel *> channels = podcastManager.podcastChannels();
        foreach(PodcastChannel *chan, channels) {
            qDebug() <<  chan->channelDbId() << chan->title() << chan->logo();
        }
    }

    void testEpisodes() {
        qDebug() << "  Testing episodes!";

        PodcastChannel channel;
        channel.setUrl("http://leoville.tv/podcasts/kfi.xml");
        podcastManager.refreshPodcastChannelEpisodes(&channel);
    }

private:
    PodcastManager podcastManager;
};

#endif // PODCASTTESTER_H
