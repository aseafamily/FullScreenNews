using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.News
{
    public interface INewsProvider
    {
        bool HasArticle { get; }

        bool IsLoading { get; set; }

        string ArticleIndex { get; }

        NewsArticle Current { get; }

        void MoveNext();

        void MovePrevious();

        Task SearchAsync();
    }
}
