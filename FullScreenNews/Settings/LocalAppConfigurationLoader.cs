using FullScreenNews.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FullScreenNews.Settings
{
    public class LocalAppConfigurationLoader : IAppConfigurationLoader
    {
        private const string FileName = "DefaultConfiguration.json";

        public AppConfiguration Configuration { get; set; }

        private ILoggerFacade Logger { get; set; }

        public LocalAppConfigurationLoader(ILoggerFacade logger)
        {
            Logger = logger;
            Logger.LogType<LocalAppConfigurationLoader>();
        }

        public async Task Load()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.TryGetItemAsync(FileName) as StorageFile;

            if (file == null)
            {
                // Get from default place
                Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
                file = await folder.GetFileAsync(FileName);

                Logger.Log("Get default app configuration: " + file.Path, Category.Debug, Priority.Low);

                // save it to local folder
                await file.CopyAsync(localFolder);

                Logger.Log("Copy configuration to the local folder", Category.Debug, Priority.Low);
            }

            Logger.Log("Load app configuration: " + file.Path, Category.Debug, Priority.Low);

            using (FileStream stream = File.OpenRead(file.Path))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppConfiguration));
                stream.Position = 0;
                Configuration = (AppConfiguration)ser.ReadObject(stream);
            }

            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser1 = new DataContractJsonSerializer(typeof(AppConfiguration));
            ser1.WriteObject(stream1, Configuration);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            string json = sr.ReadToEnd();
            Logger.Log(Environment.NewLine + JsonHelper.FormatJson(json), Category.Info, Priority.Medium);
        }

        public async Task<bool> Save()
        {
            Logger.Log("Save new configuration", Category.Debug, Priority.Low);

            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser1 = new DataContractJsonSerializer(typeof(AppConfiguration));
            ser1.WriteObject(stream1, Configuration);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            string json = sr.ReadToEnd();
            Logger.Log(Environment.NewLine + JsonHelper.FormatJson(json), Category.Info, Priority.Medium);

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await localFolder.CreateFileAsync(FileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(file, json);

            return true;
        }
    }
}
