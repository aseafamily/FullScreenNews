using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.Weather
{
    public class WeatherProvider
    {
        public static int TodayWeatherIconWidth { get; set; } = NationalWeather.TodayWeatherIconWidth;
        public static int ForcastWeatherIconWidth { get; set; } = NationalWeather.ForcastWeatherIconWidth;

        public static async Task<List<WeatherResult>> GetWeather()
        {
            //return await OpenWeather.GetWeather();
            return await NationalWeather.GetWeather();
        }
    }
}
