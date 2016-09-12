using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherNet;
using WeatherNet.Clients;

namespace FullScreenNews
{
    public class OpenWeather
    {
        public const string ApiUrl = "http://api.openweathermap.org/data/2.5";
        public const string ApiKey = "07fdb03f093152bf3aeb51952ae9a1e7";

        public static async Task<List<WeatherResult>> GetWeather()
        {
            ClientSettings.ApiKey = ApiKey;
            ClientSettings.ApiUrl = ApiUrl;

            var result = await CurrentWeather.GetByCityIdAsync(5808079, "en", "imperial");
            var fileDays = await FiveDaysForecast.GetByCityIdAsync(5808079, "en", "imperial");

            if (result.Success && fileDays.Success)
            {
                List<WeatherResult> wr = new List<FullScreenNews.WeatherResult>();

                string temp = result.Item.Temp.ToString("N0");
                string iconUrl = string.Format("http://openweathermap.org/img/w/{0}.png", result.Item.Icon);

                wr.Add(new WeatherResult
                {
                    Temp = temp,
                    IconUrl = iconUrl,
                    Date = result.Item.Date
                });

                for (int i =7; i<40; i+=8)
                {
                    var r = fileDays.Items[i];
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

    public class WeatherResult
    {
        public string Temp { get; set; }
        public string IconUrl { get; set; }
        public DateTime Date { get; set; }
    }
}
