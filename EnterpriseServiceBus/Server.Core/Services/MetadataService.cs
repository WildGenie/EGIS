using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Timers;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Services;

namespace GeoDecisions.Esb.Server.Core.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, AddressFilterMode = AddressFilterMode.Any)]
    internal class MetadataService : IMetadataService
    {
        //IMetadataCallback _callback = null;

        // todo: use redis caching
        internal static readonly Dictionary<string, string> _typeRegistrations;
        private Timer _timer;

        static MetadataService()
        {
            _typeRegistrations = new Dictionary<string, string>();
        }

        public MetadataService()
        {
            _timer = new Timer();
            //_callback = OperationContext.Current.GetCallbackChannel<IMetadataCallback>();
        }

        public string SendMeEvery(int seconds)
        {
            //_timer.Stop();
            //_timer = new Timer(seconds);

            //_timer.Elapsed += (sender, args) =>
            //    {
            //        _callback.SendResponse(DateTime.Now.ToLongTimeString());        
            //    };

            return "OK";
        }

        public string GetMetadata(DateTime lastChecked)
        {
            string requestedUrl = OperationContext.Current.IncomingMessageHeaders.To.PathAndQuery;

            KeyValuePair<string, string> one = _typeRegistrations.FirstOrDefault();

            if (one.Equals(default(KeyValuePair<string, string>)))
            {
                return "none";
            }

            return one.Value;
        }

        public string SetMetadata()
        {
            throw new NotImplementedException();
        }

        public MessageMetadata GetTypeInfo(string type)
        {
            //if (WebOperationContext.Current != null)
            //{
            //     var incoming = WebOperationContext.Current.IncomingRequest;
            //    var outgoing = WebOperationContext.Current.OutgoingResponse;

            //    if (string.IsNullOrEmpty(incoming.Accept))
            //    {
            //        // default to json
            //        outgoing.ContentType = "application/json";
            //        outgoing.Format = WebMessageFormat.Json;
            //    }

            //    // default to json
            //    outgoing.ContentType = "application/xml";
            //    outgoing.Format = WebMessageFormat.Xml;
            //}


            //var one = _typeRegistrations.SingleOrDefault(kvp => kvp.Key == type);

            //if (one.Equals(default(KeyValuePair<string, string>)))
            //{
            //    return null;
            //}

            return new MessageMetadata {Author = "Rangeli", Uri = "TESTING"};
        }

        public string RegisterType(string type, string somethingElse)
        {
            string requestedUrl = OperationContext.Current.IncomingMessageHeaders.To.PathAndQuery;

            // what do we need here?
            //Need to know how to call the client back (HTTP [POST], WCF (CallbackChannel))?

            //OperationContext.Current.GetCallbackChannel<>()


            _typeRegistrations.Add(type, somethingElse);

            return "ok";
        }
    }
}