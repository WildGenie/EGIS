using System;
using GeoDecisions.Esb.Common.Messages;

namespace GeoDecisions.Esb.Common.Storage.Entities
{
    internal class MessageEntity
    {
        public virtual string Id { get; set; }
        public virtual string Uri { get; set; }
        public virtual string Type { get; set; }
        public virtual string ReplyTo { get; set; }
        public virtual int? Priority { get; set; }
        public virtual int? RetryAttempts { get; set; }
        public virtual DateTime PublishedDateTime { get; set; }
        public virtual string Publisher { get; set; }
        public virtual byte[] Payload { get; set; }
        public virtual string HandlerId { get; set; }
    }
}