using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace LiveFrame
{
    // http://finance.yahoo.com/d/quotes.csv?s=MSFT+GOOG&f=snl1t1p2&e=.csv
    public class YahooStockQuoteProvider
    {
        private static string QuotesUrlFormat = "http://finance.yahoo.com/d/quotes.csv?s={0}&f=snl1t1p2&e=.csv";

        public async Task<List<Tick>> GetQuotes()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Linux; Android 4.0.4; Galaxy Nexus Build/IMM76B) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.133 Mobile Safari/535.19");

            string ticks = "SPY+MSFT+NGD+TSLA";
            string url = string.Format(QuotesUrlFormat, ticks);

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

    public class Tick
    {
        public string Symbol { get; set; }
        public string Price { get; set; }
        public string Change { get; set; }
        public bool IsUp
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Change))
                {
                    return Change.ToCharArray()[0] != '-';
                }

                return false;
            }
        }
    }

    public class Ticker
    {
        public string Symbol { get; set; }

        public string Price { get; set; }

        public string Up { get; set; }

        public SolidColorBrush Color { get; set; }
    }
}
