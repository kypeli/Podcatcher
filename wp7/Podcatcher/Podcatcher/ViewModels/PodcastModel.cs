using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;

namespace Podcatcher
{
    public class PodcastModel : INotifyPropertyChanged
    {
        private const string PODCAST_ICON_DIR = "PodcastIcons";
        
        private IsolatedStorageFile m_localPodcastIconCache;

        public PodcastModel()
        {
            m_localPodcastIconCache = IsolatedStorageFile.GetUserStoreForApplication();
            m_localPodcastIconCache.CreateDirectory(PODCAST_ICON_DIR);
        }

        private string m_PodcastName;
        public string PodcastName
        {
            get
            {
                return m_PodcastName;
            }
            
            set
            {
                if (value != m_PodcastName)
                {
                    m_PodcastName = value;
                    NotifyPropertyChanged("PodcastName");
                }
            }
        }

        private string m_PodcastDescription;
        public string PodcastDescription
        {
            get
            {
                return m_PodcastDescription;
            }

            set
            {
                if (value != m_PodcastDescription)
                {
                    m_PodcastDescription = value;
                    NotifyPropertyChanged("PodcastDescription");
                }
            }
        }

        private string m_PodcastLogoLocalLocation;
        private string m_PodcastLogoUrl;
        public String PodcastLogoUrl
        {
            get
            {
                return m_PodcastLogoUrl;
            }

            set
            {
                if (value != m_PodcastLogoUrl)
                {
                    m_PodcastLogoUrl = value;

                    Debug.WriteLine("Getting twitter icon: " + m_PodcastLogoUrl);

                    // Fetch the remote podcast logo to store locally in the IsolatedStorage.
                    Uri podcastLogoUri = new Uri(m_PodcastLogoUrl, UriKind.Absolute);
                    WebClient wc = new WebClient();
                    wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_FetchPodcastLogoCompleted);
                    wc.OpenReadAsync(podcastLogoUri, podcastLogoUri);
                }
            }
        }

        private void wc_FetchPodcastLogoCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                Debug.WriteLine("ERROR: Cannot fetch channel logo.");
                return;
            }

            Debug.WriteLine("Storing podcast icon locally...");

            Stream logoInStream = null;
            try
            {
                logoInStream = (Stream)e.Result;
            }
            catch (WebException webEx)
            {
                if (webEx.Status != WebExceptionStatus.Success)
                {
                    Debug.WriteLine("ERROR: Web error occured. Cannot load image!");
                    return;
                }
            }

            // Parse the filename of the logo.
            string localPath = (e.UserState as Uri).LocalPath;
            string podcastLogoFilename      = localPath.Substring(localPath.LastIndexOf('/') + 1);
            string localPodcastLogoFilename = PODCAST_ICON_DIR + @"/" + podcastLogoFilename;

            Debug.WriteLine("Found icon filename: " + podcastLogoFilename);

            // Store the downloaded podcast logo to isolated storage for local cache.
            MemoryStream logoMemory = new MemoryStream();
            logoInStream.CopyTo(logoMemory);
            using (var isoFileStream = new IsolatedStorageFileStream(localPodcastLogoFilename, 
                                                                     FileMode.OpenOrCreate, 
                                                                     m_localPodcastIconCache))
            {
                isoFileStream.Write(logoMemory.ToArray(), 0, (int)logoMemory.Length);
            }

            m_PodcastLogoLocalLocation = localPodcastLogoFilename;
            Debug.WriteLine("Stored local podcast icon as: " + m_PodcastLogoLocalLocation);
            
            // Local cache has been updated - notify the UI that the logo property has changed.
            // and the new logo can be fetched to the UI. 
            NotifyPropertyChanged("PodcastLogo");
        }

        public BitmapImage PodcastLogo
        {
            get
            {
                // When we request for the podcast logo we will in fact fetch
                // the image from the local cache, create the BitmapImage object
                // and return that. 
                BitmapImage image = new BitmapImage();
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();
                string isoFilename = m_PodcastLogoLocalLocation;
                var stream = isoStore.OpenFile(isoFilename, System.IO.FileMode.Open);
                image.SetSource(stream);
                return image;
            }

            private set { }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}