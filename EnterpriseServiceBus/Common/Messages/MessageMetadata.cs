using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GeoDecisions.Esb.Common.Messages
{
    [DataContract]
    public class MessageMetadata
    {
        [DataMember(Name = "uri")]
        public string Uri { get; set; }

        [DataMember(Name = "fields")]
        public List<string> Fields { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }

        //[DataMember(Name = "type")]
        //public string Type { get; set; }
    }
}