using System;

namespace GeoDecisions.Esb.Common.Messages
{
    public interface ISubscriber
    {
        string Uri { get; set; }
        string MachineName { get; set; }
        string ClientId { get; set; }
        DateTime LastHeartbeat { get; set; }
    }

    public class Subscriber : ISubscriber
    {
        //public string SubscriptionId { get; set; }
        public string Uri { get; set; }
        public string MachineName { get; set; }
        public string ClientId { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }

    public class RedisSubscriber : ISubscriber
    {
        public bool IsBroker { get; set; }
        public string Uri { get; set; }
        public string MachineName { get; set; }
        public string ClientId { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}