using Microsoft.Live;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Podcatcher.OneDrive
{
    internal class OneDriveManager
    {
        private LiveConnectClient liveConnect = null;

        private static OneDriveManager m_instance = null;
        public static OneDriveManager getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new OneDriveManager();
            }

            return m_instance;
        }

        async public Task loginUserToOneDrive()
        {
            LiveAuthClient auth = new LiveAuthClient(App.LSKEY_LIVE_CLIENT_ID);
            LiveLoginResult loginResult = await auth.LoginAsync(new string[] { "wl.signin", "wl.SkyDrive_update" });
            if (loginResult.Status != LiveConnectSessionStatus.Connected)
            {
                Debug.WriteLine("Could not finish OneDrive login.");
                MessageBox.Show("Sorry. Could not log in to OneDrive. Please try again.");
            }
            else
            {
                liveConnect = new LiveConnectClient(auth.Session);
            }
        }

        async public Task<bool> userIsLoggedToOneDrive()
        {
            if (liveConnect == null)
            {
                return false;
            }

            var auth = new LiveAuthClient(App.LSKEY_LIVE_CLIENT_ID);
            var result = await auth.InitializeAsync(new[] { "wl.signin", "wl.SkyDrive_update" });
            if (result.Status == LiveConnectSessionStatus.NotConnected)
            {
                return false;
            }

            return true;
        }

        async public Task<IEnumerable<OneDriveFile>> getFileListing(String oneDrivePath)
        {
            IEnumerable<OneDriveFile> files = Enumerable.Empty<OneDriveFile>();
            LiveOperationResult result = await liveConnect.GetAsync(oneDrivePath);

            if (result.Result.ContainsKey("data"))
            {
                JObject json = JObject.Parse(result.RawResult);
                files = json["data"].Values<JObject>()
                    .Select(file => new OneDriveFile {
                                        Id = (String)file["id"],
                                        Name = ((String)file["name"]),
                                        Created = DateTime.Parse((String)file["updated_time"]),
                                        Url = (String)file["source"]
                                    });
            }

            return files;
        }

        async public Task uploadFile(String path, String filename, Stream bytes)
        {
            await liveConnect.UploadAsync(path, filename, bytes, OverwriteOption.Overwrite);
        }

        internal class OneDriveFile
        {
            public String Id { get; set; }
            public String Name { get; set; }
            public DateTime Created { get; set; }
            public String Url { get; set; }
        }
    }
}
