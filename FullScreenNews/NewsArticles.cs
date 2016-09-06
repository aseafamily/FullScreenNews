using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews
{
    public class NewsArticles
    {
        private List<NewsArticle> articles = new List<NewsArticle>();
        private int current = 0;

        public NewsArticles()
        {   
        }

        public async Task SearchAsync()
        {
            /*
            var cl = new RssClient();
            cl.GetRssFeed(new Uri("http://feeds2.feedburner.com/businessinsider"), fd =>
            {
                foreach (var item in fd.Items)
                {
                    //Read item property(Title,Link,PubDate) and do something.
                }
            });
            */

            articles = await BingSearchHelper.GetNewsSearchResults("top stories", count: 100, offset: 0, market: "en-US");
        }

        public bool HasArticle
        {
            get
            {
                return (articles.Count() != 0);
            }
        }

        public NewsArticle Current
        {
            get
            {
                if (articles.Count() != 0)
                {
                    if (this.articles.Count() <= current)
                    {
                        current = 0;
                    }

                    return this.articles[current];
                }
                else
                {
                    return null;
                }
            }
        }

        public void MoveNext()
        {
            current++;

            if (current == articles.Count())
            {
                current = 0;
            }
        }

        public void MovePrevious()
        {
            if (current > 0)
            {
                current--;
            }
            else
            {
                current = articles.Count() - 1;
            }
        }

        public string ArticleIndex
        {
            get
            {
                return string.Format("{0}/{1}", current + 1, articles.Count());
            }
        }
    }
}
