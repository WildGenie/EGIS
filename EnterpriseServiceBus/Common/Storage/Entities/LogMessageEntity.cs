namespace GeoDecisions.Esb.Common.Storage.Entities
{
    internal class LogMessageEntity
    {
        public virtual string Id { get; protected set; }
        public virtual string MessageText { get; set; }
        public virtual string DateTime { get; set; }
        public virtual string MachineName { get; set; }
    }
}