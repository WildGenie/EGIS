using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GeoDecisions.Esb.Common.Messages
{
    //[DataContract]
    //public class Subscribe
    //{
    //    public Subscribe()
    //    {
    //    }

    //    [DataMember(Name = "uri")]
    //    public string Uri { get; set; }

    //    [DataMember(Name = "author")]
    //    public string Author { get; set; }
    //}


    [DataContract]
    public class SubscriptionResponse
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "messages")]
        public List<string> Messages { get; set; }
    }


    [DataContract]
    public sealed class Subscription : SubscriptionResponse
    {
        [DataMember(Name = "uri")]
        public string Uri { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }
    }


    //[DataContract]
    //public class SubscribeRequest
    //{
    //    [DataMember(Name = "status")]
    //    public string Status { get; set; }

    //    [DataMember(Name = "messages")]
    //    public List<string> Messages { get; set; }
    //}

    //[DataContract]
    public class Subscribe
    {
        private const string INVALID_MESSAGE_URI_ERROR_TEXT = "Invalid message uri";

        public Subscribe()
        {
        }

        public Subscribe(string messageUri, string machineName, string clientId)
        {
            Type = "Subscribe";

            // format: author/category/messageName

            if (string.IsNullOrEmpty(messageUri))
                throw new Exception(INVALID_MESSAGE_URI_ERROR_TEXT);

            string[] msgSegs = messageUri.Split('/');

            if (msgSegs.Length != 3)
                throw new Exception(INVALID_MESSAGE_URI_ERROR_TEXT);

            string author = msgSegs[0];
            string category = msgSegs[1];
            string messageName = msgSegs[2];

            Author = author;
            Category = category;
            MessageName = messageName;
            MachineName = machineName;
            ClientId = clientId;

            Uri = messageUri;
        }

        public string Type { get; set; }
        public string MessageId { get; set; }
        public string Uri { get; set; }
        public SubscriptionTypes SubscriptionType { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string MessageName { get; set; }
        public string MachineName { get; set; }
        public string ClientId { get; set; }

        //public Action<EsbMessage> Action { get; set; }
    }

    //[DataContract]
    public class UnSubscribe
    {
        private const string INVALID_MESSAGE_URI_ERROR_TEXT = "Invalid message uri";

        public UnSubscribe()
        {
            Type = "UnSubscribe";
        }

        public string Type { get; set; }
        public string Uri { get; set; }
        public string MachineName { get; set; }
        public string ClientId { get; set; }
    }


    public enum SubscriptionTypes
    {
        Temporary,
        Persistant
    }
}