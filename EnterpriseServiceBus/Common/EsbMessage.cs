using System;
using System.Collections.Generic;
using GeoDecisions.Esb.Common.Messages;

namespace GeoDecisions.Esb.Common
{
    public sealed class EsbMessage //: IEsbMessage
    {
        public string Id { get; set; }
        public string Uri { get; set; }
        //public string Name { get; set; }
        //public string Author { get; set; }
        public string Type { get; set; }
        public string ReplyTo { get; set; }
        public int? Priority { get; set; }
        public int? RetryAttempts { get; set; }
        public DateTime PublishedDateTime { get; set; }
        public Publisher Publisher { get; set; }
        public byte[] Payload { get; set; }

        public string HandlerId { get; set; }

        public List<string> Errors { get; set; }

        public string ClientId { get; set; }
    }
}