#ifndef PODCASTCHANNELSMODEL_H
#define PODCASTCHANNELSMODEL_H

#include <QAbstractListModel>

#include "podcastchannel.h"
#include "podcastsqlmanager.h"

class PodcastManager;
class PodcastChannelsModel : public QAbstractListModel
{
    Q_OBJECT

    enum ChannelRoles {
        ChannelIdRole = Qt::UserRole + 1,
        TitleRole,
        DescriptionRole,
        LogoRole,
        IsRefreshingRole,
        IsDownloadingRole,
        UnplayedEpisodesRole,
        AutoDownloadOnRole
    };

public:
    ~PodcastChannelsModel();

    int rowCount(const QModelIndex & parent = QModelIndex()) const;
    QVariant data(const QModelIndex & index, int role = Qt::DisplayRole) const;

    bool addChannel(PodcastChannel *channel);
    bool removeChannel(PodcastChannel *channel);
    QList<PodcastChannel *> channels();

    PodcastChannel * podcastChannelById(int id);
    bool channelAlreadyExists(PodcastChannel *channel);
    void refreshChannel(int id);
    void setAutoDownloadToDB(bool autoDownload);
    void updateChannel(PodcastChannel *channel);

signals:

public slots:

private slots:
    void onChannelChanged();

private:
    explicit PodcastChannelsModel(QObject *parent = 0);  // Do not let instantiation of this class...
    QList<PodcastChannel *> m_channels;
    PodcastSQLManager *m_sqlmanager;

    friend class PodcastManager;                         // ..except for PodcastManager;
};

#endif // PODCASTCHANNELSMODEL_H
