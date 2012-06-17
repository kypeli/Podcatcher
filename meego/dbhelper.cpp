#include <gq/gconfitem.h>

#include "podcastsqlmanager.h"
#include "dbhelper.h"

DBHelper::DBHelper()
{
}

void DBHelper::createAutoDownloadFieldChannels()
{

    GConfItem *autoDlConf        = new GConfItem("/apps/ControlPanel/Podcatcher/autodownload", this);
    bool autodownloadOnSettings = autoDlConf->value().toBool();
    delete autoDlConf;

    PodcastSQLManager *sqlmanager = PodcastSQLManagerFactory::sqlmanager();
    sqlmanager->checkAndCreateAutoDownload(autodownloadOnSettings);
}
