using HigLabo.Net.Rss;
using HtmlAgilityPack;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FullScreenNews
{
    public class NewsArticles
    {
        private List<NewsArticle> articles = new List<NewsArticle>();
        private int current = 0;

        private static List<string> Rsses = new List<string>()
        {
            "https://news.google.com/news/feeds?output=rss",
            "http://feeds2.feedburner.com/businessinsider",
            "http://www.ifanr.com/feed",
            "http://feeds.feedburner.com/TheMoneyGame",
            "http://slickdeals.net/newsearch.php?mode=popdeals&searcharea=deals&searchin=first&rss=1",
            "http://feeds.feedburner.com/blogspot/wdMq",
            "http://www.codinghorror.com/blog/index.xml",
            "http://zhihurss.miantiao.me/dailyrss",
            "http://feeds.feedburner.com/MileNerd"
        };

        public NewsArticles()
        {   
        }

        public async Task SearchAsync()
        {
            List<NewsArticle> localArticles = new List<ServiceHelpers.NewsArticle>();

            this.IsLoading = true;

            RssClient cl = new RssClient();
            foreach (string url in Rsses)
            {
                try
                { 
                    var items = await cl.GetRssFeedAsync(new Uri(url));
                    foreach (var item in items.Items)
                    {
                        string img = null;
                        string description;

                        if (string.IsNullOrWhiteSpace(item.Description))
                        {
                            continue;
                        }

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml("<div>" + item.Description + "</div>");

                        var col = doc.DocumentNode.DescendantsAndSelf();
                        foreach (var node in col)
                        {
                            if (node.Name == "img")
                            {
                                var att = node.Attributes.Single(a => a.Name == "src");

                                if (att != null)
                                {
                                    img = node.Attributes.Single(a => a.Name == "src").Value;
                                    if (img.StartsWith("/"))
                                    {
                                        img = "http:" + img;
                                    }

                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }

                        description = doc.DocumentNode.InnerText;

                        // Remove HTML characters
                        description = Regex.Replace(description, "&nbsp;", " ").Trim();
                        description = Regex.Replace(description, @"\s{2,}", " ");

                        if (img == null)
                        {
                            if (items.Image != null && !string.IsNullOrWhiteSpace(items.Image.Url))
                            {
                                img = items.Image.Url;
                            }
                        }

                        localArticles.Add(new NewsArticle()
                        {
                            Title = item.Title,
                            Description = description,
                            Url = item.Link,
                            ThumbnailUrl = img,
                            Provider = items.Channel.Title,
                            Refreshed = DateTime.Now,
                            Published = item.PubDate.HasValue? item.PubDate.Value.DateTime : DateTime.Now
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }

                if (this.articles.Count == 0)
                {
                    this.articles = localArticles;
                }
            }

            // Let's shuffle the list
            Random rng = new Random();
            int n = localArticles.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                NewsArticle value = localArticles[k];
                localArticles[k] = localArticles[n];
                localArticles[n] = value;
            }

            articles = localArticles;
            //articles = await BingSearchHelper.GetNewsSearchResults("top stories", count: 100, offset: 0, market: "en-US");

            this.IsLoading = false;
        }

        public bool HasArticle
        {
            get
            {
                return (articles.Count() != 0);
            }
        }

        public bool IsLoading
        {
            get; set;
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
