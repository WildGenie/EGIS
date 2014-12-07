using System;
using GeoDecisions.Esb.Common;
using NLog;

namespace GeoDecisions.Esb.Client.Core
{
    public class ClientLogger : IDetailLogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Log(string message)
        {
            logger.Log(LogLevel.Trace, message);
        }

        public void LogError(string message)
        {
            logger.Log(LogLevel.Error, message);
        }

        public void LogErrorStackTrace(string message)
        {
            logger.Log(LogLevel.Error, message + "Stacktrace={0}", Environment.StackTrace);
        }

        public void LogFatalStackTraceAndMore(string message)
        {
            //TODO Use event info with request information and stacktrace here
            logger.Log(LogLevel.Error, message + "Stacktrace={0}", Environment.StackTrace);
        }
    }
}