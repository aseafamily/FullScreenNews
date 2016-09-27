using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FullScreenNews.Providers.Weather
{
    public class NationalWeather
    {
        public const int TodayWeatherIconWidth = 55;
        public const int ForcastWeatherIconWidth = 40;

        public static async Task<List<WeatherResult>> GetWeather()
        {
            var url = "http://forecast.weather.gov/MapClick.php?lat=47.678&lon=-122.1256&unit=0&lg=english&FcstType=dwml";

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "FullScreenNews/1.0");
            var xml = await client.GetStreamAsync(url);

            XDocument doc = XDocument.Load(xml);

            WeatherResult current = null;
            List<WeatherResult> forcasts = new List<WeatherResult>();

            foreach (var elem in doc.Root.Elements("data"))
            {
                var attribute = elem.Attribute("type");
                if (attribute.Value == "current observations")
                {
                    current = new WeatherResult();
                    foreach (var t in elem.Descendants("temperature"))
                    {
                        var a = t.Attribute("type");
                        if (a.Value == "apparent")
                        {
                            current.Temp = t.Element("value").Value;
                            break;
                        }
                    }

                    var icon = elem.Descendants("conditions-icon");
                    if (icon != null)
                    {
                        var link = icon.Elements("icon-link").FirstOrDefault();
                        if (link != null)
                        {
                            current.IconUrl = link.Value;
                        }
                    }
                }

                if (attribute.Value == "forecast")
                {
                    foreach (var layout in elem.Elements("time-layout"))
                    {
                        var layoutKey = layout.Element("layout-key");
                        if (layoutKey != null && layout.Value.Contains("p12h"))
                        {
                            foreach (var start in layout.Elements("start-valid-time"))
                            {
                                forcasts.Add(new WeatherResult()
                                {
                                    Date = DateTime.Parse(start.Value)
                                });
                            }

                            break;
                        }
                    }

                    int forcastsIndex = 0;
                    var icons = elem.Descendants("conditions-icon").FirstOrDefault().Elements("icon-link");
                    foreach (var icon in icons)
                    {
                        if (forcastsIndex < forcasts.Count)
                        {
                            forcasts[forcastsIndex++].IconUrl = icon.Value;
                        }
                    }

                    var temps = elem.Descendants("temperature").ToArray();
                    List<string> temp1 = new List<string>();
                    List<string> temp2 = new List<string>();

                    if (temps.Length == 2)
                    {
                        foreach (var t in temps[0].Elements("value"))
                        {
                            temp1.Add(t.Value);
                        }

                        foreach (var t in temps[1].Elements("value"))
                        {
                            temp2.Add(t.Value);
                        }
                    }

                    for (int i = 0; i < temp1.Count; i++)
                    {
                        forcastsIndex = i * 2;
                        if (forcastsIndex < forcasts.Count)
                        {
                            forcasts[forcastsIndex].Temp = temp1[i];
                        }
                    }

                    for (int i = 0; i < temp2.Count; i++)
                    {
                        forcastsIndex = i * 2 + 1;
                        if (forcastsIndex < forcasts.Count)
                        {
                            forcasts[forcastsIndex].Temp = temp2[i];
                        }
                    }
                }
            }

            // return now
            List<WeatherResult> results = new List<WeatherResult>();

            int dateDiff = 0;
            DateTime date = DateTime.Now;

            current.Date = date;
            results.Add(current);

            foreach (var r in forcasts)
            {
                date = current.Date.AddDays(dateDiff);

                if (date.DayOfYear != r.Date.DayOfYear)
                {
                    results.Add(r);

                    dateDiff++;
                }
            }

            return results;
        }
    }
}
