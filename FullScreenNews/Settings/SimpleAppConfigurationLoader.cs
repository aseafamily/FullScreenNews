using FullScreenNews.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Settings
{
    public class SimpleAppConfigurationLoader : IAppConfigurationLoader
    {
        public AppConfiguration Configuration { get; set; } = new AppConfiguration();

        private ILoggerFacade Logger { get; set; }

        public SimpleAppConfigurationLoader(ILoggerFacade logger)
        {
            Logger = logger;
            Logger.LogType<SimpleAppConfigurationLoader>();

            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppConfiguration));
            ser.WriteObject(stream1, Configuration);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            string json = sr.ReadToEnd();

            Logger.Log(Environment.NewLine + JsonHelper.FormatJson(json), Category.Info, Priority.Medium);
        }

        public Task Load()
        {
            return null; 
        }

        public Task<bool> Save()
        {
            return null;
        }
    }
}
