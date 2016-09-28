using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Providers.Stock
{
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
}
