using System;
using System.Collections.Generic;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Utility.Plugins;

namespace GeoDecisions.Esb.Client.Core.Interfaces
{
    public interface IComHandler : IPlugin
    {
        string Name { get; }
        int Priority { get; set; }

        IComHandler Successor { get; set; }
        IComHandler CreateHandler(Configuration config);

        bool Connect();

        bool CanHandle(Subscribe subscribe);
        bool CanHandle(EsbMessage message);

        string DispatchMessage(EsbMessage message, string uri = null);
        void DispatchSubscribe<T>(Subscribe subscribe, Action<T, EsbMessage> callback);
        void DispatchUnSubscribe(UnSubscribe unSubscribe);

        bool Shutdown();
        void Replay<T>(string uri, double dDays, Action<List<T>, List<EsbMessage>> callback);
    }
}