using FullScreenNews.Logging;
using FullScreenNews.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherNet;
using WeatherNet.Clients;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace FullScreenNews.Providers.Weather
{
    public class OpenWeatherProvider : BaseProvider, IWeatherProvider
    {
        public const string ApiUrl = "http://api.openweathermap.org/data/2.5";
        public const string ApiKey = "07fdb03f093152bf3aeb51952ae9a1e7";

        public int TodayWeatherIconWidth { get; set; } = 70;
        public int ForcastWeatherIconWidth { get; set; } = 50;

        
        public OpenWeatherProvider(ILoggerFacade logger, IAppConfigurationLoader appConfigurationLoader)
            : base(logger, appConfigurationLoader)
        {
            logger.LogType<OpenWeatherProvider>();
        }

        public async Task<List<WeatherResult>> GetWeather()
        {
            ClientSettings.ApiKey = ApiKey;
            ClientSettings.ApiUrl = ApiUrl;

            string city = "Seattle";
            
            // Get location
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    Logger.Log("Waiting for update...", Category.Debug, Priority.Low);

                    // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                    Geolocator geolocator = new Geolocator();

                    // Carry out the operation.
                    Geoposition pos = await geolocator.GetGeopositionAsync();

                    if (pos.CivicAddress != null)
                    {
                        if (!string.IsNullOrEmpty(pos.CivicAddress.City))
                        {
                            city = pos.CivicAddress.City;
                        }
                    }
                    else
                    {
                        BasicGeoposition queryHint = new BasicGeoposition();
                        queryHint.Latitude = pos.Coordinate.Point.Position.Latitude;
                        queryHint.Longitude = pos.Coordinate.Point.Position.Longitude;
                        Geopoint hintPoint = new Geopoint(queryHint);

                        MapLocationFinderResult result =
                            await MapLocationFinder.FindLocationsAtAsync(hintPoint);

                        if (!string.IsNullOrWhiteSpace(result.Locations[0].Address.Town))
                        {
                            city = result.Locations[0].Address.Town;
                        }
                    }

                    break;

                case GeolocationAccessStatus.Denied:
                    Logger.Log("Access to location is denied.", Category.Warn, Priority.Medium);
                    
                    break;

                case GeolocationAccessStatus.Unspecified:
                    Logger.Log("Unspecified error.", Category.Exception, Priority.Medium);
                    break;
            }

            Logger.Log("Open weather city is " + city, Category.Debug, Priority.Low);

            List<WeatherResult> wr = new List<WeatherResult>();

            try
            {

                var result = await CurrentWeather.GetByCityNameAsync(city, "us", "en", "imperial");
                var fiveDays = await FiveDaysForecast.GetByCityNameAsync(city, "us", "en", "imperial");

                if (result.Success && fiveDays.Success)
                {
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

                    for (int i = 7; i < 40; i += 8)
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
                }
            }
            catch (Exception ex)
            {
                Logger.Log("OpenWeather call failed\n" + ex.ToString(), Category.Exception, Priority.High);
            }

            return wr;

        }
    }
}
