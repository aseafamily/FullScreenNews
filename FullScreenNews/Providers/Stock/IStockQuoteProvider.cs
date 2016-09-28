using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.Stock
{
    public interface IStockQuoteProvider
    {
        Task<List<Tick>> GetQuotes();
    }
}
