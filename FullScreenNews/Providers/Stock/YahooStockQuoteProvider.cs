using FullScreenNews.Logging;
using FullScreenNews.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.Stock
{
    // http://finance.yahoo.com/d/quotes.csv?s=MSFT+GOOG&f=snl1t1p2&e=.csv
    public class YahooStockQuoteProvider : BaseProvider, IStockQuoteProvider
    {
        private static string QuotesUrlFormat = "http://finance.yahoo.com/d/quotes.csv?s={0}&f=snl1t1p2&e=.csv";

        private string[] StockSymbols;

        public YahooStockQuoteProvider(ILoggerFacade logger, IAppConfigurationLoader appConfigurationLoader)
            : base(logger, appConfigurationLoader)
        {
            Logger.LogType<YahooStockQuoteProvider>();
            this.StockSymbols = appConfigurationLoader.Configuration.StockSymbols;
        }

        public async Task<List<Tick>> GetQuotes()
        {
            HttpClient client = new HttpClient();

            //string ticks = "SPY+MSFT+NGD+TWTR+TSLA+SCTY+GOOG";
            string ticks = string.Join("+", this.StockSymbols);
            string url = string.Format(QuotesUrlFormat, ticks);

            Logger.Log("Request URL is: " + url, Category.Debug, Priority.Low);

            List<Tick> tickList = new List<Tick>();

            var result = await client.GetAsync(url);
            result.EnsureSuccessStatusCode();
            string csv = await result.Content.ReadAsStringAsync();

            // "\"SPY\",\"SPDR S&P 500\",216.70,\"2:02pm\",\"-0.46%\"\n\"MSFT\",\"Microsoft Corporation\",57.807,\"2:02pm\",\"-0.624%\"\n\"NGD\",\"New Gold Inc.\",5.03,\"2:02pm\",\"-1.76%\"\n"
            //csv.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
            string[] entries = csv.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                var fields = Regex.Split(entry, "[\t,](?=(?:[^\"]|\"[^\"]*\")*$)")
                    .Select(s => Regex.Replace(s.Replace("\"\"", "\""), "^\"|\"$", "")).ToArray();
                
                if (fields.Length == 5)
                {
                    tickList.Add(new Tick
                    {
                        Symbol = fields[0],
                        Price = fields[2],
                        Change = fields[4]
                    });
                }
            }


            return tickList;
        }
    }
}
