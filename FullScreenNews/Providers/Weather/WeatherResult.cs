using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.Weather
{
    public class WeatherResult
    {
        public string Temp { get; set; }
        public string IconUrl { get; set; }
        public DateTime Date { get; set; }
    }
}
