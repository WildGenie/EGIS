using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin.Host.HttpListener;
using Microsoft.Owin.Hosting;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Owin;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Server.Core.Transporters.Http
{
    //[System.Runtime.InteropServices.GuidAttribute("FB10ABE0-522E-408E-AA3C-8649EAD97032")]
    internal class HttpTransporter : Transporter<HttpSubscriber>
    {
        private const string HANDLER_ID = "982edy1bxqqq.http";
        private static HttpTransporter _instance;
        private AutoResetEvent _autoReset;
        private IHubContext _hubContext;
        //private static IHubContext _staticContext;

        public HttpTransporter(ITransportManager transportManager, Configuration configuration) : base(transportManager, configuration)
        {
            if (_instance == null)
                _instance = this;
        }

        internal static HttpTransporter Instance
        {
            get { return _instance; }
        }

        public override string Name
        {
            get { return Names.HttpTransporter; }
        }

        public override void Initialize()
        {
            _autoReset = new AutoResetEvent(false);

            var waitForStart = new AutoResetEvent(false);

            var appTask = new Task(() =>
                {
                    try
                    {
                        string url = string.Format("http://{0}:{1}", Host, Port);

                        using (WebApp.Start<Startup>(url))
                        {
                            var serializerSettings = new JsonSerializerSettings
                            {
                                ContractResolver = new SignalRContractResolver()
                            };

                            var serializer  = new JsonNetSerializer(serializerSettings);
                            GlobalHost.DependencyResolver.Register(typeof(IJsonSerializer), () => serializer);
                            
                            // newer way for signalr 2.0
                            //var serializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
                            //GlobalHost.DependencyResolver.Register(typeof(Newtonsoft.Json.JsonSerializer), () => serializer); 

                            _hubContext = GlobalHost.ConnectionManager.GetHubContext<EsbHub>();

                            Console.WriteLine("Server running on {0}", url);
                            waitForStart.Set();
                            _autoReset.WaitOne();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw ex;
                    }
                });

            appTask.Start();

            waitForStart.WaitOne();

            //_hubContext = GlobalHost.ConnectionManager.GetHubContext<EsbHub>();

            //if (_staticContext == null)
            //    _staticContext = GlobalHost.ConnectionManager.GetHubContext<EsbHub>();
        }

        public override void RePublish(string channel, EsbMessage message)
        {
            if (message.HandlerId == HANDLER_ID) // skip if this was original handler
                return;

            // find all subs
            base.Configuration.DefaultLogger.Log("Inside Republish from " + Name);

            PrivatePublish(message);
        }

        //Note: To receive a replayed message, client should subscribe with the below string prefixed to the original channel
        public override void RePlay(string channel, EsbMessage message)
        {
            //if (message.HandlerId == HANDLER_ID) // skip if this was original handler
            //    return;

            // find all subs
            base.Configuration.DefaultLogger.Log("Inside RePlay from " + Name);

            var newEsbMessage = new EsbMessage
            {
                Id = Guid.NewGuid().ToString(),
                Uri = Names.REPLAY_CHANNEL_PREFIX + message.Uri,
                Type = message.Type,
                PublishedDateTime = message.PublishedDateTime,
                Publisher = message.Publisher,
                Payload = message.Payload
            };

            PrivatePublish(newEsbMessage);
        }


        public override bool Shutdown()
        {
            base.Shutdown();  // handles clearing out subscribers

            _hubContext.Clients.All.forceClose();  // call this on all clients
            _hubContext = null;
            _instance = null; // reset static instance!

            _autoReset.Set();
            return true;
        }

        public override void Configure(ITransportConfig transportConfig)
        {
            var config = transportConfig as HttpConfig;
            Host = config.Host;
            Port = config.Port;
        }

        
        private static readonly object _lock = new object();
        
        /// <summary>
        ///     Called from the hub
        /// </summary>
        /// <param name="subscriber"></param>
        internal void Subscribe(HttpSubscriber subscriber)
        {
            lock (_lock)
            {
                var alreadyListening = base.TransportManager.CurrentSubscribers.Any(s => s.GetType() == typeof (HttpSubscriber)
                                                                                         && s.Uri == subscriber.Uri 
                                                                                         && s.ClientId == subscriber.ClientId 
                                                                                         && s.MachineName == subscriber.MachineName);
                if (!alreadyListening)
                {
                    //add subscriber to the manager
                    base.TransportManager.AddSubscriber(this, subscriber);
                }
            }

        }

        /// <summary>
        ///     Called from the hub
        /// </summary>
        /// <param name="subscriber"></param>
        internal void UnSubscribe(HttpSubscriber subscriber)
        {
            base.TransportManager.RemoveSubscriber(this, subscriber);
        }

        /// <summary>
        ///     Called from the hub
        /// </summary>
        /// <param name="esbMessage"></param>
        internal void Publish(EsbMessage esbMessage)
        {
            // find all subs
            //_configuration.DefaultLogger.Log("Inside Republish from " + this.Name);

            // add handler id to message
            esbMessage.HandlerId = HANDLER_ID;

            PrivatePublish(esbMessage);

            //2.) tell others
            base.TransportManager.RePublish(esbMessage.Uri, esbMessage); // republishes message to other transporters

            //// add message to the manager, rxs, 04/12/2013
            base.TransportManager.AddMessage(esbMessage.Uri, esbMessage);

            ////// add publisher to the manager, rxs, 04/18/2013
            base.TransportManager.AddPublisher(esbMessage.Publisher);
        }

        private static object _clientLock = new object();
        /// <summary>
        /// Called from the hub
        /// </summary>
        internal void ClientShutdown(string clientId)
        {
            var foundSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof (HttpSubscriber) && sub.ClientId == clientId).ToList();

            foundSubs.ForEach(sub =>
                {
                    base.TransportManager.CurrentSubscribers.Remove(sub);
                });


            // remove client
            lock (_clientLock)
            {
                var client = base.TransportManager.CurrentClients.SingleOrDefault(c => c.Id == clientId);
                base.TransportManager.CurrentClients.Remove(client);
            }
        }

        /// <summary>
        /// Called from the hub
        /// </summary>
        internal void UpdatePulse(string clientId, DateTime time)
        {
            base.Pulses.AddOrUpdate(clientId, time, (key, oldTime) => time);
        }

        private void PrivatePublish(EsbMessage esbMessage)
        {
            var foundSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof (HttpSubscriber) && sub.Uri == esbMessage.Uri).ToList();

            string serializedPayload = Encoding.UTF8.GetString(esbMessage.Payload).TrimEnd('\0');

            // we need to serialize the payload for http, so we create a new message
            var httpEsbMessage = new EsbHttpMessage
                {
                    Uri = esbMessage.Uri,
                    Id = esbMessage.Id,
                    ClientId = esbMessage.ClientId,
                    HandlerId = esbMessage.HandlerId,
                    Payload = serializedPayload,
                    Priority = esbMessage.Priority,
                    PublishedDateTime = esbMessage.PublishedDateTime,
                    Publisher = new HttpPublisher() {MachineName = esbMessage.Publisher.MachineName},
                    ReplyTo = esbMessage.ReplyTo,
                    RetryAttempts = esbMessage.RetryAttempts,
                    Type = esbMessage.Type
                };

            foundSubs.ForEach(sub =>
                {
                    try
                    {
                        var httpSubscriber = sub as HttpSubscriber;

                        if (httpSubscriber != null)
                        {
                            // This is kinda hacky.  We need to send the http message to the jsclients only, the other http subs are from the dll client, but using the http transport.  They want the EsbMessage.
                            // TODO: We need to fix this.  
                            if (httpSubscriber.ClientId.EndsWith("JSCLIENT"))
                            {
                                //httpSubscriber.Caller.pushMessage(httpEsbMessage);
                                _hubContext.Clients.Client(httpSubscriber.ConnectionId).pushMessage(httpEsbMessage);
                            }
                            else
                            {
                                //httpSubscriber.Caller.pushMessage(esbMessage);
                                _hubContext.Clients.Client(httpSubscriber.ConnectionId).pushMessage(esbMessage);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        base.Configuration.DefaultLogger.LogError(ex.Dump());
                        //base.Configuration.DefaultLogger.LogErrorStackTrace();
                    }

                });
        }

        public void Announce(ClientAnnounce client)
        {
            // add client to list?
            // once announced and active, they can start subscribing and publishing

            
            base.TransportManager.AddClient(new Client()
                {
                    Id = client.Id,
                    Name = client.Name,
                    Description = client.Description,
                    LastPulse = DateTime.UtcNow
                });

        }

        internal bool HasClient(string clientId)
        {
            return base.TransportManager.CurrentClients.Any(c => c.Id == clientId);
        }
    }

    public class HttpConfig : ITransportConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public class HttpSubscriber : ISubscriber
    {
        public string ConnectionId { get; set; } // for our hub connection
        public dynamic Caller { get; set; }
        public string Uri { get; set; }
        public string MachineName { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string ClientId { get; set; }
    }

    /// <summary>
    ///     Only for output to Js clients
    /// </summary>
    public sealed class EsbHttpMessage
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string Uri { get; set; }
        public string Type { get; set; }
        public string ReplyTo { get; set; }
        public int? Priority { get; set; }
        public int? RetryAttempts { get; set; }
        public DateTime PublishedDateTime { get; set; }
        public HttpPublisher Publisher { get; set; }
        public string Payload { get; set; }
        public string HandlerId { get; set; }
    }

    public sealed class HttpPublisher : IPublisher
    {
        public string MachineName { get; set; }
    }

    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var listener = (OwinHttpListener)app.Properties[typeof(OwinHttpListener).FullName];
            listener.SetRequestProcessingLimits(500, 1000);
         
            // we had to new up the DependencyResolver on the GlobalHost here because on restarts it was not working correctly! - RANGELI
            GlobalHost.DependencyResolver = new DefaultDependencyResolver();

            // Turn cross domain on 
            var config = new HubConfiguration {EnableCrossDomain = true};

            app.MapHubs("esbdyn", config);
            app.UseNancy(new CustomBootstrapper());
        }

        //public static Bus BusInstance { get; private set; }
    }

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/files", @"/Transporters/Http"));
            conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/files/scripts", @"/Transporters/Http/Scripts"));
        }

        protected override IEnumerable<ModuleRegistration> Modules
        {
            get
            {
                return new ModuleRegistration[0];// {new ModuleRegistration(typeof (HttpModule))}; 
            }
        }
    }

    //public class HttpModule : NancyModule
    //{
    //    public HttpModule()
    //    {
    //        Post["/waitForShutdown"] = o =>
    //            {
    //                var client = this.Bind<Client>();
    //                var totalWait = 0;
    //                while (HttpTransporter.Instance.HasClient(client.Id) && totalWait <= 5000)
    //                {
    //                    // keep checking until we don't have it any longer, or 5 seconds elapses
    //                    Thread.Sleep(1000);
    //                    totalWait += 1000;
    //                }

    //                return Response.AsJson(new {status = "OK"}, HttpStatusCode.OK);
    //            };
    //    }
    //}

    public class EsbHub : Hub
    {
        private readonly HttpTransporter _transporter;

        public EsbHub() : this(HttpTransporter.Instance)
        {
        }

        private EsbHub(HttpTransporter transporter)
        {
            _transporter = transporter;

            
            //_transporter.Set
        }

        public string Subscribe(Subscriber subscriber)
        {
          
           
            var httpSubscriber = new HttpSubscriber
                {
                    Uri = subscriber.Uri,
                    ConnectionId = Context.ConnectionId,
                    Caller =  base.Clients.Caller,
                    ClientId = subscriber.ClientId,
                    MachineName = subscriber.MachineName
                };

            _transporter.Subscribe(httpSubscriber);

            return "ok";
        }

        public void UnSubscribe(Subscriber subscriber)
        {
            var httpSubscriber = new HttpSubscriber
                {
                    Uri = subscriber.Uri,
                    ConnectionId = Context.ConnectionId,
                    MachineName = subscriber.MachineName,
                    ClientId = subscriber.ClientId,
                    LastHeartbeat = DateTime.Today
                };

            _transporter.UnSubscribe(httpSubscriber);
        }

        public void Publish(string uri, EsbMessage esbMessage)
        {
            //var esbPubMessage = new EsbMessage
            //    {
            //        Uri = uri,
            //        ClientId = esbMessage.ClientId,
            //        Payload = esbMessage.Payload, //message.ToJson()),
            //        PublishedDateTime = DateTime.Now,
            //        Publisher = new Publisher {MachineName = "jsClient"}
            //    };

            esbMessage.Uri = esbMessage.Uri ?? uri;
            esbMessage.Publisher = esbMessage.Publisher ?? new Publisher() {MachineName = "<unknown>"};
            esbMessage.PublishedDateTime = DateTime.Now;

            _transporter.Publish(esbMessage);
        }

        public void ClientShutdown(string clientId)
        {
            _transporter.ClientShutdown(clientId);
        }

        /// <summary>
        /// Called from the client every x minutes as a pulse to know they are still there...  js close events are hard to detect.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="time"></param>
        public void Pulse(string clientId, DateTime time)
        {
            _transporter.UpdatePulse(clientId, DateTime.UtcNow);
        }


        /// <summary>
        /// Called from the client to announce themselves.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="time"></param>
        public void Announce(ClientAnnounce client)
        {
            _transporter.Announce(client);
        }
    }
}