# Add more folders to ship with the application, here

# Additional import path used to resolve QML modules in Creator's code model
QML_IMPORT_PATH =

QT += declarative network xml sql

# If your application uses the Qt Mobility libraries, uncomment the following
# lines and add the respective components to the MOBILITY variable.
# CONFIG += mobility
# MOBILITY +=

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
    podcastchannelsmodel.cpp

HEADERS += \
        podcastmanager.h \
        podcastrssparser.h \
        podcastchannel.h \
        podcastsqlmanager.h \
        podcastglobals.h \
        podcatcherui.h \
        podcastepisode.h \
        podcastepisodesmodel.h \
        podcastepisodesmodelfactory.h \
        podcastchannelsmodel.h

maemo5 | !isEmpty(MEEGO_VERSION_MAJOR): {
    DEFINES += PODCATCHER_VERSION=1011
    DEFINES += LITE

    RESOURCES += \
        res.qrc

    CONFIG += link_pkgconfig
    PKGCONFIG += gq-gconf

    SOURCES += podcastmanagermeego.cpp \
               podcastepisodemeego.cpp

    HEADERS += podcastmanagermeego.h \
               podcastepisodemeego.h

    OTHER_FILES += \
        qml_meego/MainPage.qml \
        qml_meego/main.qml \
        Podcatcher.svg \
        Podcatcher.png \
        qml_meego/Utils.js \
        qml_meego/PodcastEpisodes.qml \
        qml_meego/PodcastEpisodesChannelInfo.qml \
        qml_meego/PodcastEpisodesList.qml \
        qml_meego/PodcastDownloadingProgress.qml \
        qml_meego/EmptyChannelPage.qml \
        qml_meego/BrowsePodcasts.qml \
        qml_meego/SearchPodcasts.qml \
        qml_meego/DPointerIntro.qml \
        qml_meego/EpisodeDescriptionPage.qml \
        qml_meego/ScrollBar.qml \
        qml_meego/QueueButton.qml \
        qml_meego/About.qml \
        qml_meego/PodcastChannelLogo.qml \
        qml_meego/PodcatcherInfoBanner.qml \
        qml_meego/AudioStreamer.qml

    OTHER_FILES += \
        Podcatcher.desktop \
        PodcatcherSettings.xml \
        PodcatcherSettings.desktop \
        podcatcher.schema \
        qtc_packaging/debian_harmattan/postinst \
        qtc_packaging/debian_harmattan/rules \
        qtc_packaging/debian_harmattan/README \
        qtc_packaging/debian_harmattan/copyright \
        qtc_packaging/debian_harmattan/control \
        qtc_packaging/debian_harmattan/compat \
        qtc_packaging/debian_harmattan/changelog \

    LIBS += -lcontentaction

    # enable booster
    CONFIG += qdeclarative-boostable
    QMAKE_CXXFLAGS += -fPIC -fvisibility=hidden -fvisibility-inlines-hidden
    QMAKE_LFLAGS += -pie -rdynamic

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
}

symbian: {
    TARGET = Podmaster
    DEFINES += PODCATCHER_VERSION=103
    VERSION = 1.0.3

    CONFIG += qt-components mobility
    MOBILITY += systeminfo multimedia

    ICON = Podmaster.svg

    RESOURCES += \
        res_symbian.qrc

    # Some Symbian magic
    TARGET.CAPABILITY += NetworkServices
#    TARGET.EPOCHEAPSIZE = 10000 10000000
#    TARGET.EPOCSTACKSIZE = 0x8000
    TARGET.EPOCHEAPSIZE = 0x20000 0x2000000


    # Add more folders to ship with the application, here
    folder_01.source = qml_symbian/
    folder_01.target = qml
    DEPLOYMENTFOLDERS = folder_01

    symbian:TARGET.UID3 = 0x2005ffdc   # OVI Store UID!
#    symbian:TARGET.UID3 = 0x80000042     # Self-signed!

    SOURCES += podcastmanagersymbian.cpp \
               podcastepisodesymbian.cpp

    HEADERS += podcastmanagersymbian.h \
               podcastepisodesymbian.h

    OTHER_FILES += \
        qml_symbian/MainPage.qml \
        qml_symbian/main.qml \
        Podmaster.svg \
        Podcatcher.png \
        qml_symbian/Utils.js \
        qml_symbian/PodcastEpisodes.qml \
        qml_symbian/PodcastEpisodesChannelInfo.qml \
        qml_symbian/PodcastEpisodesList.qml \
        qml_symbian/PodcastDownloadingProgress.qml \
        qml_symbian/EmptyChannelPage.qml \
        qml_symbian/BrowsePodcasts.qml \
        qml_symbian/SearchPodcasts.qml \
        qml_symbian/DPointerIntro.qml \
        qml_symbian/EpisodeDescriptionPage.qml \
        qml_symbian/ScrollBar.qml \
        qml_symbian/QueueButton.qml \
        qml_symbian/About.qml \
        qml_symbian/PodcastChannelLogo.qml \
        qml_symbian/PodcatcherInfoBanner.qml

    my_deployment.pkg_prerules += vendorinfo

    DEPLOYMENT += my_deployment

    vendorinfo += "%{\"\"}" ":\"\""

    TARGET.UID3 += 0x20034a6f
}

# Please do not modify the following two lines. Required for deployment.
include(deployment.pri)
qtcAddDeployment()














