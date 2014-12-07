using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Utility.Plugins;

namespace GeoDecisions.Esb.Server.Core.Interfaces
{
    public interface IServerTransporter : IPlugin
    {
        string Name { get; }

        string Host { get; }

        int Port { get; }
        bool Enabled { get; set; }
        IServerTransporter CreateTransporter(ITransportManager manager, Configuration config);

        bool InterestedIn(string msgUri);

        void Initialize();

        void RePublish(string channel, EsbMessage message);

        bool Shutdown();

        void HandleSubscribe(IServerTransporter srcTransporter, ISubscriber subscriber);
        void HandleUnSubscribe(IServerTransporter srcTransporter, ISubscriber subscriber);

        //void HandleUnSubscribeAll(IServerTransporter srcTransporter, string clientId);
        void HandleClientShutdown(IServerTransporter srcTransporter, string clientId);

        void Configure(ITransportConfig transportConfig);
        void RePlay(string channel, EsbMessage message);
    }

    public interface ITransportConfig
    {
        //Dictionary<string, object> ConfigAsDictionary();
    }
}