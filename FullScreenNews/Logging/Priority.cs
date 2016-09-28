using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Logging
{
    public enum Priority
    {
        /// <summary>
        /// No priority specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// High priority entry.
        /// </summary>
        High = 1,

        /// <summary>
        /// Medium priority entry.
        /// </summary>
        Medium,

        /// <summary>
        /// Low priority entry.
        /// </summary>
        Low
    }
}
