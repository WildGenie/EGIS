using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Server.Core;
using GeoDecisions.Esb.Server.Core.Models;
using GeoDecisions.Esb.Server.Web.Models;
using Nancy;
using Nancy.Hosting.Self;
using GeoDecisions.Esb.Common.Messages;
using Nancy.ModelBinding;
using Nancy.Responses;

namespace GeoDecisions.Esb.Server.Web
{
    public class Host
    {
        private NancyHost _host;
        private Bus _bus;

        public Host(Bus bus)
        {
           _bus = bus;
            BusInstance = bus;

            //Capture statistic from the bus into the host model 
            BusStats = new Statistics();
            BusStats.Initialize(BusInstance.StartDateTime, BusInstance.Subscribers, BusInstance.Publishers, BusInstance.Messages, BusInstance.Clients, BusInstance.Transporters, BusInstance.Databases);
        }

        public void Start()
        {
            _host = new NancyHost(new ServerBootstrapper(), new Uri("http://localhost:1234"));
            _host.Start();
        }

        public void Stop()
        {
            _host.Stop();
        }

        public static Bus BusInstance { get; private set; }

        public static Statistics BusStats { get; private set; }
    }


    public class EsbModule : NancyModule
    {
        private static readonly string _json2ScriptLocation;
        private static readonly string _signalRScriptLocation;
        private static readonly string _clientScriptLocation;

        static EsbModule()
        {
            string hostNameAndPort = Host.BusInstance.HttpTransporter.Host + ":" + Host.BusInstance.HttpTransporter.Port;
            _json2ScriptLocation = "http://" + hostNameAndPort + "/files/scripts/json2.js";
            _signalRScriptLocation = "http://" + hostNameAndPort + "/files/scripts/jquery.signalR.js";
            _clientScriptLocation = "http://" + hostNameAndPort + "/files/scripts/EsbClient.js";

        }

        public string StringInserter(string source, string insert, int positionOfInsert)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < source.Length - 1; i++)
            {
                sb.Append(source[i]);
                if (i%positionOfInsert == 0)
                {
                    sb.Append(insert);
                }
            }
            return sb.ToString();
        }


        public EsbModule()
        {
            Get["/"] = _ => "Nothing to see here try /esb";
            
            //Main ESB page with statistics
            Get["/esb"] = parameters =>
                {
                    //ViewBag["SubCount"] =  Host.BusInstance.SubscriberCount;

                    Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                    return View["index.html", new
                                                {
                                                    ServerStartTime = Host.BusStats.ServerStartTime,
                                                    SubCount = Host.BusStats.Subscribers.Count,
                                                    PubCount = Host.BusStats.Publishers.Count,
                                                    Message1mCount = Host.BusStats.Messages1m.Count,
                                                    Message1hCount = Host.BusStats.Messages1h.Count,
                                                    MessageCount = Host.BusStats.Messages.Count,
                                                    Clients = Host.BusStats.Clients
                                                }
                               ];
                };

            //clients
            Get["/clients"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["clients.html", new { clients = Host.BusStats.Clients }];
            };

            //Statistics drill down
            Get["/subscribers"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["subscribers.html", new { subscribers = Host.BusStats.Subscribers, subCount = Host.BusStats.Subscribers.Count }];
            };
            
            Get["/publishers"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["publishers.html", new { publishers = Host.BusStats.Publishers, pubCount = Host.BusStats.Publishers.Count }];
            };

            Get["/messages1m"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["messages.html", new
                    {
                        messages = Host.BusStats.Messages1m.Select(msg=> new
                            {
                                Id = msg.Id,
                                Uri=msg.Uri,
                                Type = msg.Type,
                                Publisher = msg.Publisher,
                                PublishedDateTime = msg.PublishedDateTime,
                                PayloadAsString = StringInserter(System.Text.Encoding.UTF8.GetString(msg.Payload), Environment.NewLine, 50)
                            }), 
                        msgCount = Host.BusStats.Messages1m.Count
                    }];
            };

            Get["/messages1h"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["messages.html", new 
                    { 
                        messages = Host.BusStats.Messages1h.Select(msg => new
                            {
                                Id = msg.Id,
                                Uri = msg.Uri,
                                Type = msg.Type,
                                Publisher = msg.Publisher,
                                PublishedDateTime = msg.PublishedDateTime,
                                PayloadAsString = StringInserter(System.Text.Encoding.UTF8.GetString(msg.Payload), Environment.NewLine, 50)
                            }), 
                        msgCount = Host.BusStats.Messages1h.Count 
                    }];
            };

            Get["/messages"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["messages.html", new
                    {
                        messages = Host.BusStats.Messages.Select(msg => new
                            {
                                Id = msg.Id,
                                Uri = msg.Uri,
                                Type = msg.Type,
                                Publisher = msg.Publisher,
                                PublishedDateTime = msg.PublishedDateTime,
                                PayloadAsString = StringInserter(System.Text.Encoding.UTF8.GetString(msg.Payload), Environment.NewLine, 50)
                            }),
                        msgCount = Host.BusStats.Messages.Count
                    }];
            };




            Get["/allStatistics"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);
                return View["allStatistics.html", new
                    {
                        subscribers = Host.BusStats.Subscribers,
                        subCount = Host.BusStats.Subscribers.Count,
                        publishers = Host.BusStats.Publishers,
                        pubCount = Host.BusStats.Publishers.Count,
                        messages = Host.BusStats.Messages.Select(msg => new
                            {
                                msg,
                                PayloadAsString = StringInserter(System.Text.Encoding.UTF8.GetString(msg.Payload), Environment.NewLine, 50)
                            }),
                        msgCount = Host.BusStats.Messages.Count
                    }];
            };


            //Interactive Console
            Get["/console"] = parameters =>
                {

                return View["console.html", new {   SubCount = Host.BusInstance.SubscriberCount,
                                                    HostName = Host.BusInstance.HttpTransporter.Host,
                                                    JSON2ScriptLocation = _json2ScriptLocation,
                                                    SignalRHost = _signalRScriptLocation,
                                                    ClientScriptLocation = _clientScriptLocation
                }].WithFullNegotiation().WithHeader("Access-Control-Allow-Origin", "*");
            };

            Get["/admin"] = parameters =>
                {
                    return View["admin.html", new
                        {
                            SubCount = Host.BusInstance.SubscriberCount,
                            HostName = Host.BusInstance.HttpTransporter.Host,
                            SignalRHost = _signalRScriptLocation,
                            ClientScriptLocation = _clientScriptLocation
                        }];
                };

            Get["/removeSubscribers"] = parameters =>
                {
                    Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                    return View["removeSubscribers.html", new
                        {
                            SubCount = Host.BusInstance.SubscriberCount,
                            HostName = Host.BusInstance.HttpTransporter.Host,
                            SignalRHost = _signalRScriptLocation,
                            ClientScriptLocation = _clientScriptLocation,
                            subscribers = Host.BusInstance.Subscribers
                        }];
                };

            Post["/removeSubscriberUri"] = parameters =>
                {
                    var unsub = this.Bind<UnSubscribe>();

                    Host.BusInstance.RemoveSubscriber(unsub.Uri);

                    Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                    return View["removeSubscribers.html", new
                        {
                            SubCount = Host.BusInstance.SubscriberCount,
                            HostName = Host.BusInstance.HttpTransporter.Host,
                            SignalRHost = _signalRScriptLocation,
                            ClientScriptLocation = _clientScriptLocation,
                            subscribers = Host.BusInstance.Subscribers
                        }];

                    //return Response.AsRedirect("/removeSubscribers");
                };

            Get["/removeClients"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["removeClients.html", new
                {
                    ClientCount = Host.BusInstance.Clients.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    clients = Host.BusInstance.Clients
                }];
            };

            Post["/removeClientId"] = parameters =>
            {
                var cli = this.Bind<Client>();

                Host.BusInstance.RemoveClient(cli.Id);

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["removeClients.html", new
                {
                    ClientCount = Host.BusInstance.Clients.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    clients = Host.BusInstance.Clients
                }];

                //return Response.AsRedirect("/removeSubscribers");
            };

            Get["/restart"] = parameters =>
            {
                return View["restart.html", new
                {
                    clients = Host.BusInstance.Clients
                }];
            };

            Post["/restartServer"] = parameters =>
            {
                Host.BusInstance.Restart();

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["restart.html", new
                {
                    ClientCount = Host.BusInstance.Clients.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    clients = Host.BusInstance.Clients
                }];
            };

            Post["/stopServer"] = parameters =>
            {
                Host.BusInstance.Stop();

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["restart.html", new
                {
                    ClientCount = Host.BusInstance.Clients.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    clients = Host.BusInstance.Clients
                }];
            };

            Post["/startServer"] = parameters =>
            {
                Host.BusInstance.Start();

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["restart.html", new
                {
                    ClientCount = Host.BusInstance.Clients.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    clients = Host.BusInstance.Clients
                }];
            };

            Get["/Transporters"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["transporters.html", new
                {
                    TransporterCount = Host.BusInstance.Transporters.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    transporters = Host.BusInstance.Transporters
                }];
            };

            Get["/Databases"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["databases.html", new
                {
                    DatabaseCount = Host.BusInstance.Databases.Count,
                    HostName = Host.BusInstance.HttpTransporter.Host,
                    SignalRHost = _signalRScriptLocation,
                    ClientScriptLocation = _clientScriptLocation,
                    databases = Host.BusInstance.Databases
                }];
            };

            Get["/MessageAdmin"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["messageAdmin.html", new
                {
                    RetainForDays = Host.BusInstance.DaysToRetainMessages
                }];
            };

            Post["/FlushAllMessages"] = parameters =>
            {
                var n = Host.BusInstance.FlushMessages(0);

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["flushMessagesResult.html", new
                {
                    RetainForDays = 0,
                    NoOfPurgedMessages = n
                }];
            };

            Post["/FlushMessagesKeepLastNDays"] = parameters =>
            {
                    var n = Host.BusInstance.FlushMessages( Host.BusInstance.DaysToRetainMessages );

                    Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                    return View["flushMessagesResult.html", new
                    {
                        RetainForDays = Host.BusInstance.DaysToRetainMessages,
                        NoOfPurgedMessages = n
                    }];
            };

            Get["/ChangeMessageRetainSettings"] = parameters =>
            {
                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["changeSettings.html", new
                {
                    RetainForDays = Host.BusInstance.DaysToRetainMessages,
                }];
            };

            Post["/PostSettings"] = parameters =>
            {
                    var settings = this.Bind<MessageSettings>();

                    Host.BusInstance.PostSettings(settings);

                    Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                    return View["changeSettings.html", new
                    {
                        RetainForDays = Host.BusInstance.DaysToRetainMessages,
                        returnValue = "success"
                    }];
            };

            Get["/ShowMessagesSinceDateTime"] = parameters =>
            {
                var msgSearchParameters = this.Bind<MessageSearchParameters>();

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                return View["messagesDateTime.html", new
                {
                    RetainForDays = Host.BusInstance.DaysToRetainMessages,
                    messages = Host.BusStats.Messages.Select(msg => new
                    {
                        Id = msg.Id,
                        Uri = msg.Uri,
                        Type = msg.Type,
                        Publisher = msg.Publisher,
                        PublishedDateTime = msg.PublishedDateTime,
                        ClientId = msg.ClientId,
                        PayloadAsString = System.Text.Encoding.UTF8.GetString(msg.Payload)
                    }).Where(msg => DateTime.Compare(msg.PublishedDateTime, msg.PublishedDateTime) > 0 &&
                                    msg.Uri == msgSearchParameters.Uri &&
                                    msg.ClientId == msgSearchParameters.ClientId
                                    ),
                    msgCount = Host.BusStats.Messages.Count
                }];
            };

            Get["/SearchMessagesDateTime"] = parameters =>
            {
                var msgSearchParameters = this.Bind<MessageSearchParameters>();

                Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                //Initialize and store messages to replay, this will be used when the user clicks the Replay button in the next step
                //Host.BusStats.MessagesToReplay = new List<EsbMessage>();
                Host.BusStats.MessagesToReplay = Host.BusStats.Messages.Where(msg =>
                                                                                     DateTime.Compare(msg.PublishedDateTime,
                                                                                     msgSearchParameters.PublishedDateTime) > 0 &&
                                                                                     msg.Uri == msgSearchParameters.Uri //&&
                                                                                    //msg.ClientId == msgSearchParameters.ClientId
                                                                            ).ToList();

                return View["messagesDateTime.html", new
                {
                    RetainForDays = Host.BusInstance.DaysToRetainMessages,
                    messages = Host.BusStats.Messages.Select(msg => new
                    {
                        Id = msg.Id,
                        Uri = msg.Uri,
                        Type = msg.Type,
                        Publisher = msg.Publisher,
                        PublishedDateTime = msg.PublishedDateTime,
                        ClientId = msg.ClientId,
                        PayloadAsString = System.Text.Encoding.UTF8.GetString(msg.Payload)
                    }).Where(msg => DateTime.Compare(msg.PublishedDateTime, msgSearchParameters.PublishedDateTime ) > 0 &&
                                    msg.Uri == msgSearchParameters.Uri
                                    ),
                    msgCount = Host.BusStats.Messages.Count
                }];
            };

            Post["/ReplayMessages"] = parameters =>
            {
                //var msgs = this.Bind<List<EsbMessage>>();

                //Host.BusStats.Initialize(Host.BusInstance.StartDateTime, Host.BusInstance.Subscribers, Host.BusInstance.Publishers, Host.BusInstance.Messages, Host.BusInstance.Clients, Host.BusInstance.Transporters, Host.BusInstance.Databases);

                foreach (var msg in  Host.BusStats.MessagesToReplay)
                {
                    //Republish each message
                    Host.BusInstance.RePlay(msg);
                }

                return View["messagesDateTime.html", new
                {
                    RetainForDays = Host.BusInstance.DaysToRetainMessages,
                    messages = Host.BusStats.Messages.Select(msg => new
                    {
                        Id = msg.Id,
                        Uri = msg.Uri,
                        Type = msg.Type,
                        Publisher = msg.Publisher,
                        PublishedDateTime = msg.PublishedDateTime,
                        ClientId = msg.ClientId,
                        PayloadAsString = System.Text.Encoding.UTF8.GetString(msg.Payload)
                    }),
                    msgCount = Host.BusStats.Messages.Count
                }];
            };
        }
    }
}
