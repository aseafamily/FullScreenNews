using FullScreenNews.Logging;
using FullScreenNews.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers
{
    public abstract class BaseProvider
    {
        protected ILoggerFacade Logger;
        protected IAppConfigurationLoader AppConfigurationLoader;

        public BaseProvider(ILoggerFacade logger, IAppConfigurationLoader appConfigurationLoader)
        {
            Logger = logger;
            AppConfigurationLoader = appConfigurationLoader;
        }
    }
}
