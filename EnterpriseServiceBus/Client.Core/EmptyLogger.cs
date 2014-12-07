using GeoDecisions.Esb.Common;

namespace GeoDecisions.Esb.Client.Core
{
    internal class EmptyLogger : ILogger
    {
        public void Log(string message)
        {
            return;
        }
    }
}