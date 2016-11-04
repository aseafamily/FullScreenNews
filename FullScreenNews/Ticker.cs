using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace FullScreenNews
{
    public class Ticker
    {
        public string Symbol { get; set; }

        public string Price { get; set; }

        public string Up { get; set; }

        public SolidColorBrush Color { get; set; }
    }
}
