using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Services;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;
using Ninject.Extensions.Wcf;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Server.Core.Transporters
{
    public class WcfTcpConfig : ITransportConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool Enabled { get; set; }
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    internal class WcfTcpTransporter : Transporter<WcfTcpSubscriber>, IBusService //IServerTransporter, IBusService
    {
        private const string HANDLER_ID = "34q12w3fwcftcp";
        private IEnumerable<ServiceHost> _tcpHosts;

        public WcfTcpTransporter(ITransportManager manager, Configuration config) : base(manager, config)
        {
        }

        public override string Name
        {
            get { return Names.WcfTcpTransporter; }
        }

        /// <summary>
        ///     This is the method that gets called from our client (using WCF TCP).
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void Publish(EsbMessage message)
        {
            try
            {
                message.HandlerId = HANDLER_ID;

                base.Configuration.DefaultLogger.Log("Inside Publish from " + Name + " for message id:" + message.Id + " , payload:" + message.Payload);

                // find all subs
                var foundSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.Uri == message.Uri && sub.GetType() == typeof (WcfTcpSubscriber)).Select(sub => (WcfTcpSubscriber) sub);

                var task = new Task(() =>
                    {
                        foundSubs.ForEach(mm =>
                            {
                                if (mm.Callback != null)
                                {
                                    // if we have a callback stored for this, call it.
                                    mm.Callback.Receiver(message);
                                }
                            });
                    });

                task.Start();

                base.TransportManager.RePublish(message.Uri, message);
                base.TransportManager.AddMessage(message.Uri, message);
                base.TransportManager.AddPublisher(message.Publisher);
            }
            catch (Exception ex)
            {
                base.Configuration.DefaultLogger.LogError(ex.Dump());
            }
        }


        public List<T> GetCustomMessages<T>(string uri, DateTime dateTime)
        {
            var messages = base.TransportManager.CurrentMessages.Where(
                                           msg =>
                                           DateTime.Compare(msg.PublishedDateTime, dateTime) > 0 &&
                                           msg.Uri == uri &&
                                           msg.GetType() == typeof(T)
                                           );

            return EnumerableExtensions.ToList<T>(messages);
        }


        public List<EsbMessage> GetMessages(string uri, DateTime dateTime)
        {
            var messages = base.TransportManager.CurrentMessages.Select(msg => new EsbMessage()
            {
                Id = msg.Id,
                Uri = msg.Uri,
                Type = msg.Type,
                Publisher = msg.Publisher,
                PublishedDateTime = msg.PublishedDateTime,
                ClientId = msg.ClientId,
                Payload = msg.Payload
            })
                                       .Where(
                                           msg =>
                                           DateTime.Compare(msg.PublishedDateTime, dateTime) > 0 &&
                                           msg.Uri == uri
                                           );

            return messages.ToList();
        }


        /// <summary>
        ///     Handles the custom subscription info
        ///     When this gets called, it is executed on another thread and we must access any bus information from the Context.
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        public Subscription Subscribe(Subscribe subscribe)
        {
            try
            {
                //string requestedUrl = OperationContext.Current.IncomingMessageHeaders.To.PathAndQuery;
                //string msgDest = @"/messages/{destination}/msgType";

                // duplex communication?
                var cbChannel = OperationContext.Current.GetCallbackChannel<IBusServiceCallback>();

                // maybe check if we have a call back channel already...
                bool subscribed = base.TransportManager.CurrentSubscribers.Any(sub => sub.GetType() == typeof (WcfTcpSubscriber) && sub.Uri == subscribe.Uri && sub.ClientId == subscribe.ClientId);

                if (subscribed)
                    return new Subscription {Status = "duplicate"};

                //((ICommunicationObject) cbChannel).Faulted += (sender, args) =>
                //    {
                      
                //        var subs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof (WcfTcpSubscriber)).Select(sub => (WcfTcpSubscriber) sub).ToList();
                //        var found = subs.Where(tcpSub => tcpSub.Callback == sender).ToList();

                //        found.ForEach(tcpSub =>
                //        {
                //            base.TransportManager.RemoveSubscriber(this, tcpSub);
                //        });


                //        var client = TransportManager.CurrentClients.Where(c => c.GetType() == typeof (TcpClient)).FirstOrDefault(c2 => ((TcpClient) c2).Callback == sender);
                //        base.EvictClient(client: client);
    
                //        base.Configuration.DefaultLogger.Log("CLIENT DISCONNECTED [Faulted].");
                //    };

                //((ICommunicationObject)cbChannel).Closing += (sender, args) =>
                //{
                //    base.Configuration.DefaultLogger.Log("CLIENT DISCONNECTED [Closed].");
                //};

                var tcpSubscriber = new WcfTcpSubscriber {Uri = subscribe.Uri, Callback = cbChannel, MachineName = subscribe.MachineName, ClientId = subscribe.ClientId};

                base.TransportManager.AddSubscriber(this, tcpSubscriber);

                return new Subscription {Status = "ok"};
            }
            catch (Exception ex) // catch all...  
            {
                base.Configuration.DefaultLogger.LogError(ex.Dump());
            }

            return new Subscription {Status = "error"};
        }

        /// <summary>
        ///     When this gets called, it is executed on another thread and we must access any bus information from the Context.
        /// </summary>
        /// <param name="msgId"></param>
        /// <returns></returns>
        public string UnSubscribe(UnSubscribe unSubscribe)
        {
            var foundSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.Uri == unSubscribe.Uri && sub.GetType() == typeof (WcfTcpSubscriber) && sub.ClientId == unSubscribe.ClientId).ToList();

            foundSubs.ForEach(sub =>
                {
                    base.TransportManager.RemoveSubscriber(this, sub);
                });

            return "ok";
        }

        public override void Initialize()
        {
            _tcpHosts = StartTcpHost();
        }

        private IEnumerable<ServiceHost> StartTcpHost()
        {
            string netTcpScheme = "net.tcp";
            var tcpBaseUri = new Uri(string.Format("{0}://{1}:{2}/esb", netTcpScheme, Environment.MachineName, Port));

            var busHost = new NinjectServiceHost(new NinjectServiceBehavior(type => { return new NinjectInstanceProvider(typeof (WcfTcpTransporter), Bus.BusKernel); }, new WcfRequestScopeCleanup(true)), typeof (WcfTcpTransporter), tcpBaseUri);

            // var busHost = new ServiceHost(typeof(WcfTcpTransporter), tcpBaseUri);
            // busHost.Description.Behaviors.Add(new BusServiceContextBehavior()); // add our context behavior

            var debug = busHost.Description.Behaviors.Find<ServiceDebugBehavior>();

            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                busHost.Description.Behaviors.Add(new ServiceDebugBehavior {IncludeExceptionDetailInFaults = true});
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }

            //busHost.Description.Behaviors.Add(new BusServiceContextBehavior(_transportManager));

            var tcpBinding = new NetTcpBinding(SecurityMode.None)
                {
                    SendTimeout = TimeSpan.FromHours(24),
                    ReceiveTimeout = TimeSpan.FromHours(24),
                    MaxReceivedMessageSize = int.MaxValue,
                    MaxBufferSize = int.MaxValue,
                    MaxBufferPoolSize = long.MaxValue//,
                    //PortSharingEnabled = true
                };

            busHost.AddServiceEndpoint(typeof (IBusService), tcpBinding, "bus");

            //foreach (ServiceEndpoint endpoint in busHost.Description.Endpoints)
            //{
            //    foreach (OperationDescription operation in endpoint.Contract.Operations)
            //    {
            //        var dcsob = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();

            //        if (dcsob != null)
            //        {
            //            dcsob.DataContractResolver = new EsbSerializationResolver();
            //            //dcsob.DataContractResolver = new MyResolver();
            //        }
            //    }
            //}

            // add custom inspectors... etc is possible
            //foreach (ChannelDispatcher chDisp in host.ChannelDispatchers)
            //{
            //    foreach (EndpointDispatcher epDisp in chDisp.Endpoints)
            //    {
            //        epDisp.DispatchRuntime.MessageInspectors.Add(new Inspector());
            //        foreach (DispatchOperation op in epDisp.DispatchRuntime.Operations)
            //            op.ParameterInspectors.Add(new Inspector());
            //    }
            //}

            // Open the ServiceHost to start listening for messages. Since
            // no endpoints are explicitly configured, the runtime will create
            // one endpoint per base address for each service contract implemented
            // by the service.

            busHost.Open();

            foreach (ServiceEndpoint addr in busHost.Description.Endpoints)
            {
                Console.WriteLine("The service is ready at {0}", addr.Address);
            }

            // subscription host
            return new[] {busHost};
        }

        private void SetupSecurity()
        {
            var serviceCredentials = new ServiceCredentials();

            // Configure service certificate
            serviceCredentials.ServiceCertificate.SetCertificate(
                StoreLocation.LocalMachine,
                StoreName.My,
                X509FindType.FindBySubjectName,
                "ServerCertificate");

            // Configure client certificate authentication mode
            //serviceCredentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;

            // Add custom username-password validator
            serviceCredentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
            //serviceCredentials.UserNameAuthentication.CustomUserNamePasswordValidator = _container.Resolve<MyServicesUsernameValidator>();
        }

        /// <summary>
        ///     Handle other transporter republish
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public override void RePublish(string channel, EsbMessage message)
        {
            if (message.HandlerId == HANDLER_ID) // skip if this was original handler
                return;

            // find all subs
            base.Configuration.DefaultLogger.Log("Inside Republish from " + Name);

            IEnumerable<WcfTcpSubscriber> foundSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.Uri == channel && sub.GetType() == typeof (WcfTcpSubscriber)).Select(sub => (WcfTcpSubscriber) sub);

            foundSubs.ForEach(sub => { sub.Callback.Receiver(message); });
        }

        //Note: To receive a replayed message, client should subscribe with the below string prefixed to the original channel
        public override void RePlay(string channel, EsbMessage message)
        {
            //if (message.HandlerId == HANDLER_ID) // skip if this was original handler
            //    return;

            // find all subs
            base.Configuration.DefaultLogger.Log("Inside Replay from " + Name);

            IEnumerable<WcfTcpSubscriber> foundSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.Uri == Names.REPLAY_CHANNEL_PREFIX + channel && sub.GetType() == typeof(WcfTcpSubscriber)).Select(sub => (WcfTcpSubscriber)sub);

            var newEsbMessage = new EsbMessage
            {
                Id = Guid.NewGuid().ToString(),
                Uri = Names.REPLAY_CHANNEL_PREFIX + message.Uri,
                Type = message.Type,
                PublishedDateTime = message.PublishedDateTime,
                Publisher = message.Publisher,
                Payload = message.Payload
            };

            foundSubs.ForEach(
                sub => { sub.Callback.Receiver(newEsbMessage); }
                );
        }

        public override bool Shutdown()
        {
            base.Shutdown();  // handles clearing out subscribers

            _tcpHosts.ToList().ForEach(sh => sh.Close()); // close hosts

            return true;
        }

        public override void Configure(ITransportConfig transportConfig)
        {
            var wcfTcpConfig = transportConfig as WcfTcpConfig;

            if (wcfTcpConfig == null)
                throw new Exception("WcfTcp configuration was invalid.");

            Host = wcfTcpConfig.Host;
            Port = wcfTcpConfig.Port;
        }

        /// <summary>
        /// Called from the TCP client
        /// </summary>
        /// <param name="unSubscribe"></param>
        /// <returns></returns>
        public void ClientShutdown(UnSubscribe unSubscribe)
        {
            // only by client id
            var allClientSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof (WcfTcpSubscriber) && sub.ClientId == unSubscribe.ClientId).Select(sub => (WcfTcpSubscriber) sub).ToList();

            allClientSubs.ForEach(sub =>
                {
                    //((ICommunicationObject)sub.Callback).Close(); // maybe close on the client end
                    base.TransportManager.RemoveSubscriber(this, sub);
                });


            // find client(s) by subscribers since they hold the Callback that we have to compare....
            var clients = base.TransportManager.CurrentClients.Where(c => c.Id == unSubscribe.ClientId).ToList();

            clients.ForEach(client =>
            {
                base.EvictClient(client: client);
            });

        }

        public void Pulse(Pulse pulse)
        {
            var now = DateTime.UtcNow;
            base.Pulses.AddOrUpdate(pulse.ClientId, now, (s, time) => now);
        }

        public void Announce(ClientAnnounce client)
        {
            var cbChannel = OperationContext.Current.GetCallbackChannel<IBusServiceCallback>();
       
            ((ICommunicationObject)cbChannel).Faulted += (sender, args) =>
            {

                var subs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof(WcfTcpSubscriber)).Select(sub => (WcfTcpSubscriber)sub).ToList();
                var found = subs.Where(tcpSub => tcpSub.Callback == sender).ToList();

                found.ForEach(tcpSub =>
                {
                    base.TransportManager.RemoveSubscriber(this, tcpSub);
                });


                var evictedClient = TransportManager.CurrentClients.Where(c => c.GetType() == typeof(TcpClient)).FirstOrDefault(c2 => ((TcpClient)c2).Callback == sender);
                base.EvictClient(client: evictedClient);

                base.Configuration.DefaultLogger.Log("CLIENT DISCONNECTED [Faulted].");
            };

            ((ICommunicationObject)cbChannel).Closing += (sender, args) =>
            {
                base.Configuration.DefaultLogger.Log("CLIENT DISCONNECTED [Closed].");
            };


            base.TransportManager.AddClient(new TcpClient() {Id = client.Id, Name = client.Name, Description = client.Description, LastPulse = client.LastPulse, Callback = cbChannel});
        }
    }

    public class WcfTcpSubscriber : ISubscriber
    {
        public IBusServiceCallback Callback { get; set; }
        public string Uri { get; set; }
        public string MachineName { get; set; }
        public string ClientId { get; set; }

        public DateTime LastHeartbeat { get; set; }
    }

    public class TcpClient : Client
    {
        public IBusServiceCallback Callback;
    }
}