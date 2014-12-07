using System.Collections.Concurrent;
using System.Collections.Generic;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Utility.Plugins;
using GeoDecisions.Esb.Server.Core.Models;

namespace GeoDecisions.Esb.Server.Core.Interfaces
{
    // this interface represents actions the transport implementors can do in there implementation
    public interface ITransportManager : IPlugin
    {
        SynchronizedCollection<ISubscriber> CurrentSubscribers { get; }

        SynchronizedCollection<IPublisher> CurrentPublishers { get; }

        //ConcurrentDictionary<string, Client> Clients { get; }

        SynchronizedCollection<Client> CurrentClients { get; }

        //Added this property, rxs, 04/12/2013
        SynchronizedCollection<EsbMessage> CurrentMessages { get; }

        ITransportManager CreateManager(Bus bus, Configuration config);

        //void RePublish(string channel, string message);
        void RePublish(string channel, EsbMessage message);

        void RePlay(string channel, EsbMessage message);

        void Shutdown();

        void AddSubscriber(IServerTransporter srcTransporter, ISubscriber subscriber);
        //Added this method, rxs, 04/12/2013
        void AddMessage(string channel, EsbMessage message);
        void RemoveSubscriber(IServerTransporter transporter, ISubscriber subscriber);
        void AddPublisher(IPublisher publisher);

        void ClearClient(string clientId);

        void AddClient(Client client);

        List<ISubscriber> RemoveSubscriberList(string uri);

        void RemoveClientList(string id);
    }
}