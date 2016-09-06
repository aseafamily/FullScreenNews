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

        public static async Task<WeatherResult> GetWeather()
        {
            ClientSettings.ApiKey = ApiKey;
            ClientSettings.ApiUrl = ApiUrl;

            var result = await CurrentWeather.GetByCityIdAsync(5808079, "en", "imperial");

            if (result.Success)
            {
                string temp = string.Format("{0} ({1} - {2})", result.Item.Temp.ToString("N0"), result.Item.TempMin.ToString("N0"), result.Item.TempMax.ToString("N0"));
                string iconUrl = string.Format("http://openweathermap.org/img/w/{0}.png", result.Item.Icon);

                return new WeatherResult
                {
                    Temp = temp,
                    IconUrl = iconUrl
                };
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
    }
}
