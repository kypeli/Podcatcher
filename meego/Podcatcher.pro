# Add more folders to ship with the application, here

# Additional import path used to resolve QML modules in Creator's code model
QML_IMPORT_PATH =

QT+= declarative network xml sql
CONFIG += link_pkgconfig
PKGCONFIG += gq-gconf

#DEFINES += LITE
DEFINES += PODCATCHER_VERSION=1110

# The .cpp file which was generated for your project. Feel free to hack it.
SOURCES += main.cpp \
    podcastmanager.cpp \
    podcastrssparser.cpp \
    podcastchannel.cpp \
    podcastsqlmanager.cpp \
    podcatcherui.cpp \
    podcastepisode.cpp \
    podcastepisodesmodel.cpp \
    podcastepisodesmodelfactory.cpp \
    podcastchannelsmodel.cpp \
    dbhelper.cpp

OTHER_FILES += \
    qml/MainPage.qml \
    qml/main.qml \
    Podcatcher.desktop \
    Podcatcher.svg \
    Podcatcher.png \
    qtc_packaging/debian_harmattan/rules \
    qtc_packaging/debian_harmattan/README \
    qtc_packaging/debian_harmattan/copyright \
    qtc_packaging/debian_harmattan/control \
    qtc_packaging/debian_harmattan/compat \
    qtc_packaging/debian_harmattan/changelog \
    qml/Utils.js \
    qml/PodcastEpisodes.qml \
    qml/PodcastEpisodesChannelInfo.qml \
    qml/PodcastEpisodesList.qml \
    qml/PodcastDownloadingProgress.qml \
    qml/EmptyChannelPage.qml \
    qml/BrowsePodcasts.qml \
    qml/SearchPodcasts.qml \
    qml/DPointerIntro.qml \
    qml/EpisodeDescriptionPage.qml \
    qml/ScrollBar.qml \
    qml/QueueButton.qml \
    qml/About.qml \
    PodcatcherSettings.xml \
    PodcatcherSettings.desktop \
    podcatcher.schema \
    qml/PodcastChannelLogo.qml \
    qml/PodcatcherInfoBanner.qml \
    qtc_packaging/debian_harmattan/postinst \
    qml/AudioStreamer.qml \
    qml/ImportFromGPodderSheet.qml

RESOURCES += \
    res.qrc

# Please do not modify the following two lines. Required for deployment.
include(deployment.pri)
qtcAddDeployment()

LIBS += -lcontentaction

# enable booster
CONFIG += qdeclarative-boostable
QMAKE_CXXFLAGS += -fPIC -fvisibility=hidden -fvisibility-inlines-hidden
QMAKE_LFLAGS += -pie -rdynamic

HEADERS += \
    podcastmanager.h \
    podcastrssparser.h \
    podcastchannel.h \
    podcastsqlmanager.h \
    podcastglobals.h \
    podcatcherui.h \
#     podcasttester.h \
    podcastepisode.h \
    podcastepisodesmodel.h \
    podcastepisodesmodelfactory.h \
    podcastchannelsmodel.h \
    dbhelper.h

qml.files = qml/MainPage.qml qml/main.qml qml/PodcastEpisodes.qml qml/PodcastEpisodesChannelInfo.qml qml/PodcastEpisodesList.qml qml/PodcastDownloadingProgress.qml qml/EmptyChannelPage.qml qml/BrowsePodcasts.qml qml/SearchPodcasts.qml qml/DPointerIntro.qml qml/EpisodeDescriptionPage.qml qml/ScrollBar.qml qml/QueueButton.qml
qml.path = /opt/$${TARGET}/bin/qml

settings.files = PodcatcherSettings.xml
settings.path=/usr/share/duicontrolpanel/uidescriptions/

settingsdesktop.files=PodcatcherSettings.desktop
settingsdesktop.path=/usr/lib/duicontrolpanel/

settingsschemaFoo.files=podcatcher.schema
settingsschemaFoo.path=/tmp

INSTALLS += settings settingsdesktop settingsschemaFoo

system(cp gfx/Podcatcher_full.png gfx/Podcatcher.png)
contains(DEFINES, LITE) {
    system(cp gfx/Podcatcher_lite.png gfx/Podcatcher.png)
}

contains(MEEGO_EDITION,harmattan) {
    icon.files = Podcatcher.png
    icon.path = /usr/share/icons/hicolor/80x80/apps
    INSTALLS += icon
}

# INSTALLS += qml
