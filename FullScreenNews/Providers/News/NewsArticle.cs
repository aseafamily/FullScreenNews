using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.News
{
    public class NewsArticle
    {
        private DateTime _publishedOn;

        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Provider { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime Refreshed { get; set; }
        public DateTime Published
        {
            get
            {
                return _publishedOn.ToLocalTime();
            }
            set
            {
                _publishedOn = value;
            }
        }
    }
}
