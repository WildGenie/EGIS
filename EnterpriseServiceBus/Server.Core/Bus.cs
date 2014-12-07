using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Threading;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Services;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Common.Utility.Plugins;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;
using GeoDecisions.Esb.Server.Core.Services;
using GeoDecisions.Esb.Server.Core.Utility;
using Ninject;
using Ninject.Extensions.Wcf;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;
using System.Timers;

namespace GeoDecisions.Esb.Server.Core
{
    public class Bus
    {
        //poc

        internal static IKernel BusKernel;
        private readonly Configuration _config;
        
        private ServiceHost _configBusHost;
        private WebServiceHost _configWebBusHost;

        private ServiceHost _replayMessageBusHost;


        private readonly ILogger _logger;
        private readonly List<IServerTransporter> _serverTransporters;
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly List<ITransportManager> _transportManagers;
        public int RetryCount = -1;
        private DateTime _startDateTime;
        private System.Timers.Timer _attemptMessageFlushTimer;
        private IEnumerable<ServiceHost> _tcpMessageReplayHosts;

        public Bus(Configuration configuration)
        {
            _config = configuration;

            _logger = _config.DefaultLogger;

            _transportManagers = new List<ITransportManager>();
            _serverTransporters = new List<IServerTransporter>();

            MessageMetadata = new SynchronizedCollection<MessageMetadata>();
        }

        internal static SynchronizedCollection<MessageMetadata> MessageMetadata { get; set; }

        //Added this method so that the Host from Server.Host will have access to replay a message
        public void RePlay(EsbMessage message)
        {
            _transportManagers.FirstOrDefault().RePlay(message.Uri, message);
        }

        //events
        //esb.OnMessage
        public Action<string> OnMessage { get; set; }

        //esb.OnError
        public Action<string> OnError { get; set; }

        //esb.OnSubscribe
        public Action<string> OnSubscribe { get; set; }

        //esb.OnUnsubscribe
        public Action<string> OnUnsubscribe { get; set; }

        public int SubscriberCount
        {
            get { return _transportManagers.Sum(mgr => mgr.CurrentSubscribers.Count); }
        }

        public int PublisherCount
        {
            get { return _transportManagers.Sum(mgr => mgr.CurrentPublishers.Count); }
        }

        public DateTime StartDateTime
        {
            get { return _startDateTime; }
        }

        public List<ISubscriber> Subscribers
        {
            get
            {
                var subs = new List<ISubscriber>();
                foreach (ITransportManager mgr in _transportManagers)
                {
                    if (mgr.CurrentSubscribers != null) subs.AddRange(mgr.CurrentSubscribers);
                }
                return subs;
            }
        }

        public List<IPublisher> Publishers
        {
            get
            {
                var pubs = new List<IPublisher>();
                foreach (ITransportManager mgr in _transportManagers)
                {
                    if (mgr.CurrentMessages != null) pubs.AddRange(mgr.CurrentPublishers);
                }
                return pubs;
            }
        }

        public List<EsbMessage> Messages
        {
            get
            {
                var msgs = new List<EsbMessage>();
                foreach (ITransportManager mgr in _transportManagers)
                {
                    if (mgr.CurrentMessages != null) msgs.AddRange(mgr.CurrentMessages);
                }
                return msgs;
            }
        }

        public int FlushMessages(int nDays)
        {
            int nPurged = 0;
            foreach (EsbMessage msg in Messages)
            {
                if ((DateTime.Now - msg.PublishedDateTime).TotalDays > nDays)
                {
                    RemoveMessage(msg);
                    nPurged++;
                }
            }

            //Clear from database if needed
            StorageManager.PurgeMessages(nDays);

            return nPurged;
        }

        public string PostSettings(MessageSettings settings)
        {
            //System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //config.AppSettings.Settings["DaysToRetainMessages"].Value = settings.DaysToRetainMessages.ToString();
            //config.AppSettings.Settings.Remove("DaysToRetainMessages");
            //config.AppSettings.Settings.Add("DaysToRetainMessages", settings.DaysToRetainMessages.ToString());
            //config.Save(ConfigurationSaveMode.Modified);
            //ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            //_config.MessageSettings.DaysToRetainMessages = Convert.ToInt32(ConfigurationManager.AppSettings["DaysToRetainMessages"]);

            //This is where the action that was set is getting called
            _config.CallSaveSettingsAction(settings);

            return "success";
        }

        public decimal MessagesBandwidthInMb
        {
            get { return Math.Round((_transportManagers.FirstOrDefault().CurrentMessages.Sum(msg => msg.Payload.LongCount())/1048576m), 5); }
        }

        internal IEnumerable<IServerTransporter> AllTransporters
        {
            get { return _serverTransporters; }
        }

        public List<IServerTransporter> Transporters
        {
            get { return _serverTransporters; }
        }
        
        public List<IStore> Databases
        {
            get { return _config.Databases; }
        }

        public IServerTransporter HttpTransporter
        {
            get { return AllTransporters.FirstOrDefault(t => t.Name == Names.HttpTransporter); }
        }

        public List<Client> Clients
        {
            get { return _transportManagers.FirstOrDefault().CurrentClients.ToList(); }
        }

        public void Start()
        {
            _config.ReInit(); // ensures we have a clean config to work with (mostly for cases of restart).

            RetryCount++;
            _config.DefaultLogger.Log("Starting server");

            _startDateTime = DateTime.Now;

            var transportManager = PluginManager.GetOneOf<ITransportManager>(_config, this);

            BusKernel = new StandardKernel();
            BusKernel.Bind<Configuration>().ToConstant(_config);
            BusKernel.Bind<ITransportManager>().ToConstant(transportManager);
            BusKernel.Bind<ISerialization>().ToConstant(new JsonSerialization());

            //Start config services
            _config.DefaultLogger.Log("Starting Configuration Service");
            StartConfigServices(_config);

            // maybe we start and pass a transportmanager interface to all transporters??
            _config.DefaultLogger.Log("Starting Transporters");

            List<IServerTransporter> transporters = PluginManager.GetAll<IServerTransporter>(transportManager, _config).ToList();

            // add our internal transporters.
            _config.Transporters.AddRange(transporters);

            //StorageManager has been moved to the server.core namespace so that the logger from configuration can be accessed in it
            StorageManager.Init(_config);
            
            _config.Databases = new List<IStore>();
            _config.Databases.AddRange(StorageManager.DBs);
            _config.DefaultLogger.Log("Storage managers initialized");

            _config.ConfigureTransporters();
            _config.DefaultLogger.Log("Transporters configured from config file");

            _config.ConfigureDatabases();
            _config.DefaultLogger.Log("Databases configured from config file");

            //This initialises the session in case of oracle, the other databases just return
            StorageManager.InitSession();

            _config.Transporters.Where(t => t.Enabled).ForEach(transporter =>
                {
                    try
                    {
                        _logger.Log("init() -> " + transporter.Name);

                        //var instance = Bus.BusKernel.Get<IServerTransporter>(metadata => metadata.Name == transporter.Name);
                        IServerTransporter instance = transporter.CreateTransporter(transportManager, _config) ?? transporter;

                        instance.Initialize();

                        _config.DefaultLogger.Log(instance.Name + " is enabled and added to _serverTransporters");

                        _serverTransporters.Add(instance);

                        // usecase:
                        // 1.) we get a message published through redis
                        //      - We need to handle the message (transporter) | redistransporter
                        //      - Determine who gets it and how - (bus) | httptransporter
                        //      - Ask selected transporters to republish message to the subs given to them by the bus (httptransporter)
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(ex.Message);
                    }
                });

            _transportManagers.Add(transportManager);

            //Start timer for purging messages automatically, trigger every 1 hour(s) and purge messages published before [DaysToRetainMessage] days
            StartMessagePurgeTimer(DaysToRetainMessages);

            //Start message retrieval and replay services
            //_config.DefaultLogger.Log("Starting message retrieval and replay services");
            //_tcpMessageReplayHosts = StartMessageServices(_config);


            _config.DefaultLogger.Log("Server Started.");
        }

        private IEnumerable<ServiceHost> StartMessageServices(Configuration configuration)
        {
            var tcpUri = new UriBuilder("net.tcp://localhost/esb/services"); // default
            tcpUri.Port = configuration.ServerPort + 2;
            tcpUri.Host = configuration.ServerHost;

            var tcpBaseAddresses = new[] {tcpUri.Uri};

            configuration.DefaultLogger.Log("Preparing to setup the messaging service");

            _replayMessageBusHost = new NinjectServiceHost(new NinjectServiceBehavior(type => { return new NinjectInstanceProvider(typeof (BusMessageService), Bus.BusKernel); }, new WcfRequestScopeCleanup(true)),
                                                            typeof(BusMessageService), 
                                                            tcpBaseAddresses);

            var tcpBinding = new NetTcpBinding(SecurityMode.None);

            _replayMessageBusHost.AddServiceEndpoint(typeof (IBusMessageService), tcpBinding, "replay");

            _replayMessageBusHost.Open();

            foreach (ServiceEndpoint addr in _replayMessageBusHost.Description.Endpoints)
            {
                Console.WriteLine("The message replay service is ready at {0}", addr.Address);
            }

            return new[] {_replayMessageBusHost};
        }


        private void StartMessagePurgeTimer(int nDays)
        {
            if (_attemptMessageFlushTimer == null)
            {
                _attemptMessageFlushTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
            }

            _attemptMessageFlushTimer.Elapsed += (sender, args) =>
                {
                    _attemptMessageFlushTimer.Stop();
                    _config.DefaultLogger.Log("A message flush was attempted from the automated flush mechanism at:" + DateTime.Now);
                    FlushMessages(nDays);
                    _attemptMessageFlushTimer.Start();
                };

            _attemptMessageFlushTimer.Start();
        }



        /// <summary>
        ///     This starts the server config services to help auto-configure the clients
        /// </summary>
        private void StartConfigServices(Configuration configuration)
        {
            ////string baseServiceUri = "";
            //string host = "localhost";
            //int port = 9911;

            // *********************************** [Start] RESTful HTTP ***********************************************
            var httpUri = new UriBuilder("http://localhost/esb/services"); // default
            httpUri.Port = configuration.ServerPort + 1;
            httpUri.Host = configuration.ServerHost;

            configuration.DefaultLogger.Log("Preparing to stand up the configuration settings RESTful HTTP service");

            var baseAddresses = new[] {httpUri.Uri};

            _configWebBusHost = new NinjectWebServiceHost(new NinjectServiceBehavior(type => { return new NinjectInstanceProvider(typeof(BusConfigRestService), BusKernel); }, new WcfRequestScopeCleanup(true)), typeof(BusConfigRestService), baseAddresses);

            //var webBusHost = new WebServiceHost(typeof(WcfRestTransporter), baseAddresses);

            var webBusMetadata = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = false,
                    MetadataExporter = {PolicyVersion = PolicyVersion.Policy15},
                };

            _configWebBusHost.Description.Behaviors.Add(webBusMetadata);


            var debug = _configWebBusHost.Description.Behaviors.Find<ServiceDebugBehavior>();

            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                _configWebBusHost.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }

            var busRestBind = new WebHttpBinding(WebHttpSecurityMode.None);

            ServiceEndpoint busEndpoint2 = _configWebBusHost.AddServiceEndpoint(typeof(IBusConfigRestService), busRestBind, "configuration");
            busEndpoint2.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

            _configWebBusHost.Open();

            foreach (ServiceEndpoint addr in _configWebBusHost.Description.Endpoints)
            {
                Console.WriteLine("The service is ready at {0}", addr.Address);
            }

            configuration.DefaultLogger.Log("Finished setting up the configuration settings RESTful HTTP service");

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

            // *********************************** [End] RESTful HTTP ***********************************************
            // *********************************** [Start] TCP Service **********************************************

            var tcpUri = new UriBuilder("net.tcp://localhost/esb/services"); // default
            tcpUri.Port = configuration.ServerPort;
            tcpUri.Host = configuration.ServerHost;

            var tcpBaseAddresses = new[] {tcpUri.Uri};

            configuration.DefaultLogger.Log("Preparing to setup the configuration settings TCP service");

            _configBusHost = new NinjectServiceHost(new NinjectServiceBehavior(type => { return new NinjectInstanceProvider(typeof (BusConfigService), BusKernel); }, new WcfRequestScopeCleanup(true)), typeof (BusConfigService), tcpBaseAddresses);

            debug = _configBusHost.Description.Behaviors.Find<ServiceDebugBehavior>();

            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                _configBusHost.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }

            var tcpBinding = new NetTcpBinding(SecurityMode.None);
            //tcpBinding.PortSharingEnabled = true;

            _configBusHost.AddServiceEndpoint(typeof(IBusConfigService), tcpBinding, "configuration");

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
            _configBusHost.Open();

            foreach (ServiceEndpoint addr in _configBusHost.Description.Endpoints)
            {
                Console.WriteLine("The service is ready at {0}", addr.Address);
            }

            configuration.DefaultLogger.Log("Finished standing up the configuration settings TCP service");

            // *********************************** [End] TCP Service ***********************************************
        }

        private string Selector(string test)
        {
            // this gets called from the transporters
            return null;
        }

        private string RegisteredSubscriber(string test)
        {
            // this gets called from the transporters
            return null;
        }

        public void StartAndWait()
        {
            _config.DefaultLogger.Log("Starting server");

            var task = new Task(() =>
                {
                    Start();

                    _config.DefaultLogger.Log("Server Started.");
                    _signal.WaitOne();
                });

            task.Start();
            task.Wait();
        }

        public void Stop()
        {
            _config.DefaultLogger.Log("Stopping server");

            // stop all transporters
            _serverTransporters.ForEach(st =>
                {
                    try
                    {
                        st.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(ex.Message);
                    }
                });

            _configWebBusHost.Close();
            _configBusHost.Close();

            _configWebBusHost = null;
            _configBusHost = null;

            // reset instance fields
            _startDateTime = DateTime.MinValue.ToUniversalTime();
            RetryCount = -1;
            _signal.Reset();
            _transportManagers.Clear();
            _serverTransporters.Clear();
            MessageMetadata.Clear();

            BusKernel.Unbind<Configuration>();
            BusKernel.Unbind<ITransportManager>();
            BusKernel.Unbind<ISerialization>();


            _signal.Set(); // signal any waiting thread to go.

            _config.DefaultLogger.Log("Server stopped.");
        }


        /// <summary>
        /// Restarts the esb server.  Removes all clients, subscriptions, and published messages.
        /// </summary>
        public void Restart()
        {
            _config.DefaultLogger.Log("Restarting server");

            Stop();

            //// reset instance fields
            //RetryCount = -1;
            //_signal.Reset();
            //_transportManagers.Clear();
            //_serverTransporters.Clear();
            //MessageMetadata.Clear();

            //BusKernel.Unbind<Configuration>();
            //BusKernel.Unbind<ITransportManager>();
            //BusKernel.Unbind<ISerialization>();

            //BusKernel.Dispose();
            //BusKernel = null;

            Start();

            _config.DefaultLogger.Log("Server restarted.");
        }

        //Added the two methods to test the error logging from the bus, rxs, 03/29/2013
        public void Error()
        {
            _config.DefaultLogger.LogErrorStackTrace("An error has occurred in the Bus");
        }

        public void FatalError()
        {
            _config.DefaultLogger.LogFatalStackTraceAndMore("An error has occurred in the Bus");
        }

        public void FlushCache()
        {
            _config.DefaultLogger.Log("Flushing server cache");


            _config.DefaultLogger.Log("Cache flushed.");
        }

        public void Broadcast(string p)
        {
            //throw new NotImplementedException();
        }

        //public int RetainForDays
        //{
        //    get { return _config.MessageSettings.DaysToRetainMessages; }
        //}


        internal IEnumerable<IServerTransporter> FindInterestedTransporters(string messageUri)
        {
            return _serverTransporters.Where(t =>
                {
                    try
                    {
                        return t.InterestedIn(messageUri);
                    }
                    catch (Exception ex)
                    {
                        // log it
                        _config.DefaultLogger.LogError(string.Format("Error in {0}.FindInterest. Ex: {1}", t.Name, ex.Dump()));
                    }

                    return false;
                });
        }

        public List<string> GetTransporterStats()
        {
            var stats = new List<string>();

            _serverTransporters.ForEach(t => stats.Add("Name=" + t.Name + ", Enabled=" + t.Enabled));
            return stats;
        }

        public int DaysToRetainMessages
        {
            get { return _config.MessageSettings.DaysToRetainMessages; }
            set { _config.MessageSettings.DaysToRetainMessages = value; }
        }


        //public List<Client> Clients { get; set; }
        
        //This will only be called from the administration page to remove subscribers matching on Uri
        public List<ISubscriber> RemoveSubscriber(string uri)
        {
            var allSubscribers = new List<ISubscriber>();
            _transportManagers.ForEach(mgr =>
                {
                    allSubscribers.AddRange(mgr.CurrentSubscribers);
                    mgr.RemoveSubscriberList(uri);
                });

            var removeList = allSubscribers.Where(sub => sub.Uri == uri).ToList();
            var returnList = new List<ISubscriber>();
            returnList.AddRange(removeList);

            //removeList.RemoveAll(sub => sub.Uri == uri);
            return returnList;
        }

        public void RemoveClient(string id)
        {
            var allClients = new List<Client>();
            _transportManagers.ForEach(mgr =>
            {
                allClients.AddRange(mgr.CurrentClients);
                mgr.RemoveClientList(id);
            });
        }

        public void RemoveMessage(EsbMessage msg)
        {
            _transportManagers.ForEach(mgr => mgr.CurrentMessages.Remove(msg));
        }
    }
}