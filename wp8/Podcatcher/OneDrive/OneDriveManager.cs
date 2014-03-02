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

        async public Task<IEnumerable<OneDriveFile>> getFileListing(String oneDrivePath)
        {
            await loginIfNecessary();

            IEnumerable<OneDriveFile> files = Enumerable.Empty<OneDriveFile>();
            LiveOperationResult result = await liveConnect.GetAsync(oneDrivePath);

            if (result.Result.ContainsKey("data"))
            {
                JObject json = JObject.Parse(result.RawResult);
                files = json["data"].Values<JObject>()
                    .Select(file => new OneDriveFile
                    {
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
            await loginIfNecessary();
            await liveConnect.UploadAsync(path, filename, bytes, OverwriteOption.Overwrite);
        }

        async public Task<String> createFolderIfNotExists(String folderName, String rootFolder="me/skydrive")
        {
            await loginIfNecessary();

            String createFolder = "";
            if (folderName.Contains('/'))
            {
                createFolder = folderName.Substring(0, folderName.IndexOf('/'));
                folderName = folderName.Substring(folderName.IndexOf('/') + 1);
            }
            else
            {
                createFolder = folderName;
                folderName = "";
            }

            String folderId = await getFolderIdForName(createFolder);
            dynamic createdFolderResult = "";
            if (String.IsNullOrEmpty(folderId))
            {
                // Folder does not exist, create it.
                var newFolder = new Dictionary<string, object>();
                newFolder.Add("name", createFolder);

                LiveOperationResult request = await liveConnect.PostAsync(rootFolder, newFolder);
                createdFolderResult = request.Result;
                folderId = createdFolderResult.id as String;
                Debug.WriteLine("Created folder id: " + folderId);
            }

            if (folderName.Length > 0)
            {
                return await createFolderIfNotExists(folderName, folderId);
            }
            else
            {
                return folderId;
            }
        }

        async public Task<String> getFolderIdForName(String folderName)
        {
            await loginIfNecessary();

            LiveOperationResult request = await liveConnect.GetAsync("me/skydrive/files?filter=folders");
            var iEnum = request.Result.Values.GetEnumerator();
            iEnum.MoveNext();
            var folders = iEnum.Current as IEnumerable<object>;

            foreach (dynamic folder in folders)
            {
                if (folder.name == folderName)
                {
                    // Folder exists.
                    return folder.id;
                }
            }

            return "";
        }

        async public Task uploadFileBackground(String folderId, Uri localFileUri)
        {
            LiveOperationResult res = await liveConnect.BackgroundUploadAsync(folderId, localFileUri, OverwriteOption.Overwrite);
        }

        async private Task loginUserToOneDrive()
        {
            try
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
            catch (LiveAuthException e)
            {
                Debug.WriteLine("User did not log in or got some other Live exception. Cannot import subscriptions.");
                MessageBox.Show("Authentication to OneDrive was not successful. Thus, cannot import your subscriptions. Please try again.");
                return;
            }
            catch (LiveConnectException e)
            {
                Debug.WriteLine("Error communicating to OneDrive. Cannot import subscriptions.");
                MessageBox.Show("Error communicating to OneDrive. Cannot login user. Please try again.");
                return;
            }
        }

        async private Task<bool> userIsLoggedToOneDrive()
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

        async private Task loginIfNecessary()
        {
            if (await userIsLoggedToOneDrive() == false)
            {
                await loginUserToOneDrive();
            }
        }
    }
}
