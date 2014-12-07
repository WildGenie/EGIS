namespace GeoDecisions.Esb.Common.Storage.Entities
{
    internal class PublisherEntity
    {
        public virtual string Id { get; protected set; }
        public virtual string MachineName { get; set; }
        public virtual string ClientId { get; set; }
    }
}