# NOTICE:
#
# Application name defined in TARGET has a corresponding QML filename.
# If name defined in TARGET is changed, the following needs to be done
# to match new name:
#   - corresponding QML filename must be changed
#   - desktop icon filename must be changed
#   - desktop filename must be changed
#   - icon definition filename in desktop file must be changed
#   - translation filenames have to be changed

# The name of your application
TARGET = harbour-podcatcher

DEFINES += PODCATCHER_VERSION=1110
QT += sql xml concurrent

CONFIG += sailfishapp

CONFIG += link_pkgconfig
PKGCONFIG += sailfishapp mlite5
#PKGCONFIG += contentaction5

SOURCES += src/Podcatcher.cpp \
    src/dbhelper.cpp \
    src/podcastchannel.cpp \
    src/podcastchannelsmodel.cpp \
    src/podcastepisode.cpp \
    src/podcastepisodesmodel.cpp \
    src/podcastepisodesmodelfactory.cpp \
    src/podcastmanager.cpp \
    src/podcastrssparser.cpp \
    src/podcastsqlmanager.cpp \
    src/podcatcherui.cpp

OTHER_FILES += qml/Podcatcher.qml \
    qml/cover/CoverPage.qml \
    rpm/Podcatcher.changes.in \
    translations/*.ts

SAILFISHAPP_ICONS = 86x86 108x108 128x128 256x256

# to disable building translations every time, comment out the
# following CONFIG line
CONFIG += sailfishapp_i18n

# German translation is enabled as an example. If you aren't
# planning to localize your app, remember to comment out the
# following TRANSLATIONS line. And also do not forget to
# modify the localized app name in the the .desktop file.
TRANSLATIONS += translations/harbour-podcatcher-de.ts


DISTFILES += \
    qml/Utils.js \
    qml/EmptyPage.qml \
    qml/PodcatcherInfoBanner.qml \
    qml/pages/MainPage.qml \
    qml/pages/EmptyChannelPage.qml \
    qml/pages/BrowsePodcasts.qml \
    qml/pages/PodcastEpisodes.qml \
    qml/pages/PodcastEpisodesChannelInfo.qml \
    qml/pages/PodcastEpisodesList.qml \
    qml/pages/PodcastChannelLogo.qml \
    qml/pages/PodcastDownloadingProgress.qml \
    qml/pages/EpisodeDescriptionPage.qml \
    qml/pages/SearchPodcasts.qml \
    qml/pages/ImportFromGPodder.qml \
    qml/pages/About.qml \
    qml/pages/Settings.qml \
    harbour-podcatcher.desktop \
    rpm/harbour-podcatcher.spec \
    rpm/harbour-podcatcher.yaml \
    qml/pages/InfoBanner.qml

HEADERS += \
    src/dbhelper.h \
    src/podcastchannel.h \
    src/podcastchannelsmodel.h \
    src/podcastepisode.h \
    src/podcastepisodesmodel.h \
    src/podcastepisodesmodelfactory.h \
    src/podcastglobals.h \
    src/podcastmanager.h \
    src/podcastrssparser.h \
    src/podcastsqlmanager.h \
    src/podcasttester.h \
    src/podcatcherui.h

RESOURCES += \
    res.qrc

