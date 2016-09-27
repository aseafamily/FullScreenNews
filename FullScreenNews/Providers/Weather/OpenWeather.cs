using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherNet;
using WeatherNet.Clients;

namespace FullScreenNews.Providers.Weather
{
    public class OpenWeather
    {
        public const string ApiUrl = "http://api.openweathermap.org/data/2.5";
        public const string ApiKey = "07fdb03f093152bf3aeb51952ae9a1e7";

        public const int TodayWeatherIconWidth = 70;
        public const int ForcastWeatherIconWidth = 50;

        public static async Task<List<WeatherResult>> GetWeather()
        {
            ClientSettings.ApiKey = ApiKey;
            ClientSettings.ApiUrl = ApiUrl;

            var result = await CurrentWeather.GetByCityNameAsync("Seattle", "us", "en", "imperial");
            var fiveDays = await FiveDaysForecast.GetByCityNameAsync("Seattle", "us", "en", "imperial");

            if (result.Success && fiveDays.Success)
            {
                List<WeatherResult> wr = new List<WeatherResult>();

                string temp = result.Item.Temp.ToString("N0");
                string iconUrl = string.Format("http://openweathermap.org/img/w/{0}.png", result.Item.Icon);

                wr.Add(new WeatherResult
                {
                    Temp = temp,
                    IconUrl = iconUrl,
                    Date = result.Item.Date
                });

                if (fiveDays.Items.Count() == 0)
                {
                    return null;
                }

                for (int i =7; i<40; i+=8)
                {
                    int index = i;
                    if (index >= fiveDays.Items.Count())
                    {
                        index = fiveDays.Items.Count() - 1;
                    }

                    var r = fiveDays.Items[index];
                    wr.Add(new WeatherResult
                    {
                        Temp = r.Temp.ToString("N0"),
                        IconUrl = string.Format("http://openweathermap.org/img/w/{0}.png", r.Icon),
                        Date = r.Date
                    });
                }

                return wr;
            }
            else
            {
                return null;
            }
        }
    }
}
