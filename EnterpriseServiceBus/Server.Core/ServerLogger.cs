using System;
using GeoDecisions.Esb.Common;
using NLog;

namespace GeoDecisions.Esb.Server.Core
{
    public class ServerLogger : IDetailLogger
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
            logger.Log(LogLevel.Error, message + ", Stacktrace={0}", Environment.StackTrace);
        }

        public void LogFatalStackTraceAndMore(string message)
        {
            var eventInfo = new LogEventInfo(LogLevel.Error, "EventLogger", "");
            eventInfo.Level = LogLevel.Fatal;
            //eventInfo.Properties["requestType"] = 
            //eventInfo.Properties["requestUrl"] = 
            //eventInfo.Properties["requestQuerystring"] = 
            //eventInfo.Properties["requestStatus"] = 
            eventInfo.Properties["message"] = "This is a Fatal error";
            eventInfo.Properties["stackTrace"] = Environment.StackTrace;

            logger.Log(eventInfo);
        }
    }
}