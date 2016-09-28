﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Settings
{
    public class AppConfiguration
    {
        public bool ShowChineseCalendar { get; set; } = true;

        public int UpdateWeatherInterval { get; set; } = 60 * 60;
        public int UpdateFeedInterval { get; set; } = 30;
        public int UpdateFeedSourcesInterval { get; set; } = 30 * 60;
        public int UpdateStockInterval { get; set; } = 2 * 60;
        public int UpdatePhotoInterval { get; set; } = 25;
        public int Alarminterval { get; set; } = 45 * 60;

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

        public string TwitterListUrl { get; set; } = "https://twitter.com/alexmajin/lists/everyday";

        public string WorldClock1Name = "Beijing";
        public string WorldClock1Timezone = "China Standard Time";
        public string WorldClock2Name = "Greenwich";
        public string WorldClock2Timezone = "Greenwich Standard Time";
    }
}
