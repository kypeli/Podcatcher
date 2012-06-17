#ifndef DBHELPER_H
#define DBHELPER_H

#include <QObject>

class DBHelper : public QObject
{
    Q_OBJECT
public:
    DBHelper();

    void createAutoDownloadFieldChannels();
};

#endif // DBHELPER_H
