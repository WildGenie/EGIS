using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GeoDecisions.Esb.Common.Messages.Configuration
{
    [DataContract]
    internal class ServerConfigMessage
    {
        [DataMember(Name = "serverName")]
        public string ServerName { get; set; }

        [DataMember(Name = "timestamp")]
        public DateTime Timestamp { get; set; }

        [DataMember(Name = "transporterSettings")]
        public List<TransporterSettings> TransporterSettings { get; set; }
    }

    [DataContract]
    internal class TransporterSettings
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "port")]
        public int Port { get; set; }

        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }
    }
}