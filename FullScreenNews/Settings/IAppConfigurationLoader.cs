using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Settings
{
    public interface IAppConfigurationLoader
    {
        AppConfiguration Configuration { get; set; }
    }
}
