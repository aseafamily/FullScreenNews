using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Settings
{
    [DataContract]
    public class AppConfiguration
    {
        [DataMember]
        public bool ShowChineseCalendar { get; set; } = true;

        [DataMember]
        public int UpdateWeatherInterval { get; set; } = 60 * 60;

        [DataMember]
        public int UpdateFeedInterval { get; set; } = 30;

        [DataMember]
        public int UpdateFeedSourcesInterval { get; set; } = 30 * 60;

        [DataMember]
        public int UpdateStockInterval { get; set; } = 2 * 60;

        [DataMember]
        public int UpdatePhotoInterval { get; set; } = 25;

        [DataMember]
        public int Alarminterval { get; set; } = 45 * 60;

        [DataMember]
        public string[] FeedSources { get; set; } = new string[]
        {
            "https://news.google.com/news/feeds?output=rss",
            "http://feeds2.feedburner.com/businessinsider",
            "http://www.ifanr.com/feed",
            "http://feeds.feedburner.com/TheMoneyGame",
            "http://slickdeals.net/newsearch.php?mode=popdeals&searcharea=deals&searchin=first&rss=1",
            "http://feeds.feedburner.com/blogspot/wdMq",
            //"http://zhihurss.miantiao.me/dailyrss",
            "http://feeds.feedburner.com/MileNerd"
        };

        [DataMember]
        public string[] StockSymbols { get; set; } = new string[]
        {
            "SPY",
            "MSFT",
            "NGD",
            "TWTR",
            "TSLA",
            "SCTY",
            "GOOG"
        };

        [DataMember]
        public string TwitterListUrl { get; set; } = "https://twitter.com/alexmajin/lists/everyday";

        [DataMember]
        public string[] VideoChannels { get; set; } = new string[]
        {
            "https://www.youtube.com/embed/CSafNIRagRU?autoplay=1",
            "https://www.youtube.com/embed/2QtdEq2tsh8?list=PLijOlz1lM3pzJjjR1jcIe5tfBYztEZIbY&autoplay=1",
            "https://www.youtube.com/embed/emsfNK3hVak?list=PLwXMmy5fUrVyay9LGp8sni6WhCaOZsfAX&autoplay=1",
            "https://www.youtube.com/embed/q7AiwWwiF_k?list=PLijOlz1lM3pzbT5NZ33h-S8PjQvrtOIDn&autoplay=1"
        };

        [DataMember]
        public string WorldClock1Name { get; set; } = "Beijing";

        [DataMember]
        public string WorldClock1Timezone { get; set; } = "China Standard Time";

        [DataMember]
        public string WorldClock2Name { get; set; } = "Greenwich";

        [DataMember]
        public string WorldClock2Timezone { get; set; } = "Greenwich Standard Time";
    }
}
