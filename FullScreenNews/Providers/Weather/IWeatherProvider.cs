using FullScreenNews.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.Weather
{
    public interface IWeatherProvider
    {
        int TodayWeatherIconWidth { get; set; }
        int ForcastWeatherIconWidth { get; set; }

        Task<List<WeatherResult>> GetWeather();
    }
}
