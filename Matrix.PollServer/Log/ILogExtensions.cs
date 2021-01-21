﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Matrix.PollServer
{
    public static class ILogExtentions
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Trace(this ILog log, string message, Exception exception)
        {
            //log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
            //    log4net.Core.Level.Trace, message, exception);
        }

        public static void Trace(this ILog log, string message)
        {
            log.Trace(message, null);
        }

        public static void Verbose(this ILog log, string message, Exception exception)
        {
            //log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
            //    log4net.Core.Level.Verbose, message, exception);
        }

        public static void Verbose(this ILog log, string message)
        {
            log.Verbose(message, null);
        }
    }
}
