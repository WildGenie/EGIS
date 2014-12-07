namespace GeoDecisions.Esb.Common
{
    public interface IDetailLogger : ILogger
    {
        void LogError(string message);

        void LogErrorStackTrace(string message);

        void LogFatalStackTraceAndMore(string message);
    }
}