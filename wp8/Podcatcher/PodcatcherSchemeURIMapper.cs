using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using PodcastWP;
using Microsoft.Phone.BackgroundAudio;

namespace Podcatcher
{
    class PodcatcherSchemeURIMapper : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            if (PodcastHelper.HasPodcastUri(uri))
            {
                BackgroundAudioPlayer bap = BackgroundAudioPlayer.Instance;
                var action = PodcastHelper.RetrievePodcastAction(uri);

                switch (action.Command)
                {
                    case PodcastCommand.Launch:
                        // Do nothing.
                        break;
                    case PodcastCommand.Pause:
                        if (bap.CanPause)
                        {
                            bap.Pause();
                        }
                        break;
                    case PodcastCommand.Play:
                        if (bap.PlayerState != PlayState.Playing)
                        {
                            bap.Play();
                        }
                        break;
                    case PodcastCommand.SkipNext:
                        if (bap.PlayerState == PlayState.Playing)
                        {
                            bap.SkipNext();
                        }
                        break;
                    case PodcastCommand.SkipPrevious:
                        if (bap.PlayerState == PlayState.Playing)
                        {
                            bap.SkipPrevious();
                        }
                        break;
                }

                return new Uri("/Views/MainView.xaml", UriKind.Relative);
            }
            // Otherwise perform normal launch.
            return uri;
        }
    }
}
