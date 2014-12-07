using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using NLog;
using System.Web;

namespace GeoDecisions.Esb.Server.Console
{
    internal class ServerLogger : IDetailLogger
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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
            LogEventInfo eventInfo = new LogEventInfo(LogLevel.Error, "EventLogger", "");
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
