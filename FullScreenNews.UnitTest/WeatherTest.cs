using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Net.Http;
using System.Threading.Tasks;
using FullScreenNews.Providers.Weather;
using System.Collections.Generic;
using FullScreenNews.Logging;
using FullScreenNews.Settings;

namespace FullScreenNews.UnitTest
{
    [TestClass]
    public class WeatherTest
    {
        [TestMethod]
        public void TestNationalWeather()
        {
            Task.Run(async () =>
            {
                NationalWeatherProvider w = new NationalWeatherProvider(new DebugLogger(), new SimpleAppConfigurationLoader(new DebugLogger()));

                var results = await w.GetWeather();

                Assert.IsTrue(results.Count > 1);

            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestOpenWeather()
        {
            /*
            Task.Run(async () =>
            {
                List<WeatherResult> results = await IWeatherProvider.GetWeather();

                Assert.IsTrue(results.Count > 1);

            }).GetAwaiter().GetResult();
            */
        }
    }
}
