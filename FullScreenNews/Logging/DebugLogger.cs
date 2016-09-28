﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullScreenNews.Logging
{
    /// <summary>
    /// Implementation of <see cref="ILoggerFacade"/> that logs into a message into the Debug.Listeners collection.
    /// </summary>
    public class DebugLogger : ILoggerFacade
    {
        public void LogType<T>()
        {
        }

        /// <summary>
        /// Write a new log entry with the specified category and priority.
        /// </summary>
        /// <param name="message">Message body to log.</param>
        /// <param name="category">Category of the entry.</param>
        /// <param name="priority">The priority of the entry.</param>
        public void Log(string message, Category category, Priority priority)
        {
            string messageToLog = String.Format(CultureInfo.InvariantCulture, "{1}: {2}. Priority: {3}. Timestamp:{0:u}.", DateTime.Now,
                                                category.ToString().ToUpper(), message, priority);

            Debug.WriteLine(messageToLog);
        }
    }
}
