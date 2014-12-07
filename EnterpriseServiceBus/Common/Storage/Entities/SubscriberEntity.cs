using System;

namespace GeoDecisions.Esb.Common.Storage.Entities
{
    internal class SubscriberEntity
    {
        public virtual string Id { get; protected set; }
        public virtual string Uri { get; set; }
        public virtual string MachineName { get; set; }
        public virtual string ClientId { get; set; }
        public virtual DateTime LastHeartbeat { get; set; }
    }
}