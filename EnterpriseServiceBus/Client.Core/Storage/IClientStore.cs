using System.Collections.Generic;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Utility.Plugins;

namespace GeoDecisions.Esb.Client.Core.Storage
{
    public interface IClientStore : IPlugin
    {
        int Priority { get; set; }

        bool IsAvailable { get; set; }

        void Init(Configuration config);

        bool SaveFailedMessage(EsbMessage message);

        bool DeleteFailedMessage(EsbMessage message);

        bool LogMessage(string message);

        List<EsbMessage> GetFailedMessages();
    }
}