using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Services;
using GeoDecisions.Esb.Common.Utility;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Services;
using RestSharp;
using RestSharp.Serializers;
using ServiceStack.Common.Extensions;

namespace GeoDecisions.Esb.Server.Core.Transporters
{
    /// <summary>
    ///     This transporter is responsible for handling REST based request for POSTing messages to subscribers.
    /// </summary>
    internal class WcfRestTransporter : IServerTransporter, IBusRestService
    {
        private const string HANDLER_ID = "u45234v47rest";
        private static SynchronizedCollection<WcfRestSubscriber> _wcfRestSubscribers;
        private readonly Configuration _configuration;
        private readonly ITransportManager _transportManager;
        private RestClient _restClient;
        private IEnumerable<ServiceHost> _restHosts;


        //internal WcfRestTransporter()
        //{
        //}

        private WcfRestTransporter(ITransportManager manager, Configuration config)
        {
            _wcfRestSubscribers = new SynchronizedCollection<WcfRestSubscriber>();
            _transportManager = manager;
            _configuration = config;
        }

        /// <summary>
        ///     Called when the rest service has a message published to it
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string Publish(Stream data)
        {
            //1.) decode data -> message
            var esbMessage = _configuration.Serialization.Deserialize<EsbMessage>(data);

            //2.) tell others
            if (BusContext.Current != null)
                BusContext.Current.TransportManager.RePublish(esbMessage.Uri, esbMessage); // republishes message to other transporters
            else
                _transportManager.RePublish(esbMessage.Uri, esbMessage);

            //3.)
            // we handle publishing to our subs

            //_transportManager.CurrentSubscribers ?? maybe we can use this
            _wcfRestSubscribers.ForEach(sub =>
                {
                    var request = new RestRequest(sub.CallbackUri, Method.POST);
                    request.JsonSerializer = new JsonSerializer();
                    request.AddParameter("message", esbMessage);
                    //var response = _restClient.Execute(request);
                    _restClient.ExecuteAsyncPost(request, (restResponse, handle) =>
                        {
                            _configuration.DefaultLogger.Log(restResponse.ResponseStatus.ToString());

                            //restSub.AsyncHandler();
                        }, "POST");
                });

            return "ok";
        }

        public string Subscribe(string uri)
        {
            //http://pc65905:9000/esb/subscribe?uri=/messages/tea/layerPublish&replyFormat=json&callback=http://pc65905/test/rest/receiver
            // this handles subscriptions from http

            // we need to add this to a mapping collection
            string requestedUrl = OperationContext.Current.IncomingMessageHeaders.To.PathAndQuery;

            NameValueCollection qsInfo = UriExtensions.GetQueryString(requestedUrl);

            // store callback url along with this subscription
            string callback = qsInfo["callback"];

            //_messageMappings.Add(new MessageMapping() { Uri = uri, CallbackUri = callback });
            //string msgDest = @"/messages/{destination}/msgType";

            // can't reference this directly :(
            //_transportManager.AddSubscriber();

            var subscriber = new WcfRestSubscriber
                {
                    Uri = uri,
                    CallbackUri = callback
                };

            _wcfRestSubscribers.Add(subscriber);

            BusContext.Current.TransportManager.AddSubscriber(this, subscriber);

            return "";
        }

        public string Name
        {
            get { return Names.WcfHttpRestTransporter; }
        }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public IServerTransporter CreateTransporter(ITransportManager manager, Configuration config)
        {
            return new WcfRestTransporter(manager, config);
        }

        public void Initialize()
        {
            _restClient = new RestClient();
            _restHosts = StartRESTHost();
        }

        //public Func<string, string> Selector { get; set; }

        //public Func<string, string> MessageReceived { get; set; }

        //public Func<string, string> RegisteredSubscriber { get; set; }


        /// <summary>
        ///     Cross-Transporter message push implementation
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public void RePublish(string channel, EsbMessage message)
        {
            IEnumerable<WcfRestSubscriber> subscribed = _wcfRestSubscribers.Where(sub => sub.Uri == channel);

            //var subscribed = _transportManager.CurrentSubscribers.Where(s => s.Uri == channel).ToList();
            //BusContext.Current.TransportManager.CurrentSubscribers.Where(s => s.Uri == channel).ToList();

            subscribed.ForEach(sub =>
                {
                    try
                    {
                        WcfRestSubscriber restSub = sub;

                        var request = new RestRequest(restSub.CallbackUri, Method.POST);
                        request.AddParameter("message", message);
                        IRestResponse response = _restClient.Execute(request);

                        _restClient.ExecuteAsyncPost(request, (restResponse, handle) =>
                            {
                                _configuration.DefaultLogger.Log(restResponse.ResponseStatus.ToString());

                                //restSub.AsyncHandler();
                            }, "POST");
                    }
                    catch (Exception ex)
                    {
                        _configuration.DefaultLogger.Log(ex.Message);
                    }
                });
        }

        public bool Shutdown()
        {
            _restHosts.ToList().ForEach(host => host.Close());

            _restClient = null;

            return true;
        }

        /// <summary>
        ///     Can be called from other transporters.  Check to see if we need to do anything for this subscriber
        /// </summary>
        /// <param name="subscriber"></param>
        public void HandleSubscribe(IServerTransporter srcTransporter, ISubscriber subscriber)
        {
        }

        public void HandleUnSubscribe(IServerTransporter srcTransporter, ISubscriber subscriber)
        {
        }

        public bool InterestedIn(string msgUri)
        {
            return _wcfRestSubscribers.Any(sub => sub.Uri == msgUri);
        }

        public bool Enabled { get; set; }


        public void Configure(ITransportConfig transportConfig)
        {
            var redisConfig = transportConfig as RedisConfig;

            if (redisConfig == null)
                throw new Exception("Redis configuration was invalid.");

            // do stuff with it here...
        }

        public void RePlay(string channel, EsbMessage message)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<ServiceHost> StartRESTHost()
        {
            var baseAddresses = new[] {new Uri("http://localhost:9000/esb")};

            //var webHost = new WebServiceHost(typeof(MetadataService), baseAddresses);

            //var webHostMetadatabehavior = new ServiceMetadataBehavior
            //{
            //    HttpGetEnabled = false,
            //    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            //};

            //webHost.Description.Behaviors.Add(webHostMetadatabehavior);

            //var restBinding = new WebHttpBinding(WebHttpSecurityMode.None);

            //ServiceEndpoint endpoint1 = webHost.AddServiceEndpoint(typeof(IMetadataService), restBinding, _configuration.MetadataEndpoint);
            //endpoint1.Behaviors.Add(new WebHttpBehavior { AutomaticFormatSelectionEnabled = true });

            //webHost.Description.Behaviors.Add(new BusServiceContextBehavior());

            //webHost.Open();

            //foreach (ServiceEndpoint addr in webHost.Description.Endpoints)
            //{
            //    Console.WriteLine("The service is ready at {0}", addr.Address);
            //}

            var webBusHost = new WebServiceHost(typeof (WcfRestTransporter), baseAddresses);

            var webBusMetadata = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = false,
                    MetadataExporter = {PolicyVersion = PolicyVersion.Policy15},
                };

            webBusHost.Description.Behaviors.Add(webBusMetadata);
            webBusHost.Description.Behaviors.Add(new BusServiceContextBehavior(_transportManager));

            var busRestBind = new WebHttpBinding(WebHttpSecurityMode.None);

            //var busEndpoint = webBusHost.AddServiceEndpoint(typeof (IBusService), busRestBind, "bus");
            //busEndpoint.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

            ServiceEndpoint busEndpoint2 = webBusHost.AddServiceEndpoint(typeof (IBusRestService), busRestBind, "rest");
            busEndpoint2.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

            webBusHost.Open();

            foreach (ServiceEndpoint addr in webBusHost.Description.Endpoints)
            {
                Console.WriteLine("The service is ready at {0}", addr.Address);
            }

            //ServiceAuthenticationBehavior sab = null;
            //sab = webHost.Description.Behaviors.Find<ServiceAuthenticationBehavior>();

            //if (sab == null)
            //{
            //    sab = new ServiceAuthenticationBehavior();
            //    sab.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Negotiate | AuthenticationSchemes.Digest | AuthenticationSchemes.Anonymous;
            //    webHost.Description.Behaviors.Add(sab);
            //}
            //else
            //{
            //    sab.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Negotiate | AuthenticationSchemes.Digest;
            //}

            //return new[] { webHost, webBusHost };
            return new[] {webBusHost};
        }

        public void HandleClientShutdown(IServerTransporter srcTransporter, string clientId)
        {
            //throw new NotImplementedException();
        }
    }

    //public class RestSubscriber : ISubscriber
    //{
    //    public string Uri { get; set; }
    //    public string CallbackUri { get; set; }
    //}

    public class WcfRestSubscriber : ISubscriber
    {
        public string CallbackUri { get; set; }
        public string Uri { get; set; }
        //public MapDestination Destination { get; set; }
        public string MachineName { get; set; }
        public string ClientId { get; set; }


        public DateTime LastHeartbeat { get; set; }
    }
}