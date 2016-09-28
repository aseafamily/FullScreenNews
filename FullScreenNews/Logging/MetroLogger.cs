using MetroLog;
using MetroLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Logging
{
    public class MetroLogger : ILoggerFacade
    {
        private ILogger Logger;

        public void LogType<T>()
        {
            Logger = LogManagerFactory.DefaultLogManager.GetLogger<T>();
        }

        public void Log(string message, Category category, Priority priority)
        {
            switch (category)
            {
                case Category.Debug:
                    this.Logger.Debug(message);
                    break;

                case Category.Exception:
                    if (priority == Priority.High)
                    {
                        this.Logger.Fatal(message);
                    }
                    else
                    {
                        this.Logger.Error(message);
                    }

                    break;

                case Category.Info:
                    this.Logger.Info(message);
                    break;

                case Category.Warn:
                    this.Logger.Warn(message);
                    break;

                default:
                    break;
            }
        }
    }
}
