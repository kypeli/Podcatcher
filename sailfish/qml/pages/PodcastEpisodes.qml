/**
 * This file is part of Podcatcher for N9.
 * Author: Johan Paul (johan.paul@gmail.com)
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
import QtQuick 2.0
import Sailfish.Silica 1.0

Page {
    id: episodesPage

    //    orientationLock: PageOrientation.LockPortrait
    SilicaFlickable{
        anchors.fill: parent
        PullDownMenu{

            MenuItem {
                text: "Remove subscription"
                onClicked: {
                    remorseSubscription.execute(qsTr("Remove subscription"), function(){
                        appWindow.deleteChannel(channel.channelId);
                        pageStack.pop(mainPage);
                    }
                    )
                }
            }


            MenuItem {
                text: "Delete all downloaded podcasts"
                onClicked: {
                    //queryDeletePodcasts.open();
                    remorseDownloads.execute("Delete all downloaded podcasts", function(){
                        ui.deletePodcasts(channel.channelId);
                    }
                    )
                }
            }

            MenuItem {
                text: "Mark all podcasts as played"
                onClicked: {
                    appWindow.allListened(channel.channelId);
                }
            }


            MenuItem {
                text: "Refresh"
                onClicked: {
                    appWindow.refreshEpisodes(channel.channelId)
                    //refreshingBanner.show()
                }
                anchors.horizontalCenter: parent.horizontalCenter
            }

        }

        RemorsePopup{
            id:remorseSubscription
            text: qsTr("Remove subscription")
        }

        RemorsePopup{
            id:remorseDownloads
            text: qsTr("Delete downloaded podcasts")
        }

        Column {
            id: episodesPageColumn
            anchors.fill: parent
            spacing: Theme.paddingMedium

            PageHeader{
                id: chanelTitle
                title: channel.title
                wrapMode: Text.WordWrap
            }


            PodcastEpisodesChannelInfo {
                id: episodeData
                width: parent.width
                height: 218
            }


            Separator{
                width:parent.width
            }

            PodcastEpisodesList {
                id: episodesList
                width: parent.width
                height: parent.height - episodeData.height -chanelTitle.height - 3*Theme.paddingMedium
                channelId: channel.channelId
            }
        }


        EpisodeDescriptionPage {
            id: episodeDescriptionPage
        }

        //        InfoBanner {
        //            id: refreshingBanner
        //            text:  "Refreshing episodes..."
        //            timerShowTime: 1500
        //        }


        //        Connections {
        //            target: ui
        //            onShowInfoBanner: {
        //                console.log("Showing banner: "+text);
        //                uiInfoBanner.text = text
        //                uiInfoBanner.show();
        //            }
        //        }

        //        InfoBanner {
        //            id: uiInfoBanner
        //            topMargin: 10
        //            leftMargin: 10
        //        }

    }
}
