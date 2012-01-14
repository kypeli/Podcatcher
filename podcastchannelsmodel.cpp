#include <QtDebug>

#include "podcastchannelsmodel.h"
#include "dbhelper.h"

PodcastChannelsModel::PodcastChannelsModel(QObject *parent) :
    QAbstractListModel(parent)
{
    QHash<int, QByteArray> roles;
    roles[ChannelIdRole] = "channelId";
    roles[TitleRole] = "title";
    roles[DescriptionRole] = "description";
    roles[LogoRole] = "logo";
    roles[IsRefreshingRole] = "isRefreshing";
    roles[IsDownloadingRole] = "isDownloading";
    roles[UnplayedEpisodesRole] = "unplayedEpisodes";
    roles[AutoDownloadOnRole] = "autoDownloadOn";

    setRoleNames(roles);

    DBHelper dbhelper;
    dbhelper.createAutoDownloadFieldChannels();

    m_sqlmanager = PodcastSQLManagerFactory::sqlmanager();
    foreach(PodcastChannel *channel, m_sqlmanager->channelsInDB()) {
        connect(channel, SIGNAL(channelChanged()),
                this, SLOT(onChannelChanged()));

        m_channels << channel;
    }
}

PodcastChannelsModel::~PodcastChannelsModel() {
    foreach(PodcastChannel *channel, m_channels) {
        delete channel;
    }
}

QVariant PodcastChannelsModel::data(const QModelIndex &index, int role) const
{
    if (index.row() < 0 || index.row() > m_channels.count())
        return QVariant();

    PodcastChannel *channel = m_channels.at(index.row());

    switch(role) {
    case ChannelIdRole:
        return channel->channelDbId();   // FIXME: Do not expose this.
        break;

    case TitleRole:
        return channel->title();
        break;

    case DescriptionRole:
        return channel->description();
        break;

    case LogoRole:
        return channel->logo();
        break;

    case IsRefreshingRole:
        return channel->isRefreshing();
        break;

    case IsDownloadingRole:
        return channel->isDownloading();
        break;

    case UnplayedEpisodesRole:
        return channel->unplayedEpisodes();
        break;

    case AutoDownloadOnRole:
        return channel->isAutoDownloadOn();
        break;
    }

    return QVariant();
}

int PodcastChannelsModel::rowCount(const QModelIndex &parent) const
{
    Q_UNUSED(parent)
    return m_channels.count();
}

bool channelsLessThan(const PodcastChannel *c1, const PodcastChannel *c2)
{
    return *c1 < *c2;
}
bool PodcastChannelsModel::addChannel(PodcastChannel *channel)
{
     if (m_sqlmanager->podcastChannelToDB(channel) > 0) {
         m_channels.append(channel);

         qSort(m_channels.begin(),
               m_channels.end(),
               channelsLessThan);

         int index = m_channels.indexOf(channel);
         beginInsertRows(QModelIndex(), index, index);
         endInsertRows();

         connect(channel, SIGNAL(channelChanged()),
                 this, SLOT(onChannelChanged()));

         return true;
     } else {
         return false;
     }
}

bool PodcastChannelsModel::removeChannel(PodcastChannel *channel)
{
    int dbid = channel->channelDbId();
    // Remove from DB
    m_sqlmanager->removeChannelFromDB(dbid);

    // Remove from model
    for (int i=0; i<m_channels.size(); i++) {
        PodcastChannel *channel = m_channels.at(i);
        if (channel->channelDbId() == dbid) {
            beginRemoveRows(QModelIndex(), i, i);
            m_channels.removeAt(i);
            endRemoveRows();

            return true;
        }
    }

    return false;
}

QList<PodcastChannel *> PodcastChannelsModel::channels()
{
    return m_channels;
}

bool PodcastChannelsModel::channelAlreadyExists(PodcastChannel *channel)
{
    return m_sqlmanager->isChannelInDB(channel);
}

PodcastChannel * PodcastChannelsModel::podcastChannelById(int id)
{
    foreach(PodcastChannel *channel, m_channels) {
        if (channel->channelDbId() == id) {
            return channel;
        }
    }

    return 0;
}

void PodcastChannelsModel::refreshChannel(int id)
{
    PodcastChannel *channel = podcastChannelById(id);
    if (channel == 0) {
        qWarning() << "Could not refresh PodcastChannel. Got NULL!";
        return;
    }

    m_sqlmanager->channelInDB(channel->channelDbId(), channel);
}


void PodcastChannelsModel::onChannelChanged()
{
    qDebug() << "Podcast channel changed. Refreshing from SQL to UI...";
    PodcastChannel *channel  = qobject_cast<PodcastChannel *>(sender());
    if (channel == 0) {
        return;
    }

    int channelIndex = m_channels.indexOf(channel);
    if (channelIndex != -1) {
        QModelIndex modelIndex = createIndex(channelIndex, 0);
        emit dataChanged(modelIndex, modelIndex);
    }
}

void PodcastChannelsModel::setAutoDownloadToDB(bool autoDownload)
{
    m_sqlmanager->updateChannelAutoDownloadToDB(autoDownload);
}

void PodcastChannelsModel::updateChannel(PodcastChannel *channel)
{
    m_sqlmanager->updateChannelInDB(channel);
}



