using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Net.Http;
using System.Threading.Tasks;
using FullScreenNews.Providers.Weather;
using System.Collections.Generic;

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
                var results = await NationalWeather.GetWeather();

                Assert.IsTrue(results.Count > 1);

            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestWeatherProvider()
        {
            Task.Run(async () =>
            {
                List<WeatherResult> results = await WeatherProvider.GetWeather();

                Assert.IsTrue(results.Count > 1);

            }).GetAwaiter().GetResult();
        }
    }
}
