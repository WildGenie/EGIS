using System;
using GeoDecisions.Esb.Common;

namespace GeoDecisions.Esb.Server.Console
{
    internal class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            System.Console.WriteLine("log:[{0}] - {1}", DateTime.UtcNow, message);
        }
    }
}