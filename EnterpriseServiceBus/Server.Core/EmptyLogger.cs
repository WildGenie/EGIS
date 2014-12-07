using GeoDecisions.Esb.Common;

namespace GeoDecisions.Esb.Server.Core
{
    internal class EmptyLogger : IDetailLogger
    {
        public void Log(string message)
        {
            return;
        }

        public void LogError(string message)
        {
            return;
        }

        public void LogErrorStackTrace(string message)
        {
            return;
        }

        public void LogFatalStackTraceAndMore(string message)
        {
            return;
        }
    }
}