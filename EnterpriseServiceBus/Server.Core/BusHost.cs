//using System;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Linq;
//using System.Reflection;
//using System.Security.Cryptography.X509Certificates;
//using System.ServiceModel;
//using System.ServiceModel.Description;
//using System.ServiceModel.Security;
//using System.ServiceModel.Web;
//using GeoDecisions.Esb.Common.Communications;
//using GeoDecisions.Esb.Common.Services;
//using GeoDecisions.Esb.Server.Core.Interfaces;
//using GeoDecisions.Esb.Server.Core.Services;
////using SuperSocket.SocketBase;
////using SuperSocket.SocketBase.Config;
////using SuperSocket.SocketBase.Logging;
////using SuperSocket.SocketBase.Protocol;
////using SuperWebSocket;
////using SuperWebSocket.Config;
////using SuperWebSocket.SubProtocol;

//namespace GeoDecisions.Esb.Server.Core
//{
//    /// <summary>
//    ///     WcfSubCommand base
//    /// </summary>
//    //public abstract class WcfSubCommandBase : SubCommandBase<WcfWebSocketSession>
//    //{
//    //}
//    //public class WcfWebSocketSession : WebSocketSession<WcfWebSocketSession>
//    //{
//    //}

//    public class WcfMethod
//    {
//        public WebGetAttribute Get { get; set; }
//        public WebInvokeAttribute Invoke { get; set; }

//        public UriTemplate Template { get; set; }
//        public MethodInfo Method { get; set; }
//    }

//    public class WcfInvocationInfo
//    {
//        public WcfInvocationInfo()
//        {
//            Methods = new List<WcfMethod>();
//        }

//        public string Name { get; set; }
//        public ConstructorInfo Ctor { get; set; }
//        //public MethodInfo Mi1 { get; set; }

//        public List<WcfMethod> Methods { get; set; }
//        public UriTemplateTable TemplateTable { get; set; }
//    }

//    //public class WcfSubProtocol : ISubProtocol<WebSocketSession>
//    //{
//    //    private static List<WcfInvocationInfo> _invocationInfos;
//    //    private readonly Assembly _serviceAsm;
//    //    private readonly WcfSubCommandParser _wcfSubCommandParser;
//    //    private Dictionary<string, UriTemplateTable> _templateTables;

//    //    public WcfSubProtocol(Assembly serviceAsm = null)
//    //    {
//    //        _invocationInfos = new List<WcfInvocationInfo>();
//    //        _wcfSubCommandParser = new WcfSubCommandParser();
//    //        _serviceAsm = serviceAsm ?? Assembly.GetExecutingAssembly();
//    //    }


//    //    public string Name
//    //    {
//    //        get { return "Wcf"; }
//    //    }

//    //    public IRequestInfoParser<SubRequestInfo> SubRequestParser
//    //    {
//    //        get { return _wcfSubCommandParser; }
//    //    }

//    //    public bool Initialize(IAppServer appServer, SubProtocolConfig protocolConfig, ILog logger)
//    //    {
//    //        //m_Logger = logger;

//    //        //var config = appServer.Config;

//    //        //m_GlobalFilters = appServer.GetType()
//    //        //        .GetCustomAttributes(true)
//    //        //        .OfType<SubCommandFilterAttribute>()
//    //        //        .Where(a => string.IsNullOrEmpty(a.SubProtocol) || Name.Equals(a.SubProtocol, StringComparison.OrdinalIgnoreCase)).ToArray();

//    //        //if (Name.Equals(DefaultName, StringComparison.OrdinalIgnoreCase))
//    //        //{
//    //        //    var commandAssembly = config.Options.GetValue("commandAssembly");

//    //        //    if (!string.IsNullOrEmpty(commandAssembly))
//    //        //    {
//    //        //        if (!ResolveCommmandAssembly(commandAssembly))
//    //        //            return false;
//    //        //    }
//    //        //}

//    //        //if (protocolConfig != null && protocolConfig.Commands != null)
//    //        //{
//    //        //    foreach (var commandConfig in protocolConfig.Commands)
//    //        //    {
//    //        //        var assembly = commandConfig.Options.GetValue("assembly");

//    //        //        if (!string.IsNullOrEmpty(assembly))
//    //        //        {
//    //        //            if (!ResolveCommmandAssembly(assembly))
//    //        //                return false;
//    //        //        }
//    //        //    }
//    //        //}

//    //        //Always discover commands
//    //        //DiscoverCommands();

//    //        DiscoverServices();

//    //        return true;
//    //    }


//    //    private void DiscoverServices()
//    //    {
//    //        // get all interfaces with  [ServiceContract] 
//    //        IEnumerable<Type> serviceContracts = _serviceAsm.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes<ServiceContractAttribute>().Any());

//    //        foreach (Type serviceContract in serviceContracts)
//    //        {
//    //            var wcfInvokeInfo = new WcfInvocationInfo();

//    //            var contract = serviceContract.GetCustomAttribute<ServiceContractAttribute>();
//    //            wcfInvokeInfo.Name = contract.Name; // we will use this for our first level uri segment (ex):   /metadata

//    //            Type instanceType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.IsClass &&
//    //                                                                                               t.GetInterfaces().Contains(serviceContract) &&
//    //                                                                                               t.GetConstructor(Type.EmptyTypes) != null);
//    //            if (instanceType == null)
//    //            {
//    //                // todo: log and continue
//    //                continue;
//    //            }

//    //            wcfInvokeInfo.Ctor = instanceType.GetConstructor(Type.EmptyTypes); // get ctor and store

//    //            IEnumerable<MethodInfo> allMethods = serviceContract.GetMethods().Where(mi => mi.GetCustomAttributes<WebGetAttribute>().Any() || mi.GetCustomAttributes<WebInvokeAttribute>().Any());

//    //            allMethods.ToList().ForEach(mi =>
//    //                {
//    //                    var getter = mi.GetCustomAttribute<WebGetAttribute>();
//    //                    var invoker = mi.GetCustomAttribute<WebInvokeAttribute>();

//    //                    if (getter != null)
//    //                    {
//    //                        wcfInvokeInfo.Methods.Add(new WcfMethod
//    //                            {
//    //                                Method = mi,
//    //                                Template = getter.UriTemplate != null ? new UriTemplate(getter.UriTemplate) : null,
//    //                                Get = getter
//    //                            });
//    //                    }

//    //                    if (invoker != null)
//    //                    {
//    //                        wcfInvokeInfo.Methods.Add(new WcfMethod
//    //                            {
//    //                                Method = mi,
//    //                                Template = invoker.UriTemplate != null ? new UriTemplate(invoker.UriTemplate) : null,
//    //                                Invoke = invoker
//    //                            });
//    //                    }
//    //                });

//    //            _invocationInfos.Add(wcfInvokeInfo);
//    //        }
//    //    }

//    //    public bool TryGetCommand(string name, out ISubCommand<WebSocketSession> command)
//    //    {
//    //        command = new WcfServiceCommand();
//    //        return true;
//    //    }

//    //    private class WcfServiceCommand : SubCommandBase
//    //    {
//    //        public override void ExecuteCommand(WebSocketSession session, SubRequestInfo requestInfo)
//    //        {
//    //            // this is the only place we have session...
//    //            string path = "http://localhost" + session.Path;

//    //            var wcfInfo = requestInfo as WcfSubRequestInfo; // do stuff with the wcf info

//    //            // perform url template matching on path

//    //            //var mdChannelFactory = new ChannelFactory<IMetadataService>(); //, _mdEndpoint);
//    //            //var channel = mdChannelFactory.

//    //            //var channel = new ChannelFactory().
//    //            //using (var scope = new OperationContextScope(new OperationContext(null)))
//    //            //{

//    //            WcfInvocationInfo invocInfo = _invocationInfos.SingleOrDefault(invoc => invoc.Name == "metadata");

//    //            invocInfo.Methods.ToList().ForEach(mi =>
//    //                {
//    //                    if (mi.Template == null)
//    //                    {
//    //                        // check method name
//    //                        string methodGuess = path.Split('/').Last();
//    //                        if (mi.Method.Name.Equals(methodGuess, StringComparison.OrdinalIgnoreCase))
//    //                        {
//    //                            // found it, instantiate class and call method
//    //                            object wcfService = invocInfo.Ctor.Invoke(new object[] {});

//    //                            var parameters = new List<object>();

//    //                            string queryString = path.Split('?').Last();
//    //                            if (!string.IsNullOrEmpty(queryString))
//    //                            {
//    //                                var queryParameters = new NameValueCollection();
//    //                                string[] querySegments = path.Split('&');
//    //                                foreach (string segment in querySegments)
//    //                                {
//    //                                    string[] parts = segment.Split('=');
//    //                                    if (parts.Length > 0)
//    //                                    {
//    //                                        string key = parts[0].Trim(new[] {'?', ' '});
//    //                                        string val = parts[1].Trim();

//    //                                        queryParameters.Add(key, val);
//    //                                        parameters.Add(val);
//    //                                    }
//    //                                }
//    //                            }


//    //                            mi.Method.Invoke(wcfService, parameters.ToArray());
//    //                        }
//    //                    }
//    //                    else
//    //                    {
//    //                        var template = new UriTemplate("weather/{state}/{city}?forecast={day}");
//    //                        var prefix = new Uri("http://localhost");

//    //                        var fullUri = new Uri("http://localhost/weather/Washington/Redmond?forecast=today");
//    //                        UriTemplateMatch results = template.Match(prefix, fullUri);

//    //                        UriTemplateMatch m = mi.Template.Match(new Uri("http://localhost"), new Uri(path));

//    //                        if (m != null)
//    //                        {
//    //                            // matched!
//    //                        }
//    //                    }
//    //                });

//    //            //var service = _test.Ctor.Invoke(new object[] { });


//    //            //_mdEndpoint = new EndpointAddress("net.tcp://localhost:9001/esb/metadata");
//    //            //var binding = new NetTcpBinding(SecurityMode.None);
//    //            //_mdChannelFactory = new ChannelFactory<IMetadataService>(binding, _mdEndpoint);
//    //            //var channel = _mdChannelFactory.CreateChannel(_mdEndpoint);
//    //            //var metadata = channel.GetMetadata(DateTime.MinValue);

//    //            //var result = _test.Mi1.Invoke(service, new object[] { "hello world" });
//    //            //}

//    //            //// setup wcf context
//    //            //OperationContext.Current = new OperationContext(null);

//    //            foreach (var p in requestInfo.Body.Split(' '))
//    //            {
//    //                session.Send(p);
//    //            }
//    //        }
//    //    }

//    //    // Summary:
//    //    //     SubProtocol RequestInfo type

//    //    public class WcfSubCommandParser : IRequestInfoParser<SubRequestInfo>
//    //    {
//    //        #region ISubProtocolCommandParser Members

//    //        /// <summary>
//    //        ///     Parses the request info.
//    //        /// </summary>
//    //        /// <param name="source">The source.</param>
//    //        /// <returns></returns>
//    //        public SubRequestInfo ParseRequestInfo(string source)
//    //        {
//    //            string cmd = source.Trim();
//    //            int pos = cmd.IndexOf(' ');
//    //            string name;
//    //            string param;

//    //            if (pos > 0)
//    //            {
//    //                name = cmd.Substring(0, pos);
//    //                param = cmd.Substring(pos + 1);
//    //            }
//    //            else
//    //            {
//    //                name = cmd;
//    //                param = string.Empty;
//    //            }

//    //            pos = name.IndexOf('-');

//    //            string token = string.Empty;

//    //            if (pos > 0)
//    //            {
//    //                token = name.Substring(pos + 1);
//    //                name = name.Substring(0, pos);
//    //            }

//    //            return new WcfSubRequestInfo(name, token, param);
//    //        }

//    //        #endregion
//    //    }

//    //    public class WcfSubRequestInfo : SubRequestInfo //RequestInfo<string>, ISubRequestInfo, IRequestInfo
//    //    {
//    //        public WcfSubRequestInfo(string key, string token, string data) : base(key, token, data)
//    //        {
//    //        }

//    //        // Summary:
//    //        //     Gets the token of this request, used for callback
//    //        public string Token
//    //        {
//    //            get { return null; }
//    //        }
//    //    }
//    //}

//    //public class ECHO : SubCommandBase
//    //{
//    //    public override void ExecuteCommand(WebSocketSession session, SubRequestInfo requestInfo)
//    //    {
//    //        foreach (var p in requestInfo.Body.Split(' '))
//    //        {
//    //            session.Send(p);
//    //        }
//    //    }
//    //}

//    //public class Say : SubCommandBase
//    //{
//    //    public override void ExecuteCommand(WebSocketSession session, SubRequestInfo requestInfo)
//    //    {
//    //        foreach (var s in session.AppServer.GetAllSessions())
//    //        {
//    //            s.Send(requestInfo.Body);
//    //        }
//    //    }
//    //}

//    public class BusHost
//    {


//        private readonly Configuration _configuration;
//        private IEnumerable<ServiceHost> _hosts;
//        //private string baseAddressFormat = string.Format("{0}://{}:9000/esb");

//        public BusHost(Configuration configuration)
//        {
//            _configuration = configuration;
//        }

//        public void Start()
//        {

//             //_hosts = StartRESTHost();
//            // _hosts = StartTCPHost();
//            // _hosts = StartWebSocketHost();
//        }

//        //private WebSocketServer StartWebSocketHost()
//        //{
//        //    string uri = "";

//        //    //ISubProtocol<WebSocketSession> sp = new BasicSubProtocol(); 

//        //    var m_WebSocketServer = new WebSocketServer(new WcfSubProtocol(serviceAsm: Assembly.GetAssembly(typeof (IMetadataService)))); //new BasicSubProtocol("Basic", new[] {Assembly.GetExecutingAssembly()}));

//        //    m_WebSocketServer.Setup(new ServerConfig
//        //        {
//        //            Port = 9002,
//        //            Ip = "Any",
//        //            MaxConnectionNumber = 100,
//        //            Mode = SocketMode.Tcp,
//        //            Name = "SuperWebSocket Server"
//        //        }
//        //        //, logFactory: new ConsoleLogFactory()
//        //        );


//        //    m_WebSocketServer.NewSessionConnected += session => { session.Send("Hello"); };

//        //    //m_WebSocketServer.NewRequestReceived += (session, info) =>
//        //    //    {
//        //    //        Console.WriteLine(info.Key);
//        //    //    };

//        //    //m_WebSocketServer.NewMessageReceived += (session, value) =>
//        //    //    {
//        //    //        // need to somehow map this path to the wcf interface
//        //    //        // session.Path;

//        //    //        //session.
//        //    //        Console.WriteLine(value);
//        //    //    };

//        //    if (!m_WebSocketServer.Start())
//        //        throw new Exception("web socket server not started.");


//        //    Console.WriteLine("The service is ready at {0}", string.Format("ws://{0}:{1}/esb/metadata", Environment.MachineName, m_WebSocketServer.Config.Port));

//        //    return m_WebSocketServer;

//        //    //var webSocket = new WebSocket(uri, subProtocol, cookies, customHeaderItems, userAgent, origin, version);
//        //    //webSocket.Closed += (sender, args) => { };
//        //    //webSocket.MessageReceived += (sender, args) => { };
//        //    //webSocket.Opened += (sender, args) => { };
//        //    //webSocket.Error += (sender, args) => { };
//        //    //webSocket.EnableAutoSendPing = true;


//        //    //var websocket = new WebSocket("ws://localhost:4001/");
//        //    //websocket.Opened += (sender, args) => { };

//        //    //websocket.MessageReceived += (sender, args) =>
//        //    //    {

//        //    //    };

//        //    //websocket.Open();

//        //    //var host = new WebSocketHost(typeof(EchoService2), new Uri("http://localhost:9000/esb"));
//        //    //host.AddWebSocketEndpoint();
//        //    //host.Open();


//        //    //var webHost = new WebServiceHost(typeof(MetadataService), new Uri("http://localhost:9000/esb"));

//        //    //var webHostMetadatabehavior = new ServiceMetadataBehavior
//        //    //{
//        //    //    HttpGetEnabled = false,
//        //    //    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
//        //    //};

//        //    //webHost.Description.Behaviors.Add(webHostMetadatabehavior);

//        //    ////var webSocketBinding = new CustomBinding();
//        //    ////webSocketBinding.Elements.Add(new BinaryMessageEncodingBindingElement());
//        //    ////webSocketBinding.Elements.Add(new HttpTransportBindingElement()
//        //    ////    {
//        //    ////        WebSocketSettings = new WebSocketTransportSettings()
//        //    ////            {
//        //    ////                TransportUsage = WebSocketTransportUsage.Always,
//        //    ////                CreateNotificationOnConnection = true
//        //    ////            }
//        //    ////    });

//        //    ////var endpoint1 = webHost.AddServiceEndpoint(typeof(IMetadataService), webSocketBinding, _configuration.MetadataEndpoint + "Sock");

//        //    //var b2 = new NetHttpBinding(BasicHttpSecurityMode.None);

//        //    //var endpoint1 = webHost.AddServiceEndpoint(typeof(IMetadataService), b2, _configuration.MetadataEndpoint + "Sock");


//        //    ////endpoint1.Behaviors.Add(new WebHttpBehavior() { AutomaticFormatSelectionEnabled = true });

//        //    //webHost.Open();

//        //    //foreach (var addr in webHost.Description.Endpoints)
//        //    //{
//        //    //    Console.WriteLine("The service is ready at {0}", addr.Address);
//        //    //}
//        //}

//        private IEnumerable<ServiceHost> StartTCPHost()
//        {
//            string netTcpScheme = "net.tcp";
//            var tcpBaseUri = new Uri(string.Format("{0}://{1}:9001/esb", netTcpScheme, Environment.MachineName));

//            //var baseAddresses = new[] { _configuration.TcpBaseUri, _configuration.HttpBaseUrl };

//            // Create the ServiceHost.
//            var host = new ServiceHost(typeof (MetadataService), tcpBaseUri);

//            // Enable metadata publishing.
//            var smb = new ServiceMetadataBehavior
//                {
//                    HttpGetEnabled = false,
//                    MetadataExporter = {PolicyVersion = PolicyVersion.Policy15},
//                };

//            host.Description.Behaviors.Add(smb);

//            host.Description.Behaviors.Add(new BusServiceContextBehavior());

//            //var endpoint = new ServiceEndpoint(new ContractDescription(""), new NetTcpBinding(SecurityMode.None), new EndpointAddress(""));
//            //host.SetEndpointAddress(endpoint, "");

//            // bindings
//            host.AddServiceEndpoint(typeof (IMetadataService), new NetTcpBinding(SecurityMode.None), _configuration.MetadataEndpoint);

//            //host.AddServiceEndpoint(typeof(IBusService), new NetTcpBinding(SecurityMode.None), "bus");

//            //host.AddServiceEndpoint(typeof(IMetadataService), new BasicHttpBinding(BasicHttpSecurityMode.None), _configuration.MetadataEndpoint);

//            // add custom inspectors... etc is possible
//            //foreach (ChannelDispatcher chDisp in host.ChannelDispatchers)
//            //{
//            //    foreach (EndpointDispatcher epDisp in chDisp.Endpoints)
//            //    {
//            //        epDisp.DispatchRuntime.MessageInspectors.Add(new Inspector());
//            //        foreach (DispatchOperation op in epDisp.DispatchRuntime.Operations)
//            //            op.ParameterInspectors.Add(new Inspector());
//            //    }
//            //}

//            // Open the ServiceHost to start listening for messages. Since
//            // no endpoints are explicitly configured, the runtime will create
//            // one endpoint per base address for each service contract implemented
//            // by the service.
//            host.Open();

//            foreach (ServiceEndpoint addr in host.Description.Endpoints)
//            {
//                Console.WriteLine("The service is ready at {0}", addr.Address);
//            }


//            var busHost = new ServiceHost(typeof (BusService), tcpBaseUri);

//            // Enable metadata publishing.
//            //var busMetadata = new ServiceMetadataBehavior
//            //{
//            //    HttpGetEnabled = false,
//            //    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
//            //};

//            //busHost.Description.Behaviors.Add(busMetadata);

//            var debug = busHost.Description.Behaviors.Find<ServiceDebugBehavior>();

//            // if not found - add behavior with setting turned on 
//            if (debug == null)
//            {
//                busHost.Description.Behaviors.Add(new ServiceDebugBehavior {IncludeExceptionDetailInFaults = true});
//            }
//            else
//            {
//                // make sure setting is turned ON
//                if (!debug.IncludeExceptionDetailInFaults)
//                {
//                    debug.IncludeExceptionDetailInFaults = true;
//                }
//            }

//            busHost.Description.Behaviors.Add(new BusServiceContextBehavior());

//            ServiceEndpoint endpt = busHost.AddServiceEndpoint(typeof (IBusService), new NetTcpBinding(SecurityMode.None), "bus");


//            foreach (ServiceEndpoint endpoint in busHost.Description.Endpoints)
//            {
//                foreach (OperationDescription operation in endpoint.Contract.Operations)
//                {
//                    var dcsob = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();

//                    if (dcsob != null)
//                    {
//                        dcsob.DataContractResolver = new EsbSerializationResolver();
//                        //dcsob.DataContractResolver = new MyResolver();
//                    }
//                }
//            }


//            // add custom inspectors... etc is possible
//            //foreach (ChannelDispatcher chDisp in host.ChannelDispatchers)
//            //{
//            //    foreach (EndpointDispatcher epDisp in chDisp.Endpoints)
//            //    {
//            //        epDisp.DispatchRuntime.MessageInspectors.Add(new Inspector());
//            //        foreach (DispatchOperation op in epDisp.DispatchRuntime.Operations)
//            //            op.ParameterInspectors.Add(new Inspector());
//            //    }
//            //}

//            // Open the ServiceHost to start listening for messages. Since
//            // no endpoints are explicitly configured, the runtime will create
//            // one endpoint per base address for each service contract implemented
//            // by the service.
//            busHost.Open();

//            foreach (ServiceEndpoint addr in busHost.Description.Endpoints)
//            {
//                Console.WriteLine("The service is ready at {0}", addr.Address);
//            }


//            // subscription host

//            var subHost = new ServiceHost(typeof (SubscriptionService), tcpBaseUri);

//            // if not found - add behavior with setting turned on 
//            if (debug == null)
//            {
//                host.Description.Behaviors.Add(new ServiceDebugBehavior {IncludeExceptionDetailInFaults = true});
//            }
//            else
//            {
//                // make sure setting is turned ON
//                if (!debug.IncludeExceptionDetailInFaults)
//                {
//                    debug.IncludeExceptionDetailInFaults = true;
//                }
//            }

//            subHost.Description.Behaviors.Add(new BusServiceContextBehavior());

//            ServiceEndpoint endpt2 = subHost.AddServiceEndpoint(typeof (ISubscriptionService), new NetTcpBinding(SecurityMode.None), "subscription");

//            foreach (ServiceEndpoint endpoint in subHost.Description.Endpoints)
//            {
//                foreach (OperationDescription operation in endpoint.Contract.Operations)
//                {
//                    var dcsob = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();

//                    if (dcsob != null)
//                    {
//                        dcsob.DataContractResolver = new EsbSerializationResolver();
//                        //dcsob.DataContractResolver = new MyResolver();
//                    }
//                }
//            }

//            subHost.Open();

//            foreach (ServiceEndpoint addr in subHost.Description.Endpoints)
//            {
//                Console.WriteLine("The service is ready at {0}", addr.Address);
//            }


//            return new[] {host, busHost, subHost};
//        }

//        private IEnumerable<ServiceHost> StartRESTHost()
//        {
//            var baseAddresses = new[] {_configuration.TcpBaseUri, _configuration.HttpBaseUrl};

//            var webHost = new WebServiceHost(typeof (MetadataService), baseAddresses);

//            var webHostMetadatabehavior = new ServiceMetadataBehavior
//                {
//                    HttpGetEnabled = false,
//                    MetadataExporter = {PolicyVersion = PolicyVersion.Policy15},
//                };

//            webHost.Description.Behaviors.Add(webHostMetadatabehavior);

//            var restBinding = new WebHttpBinding(WebHttpSecurityMode.None);

//            ServiceEndpoint endpoint1 = webHost.AddServiceEndpoint(typeof (IMetadataService), restBinding, _configuration.MetadataEndpoint);
//            endpoint1.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

//            webHost.Description.Behaviors.Add(new BusServiceContextBehavior());

//            webHost.Open();

//            foreach (ServiceEndpoint addr in webHost.Description.Endpoints)
//            {
//                Console.WriteLine("The service is ready at {0}", addr.Address);
//            }


//            var webBusHost = new WebServiceHost(typeof (BusService), baseAddresses);

//            var webBusMetadata = new ServiceMetadataBehavior
//                {
//                    HttpGetEnabled = false,
//                    MetadataExporter = {PolicyVersion = PolicyVersion.Policy15},
//                };

//            webBusHost.Description.Behaviors.Add(webBusMetadata);
//            webBusHost.Description.Behaviors.Add(new BusServiceContextBehavior());

//            var busRestBind = new WebHttpBinding(WebHttpSecurityMode.None);

//            //var busEndpoint = webBusHost.AddServiceEndpoint(typeof (IBusService), busRestBind, "bus");
//            //busEndpoint.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

//            var busEndpoint2 = webBusHost.AddServiceEndpoint(typeof (IBusRestService), busRestBind, "rest");
//            busEndpoint2.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

//            webBusHost.Open();

//            foreach (ServiceEndpoint addr in webBusHost.Description.Endpoints)
//            {
//                Console.WriteLine("The service is ready at {0}", addr.Address);
//            }

//            //ServiceAuthenticationBehavior sab = null;
//            //sab = webHost.Description.Behaviors.Find<ServiceAuthenticationBehavior>();

//            //if (sab == null)
//            //{
//            //    sab = new ServiceAuthenticationBehavior();
//            //    sab.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Negotiate | AuthenticationSchemes.Digest | AuthenticationSchemes.Anonymous;
//            //    webHost.Description.Behaviors.Add(sab);
//            //}
//            //else
//            //{
//            //    sab.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Negotiate | AuthenticationSchemes.Digest;
//            //}

//            return new[] {webHost, webBusHost};
//        }


//        internal void Stop()
//        {
//            // shutdown hosts
//            _hosts.ToList().ForEach(sh => sh.Close()); // close them 

//            //Thread.Sleep(500);
//        }
//    }
//}

