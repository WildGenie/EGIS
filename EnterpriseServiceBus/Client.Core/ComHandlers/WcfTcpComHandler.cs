using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Timers;
using GeoDecisions.Esb.Client.Core.Interfaces;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Services;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Client.Core.ComHandlers
{
    internal class WcfTcpComHandler : IComHandler, IBusServiceCallback
    {
        private SynchronizedCollection<AsyncMap> _asyncMaps;
        private readonly Configuration _configuration;
        private IBusService _busService;
        private bool _wasOpened;
        private Timer _pulseTimer;
        private bool _wasConnected;
        private bool _triedConnect;

        private WcfTcpComHandler(Configuration configuration)
        {
            _configuration = configuration;
            _asyncMaps = new SynchronizedCollection<AsyncMap>();
        }

        public bool Connect()
        {
            if (_wasOpened)
                return true;

            if (_triedConnect || _wasConnected) // if it was connected; always return false TODO: we need to make this a little less one-and-done...
                return false;

            _triedConnect = true;

            if (SetupChannel(_configuration))
            {
                StartPulse();
                return true;
            }
            return false;
        }


        public string Receiver(EsbMessage message)
        {
            List<AsyncMap> maps = _asyncMaps.Where(am => am.Subscribe.Uri == message.Uri).ToList();

            string payload = Encoding.UTF8.GetString(message.Payload);

            maps.ForEach(am =>
                {
                    if (am.MessageType == typeof (EsbMessage))
                    {
                        am.Handler.DynamicInvoke(message, message);
                    }
                    else if (am.MessageType == typeof (string))
                    {
                        am.Handler.DynamicInvoke(payload, message);
                    }
                    else
                    {
                        try
                        {
                            object obj = JsonSerializer.DeserializeFromString(payload, am.MessageType);
                            am.Handler.DynamicInvoke(obj, message);
                        }
                        catch (Exception ex)
                        {

                            //TODO: Add exceptions to list
                            //message.Errors.Add(ex.Dump());
                            am.Handler.DynamicInvoke(null, message);
                        }
                    
                    }
                });

            return null;
        }

        public IComHandler CreateHandler(Configuration config)
        {
            return new WcfTcpComHandler(config);
        }

        public int Priority { get; set; }

        public string Name
        {
            get { return Names.WcfTcpComHandler; }
        }

        public IComHandler Successor { get; set; }

        public bool CanHandle(Subscribe subscribe)
        {
            //return subscribe.Uri.StartsWith("esb://");
            return true;
            //throw new NotImplementedException();
        }

        public bool CanHandle(EsbMessage message)
        {
            //return message.Uri.StartsWith("esb://");
            return true;
            //throw new NotImplementedException();
        }

        public string DispatchMessage(EsbMessage message, string url = null)
        {
            _busService.Publish(message);
            return "ok";
        }

        public void DispatchSubscribe<T>(Subscribe subscribe, Action<T, EsbMessage> callback)
        {
            _configuration.DefaultLogger.Log("Dispatching subscribe message from comHandler:" + Name + ", SubscribeMsg:" + subscribe.Uri + ", Type:" + subscribe.Type);

            Subscription subscription = _busService.Subscribe(subscribe);

            // this is how we map subscriptions to handlers for this Comhandler
            _asyncMaps.Add(new AsyncMap {Subscribe = subscribe, Handler = callback, MessageType = typeof (T)});
        }



        public void Replay<T>(string uri, double dDays, Action<List<T>, List<EsbMessage>> callback)
        {
            //This call will get all messages satisfying the uri and dateTime criteria
            List<EsbMessage> messages = _busService.GetMessages(uri, DateTime.Now.AddDays(-1 * dDays));

            //This call will get all messages satisfying the uri and dateTime criteria and only messages of type T
            //List<T> customMessages = _busService.GetCustomMessages<T>(uri, DateTime.Now.AddDays(-1 * dDays));

            var customMessages = messages.Where(msg => msg.Type == typeof (T).ToString());

            List<T> customMessageList = new List<T>();

            foreach (EsbMessage msg in customMessages)
            {
                var hydrated = Encoding.UTF8.GetString(msg.Payload).FromJson<T>();
                customMessageList.Add(hydrated);
            }

            callback.Invoke(customMessageList, messages);

            //This line was just a work around to get the custom messages accross to the callback, a better mechanism was devised
            //callback.Invoke(EnumerableExtensions.ToList<T>(messages), messages);

            /*
            var tcpBinding = new NetTcpBinding(SecurityMode.None);
            var replayEndPoint = new EndpointAddress(string.Format("net.tcp://{0}:{1}/esb/services/replay", _configuration.EsbServerIp, 9913));
            var channelFactory = new ChannelFactory<IBusMessageService>(tcpBinding);
            IBusMessageService messageClient = channelFactory.CreateChannel(replayEndPoint);

            if (messageClient != null)
            {
                List<EsbMessage> message = messageClient.GetMessages(uri, DateTime.Now.AddDays(-1));
                callback.Invoke(EnumerableExtensions.ToList<T>(message), message);
            }
            */
        }


        public void DispatchUnSubscribe(UnSubscribe unSubscribe)
        {
            _busService.UnSubscribe(unSubscribe);

            var foundMaps = _asyncMaps.Where(am => am.Subscribe.Uri == unSubscribe.Uri && am.Subscribe.ClientId == unSubscribe.ClientId).ToList();

            foundMaps.ForEach(map =>
                {
                    _asyncMaps.Remove(map);
                });
        }


        private bool SetupChannel(Configuration configuration)
        {
            try
            {
                var tcpBinding = new NetTcpBinding(SecurityMode.None)
                    {
                        SendTimeout = TimeSpan.FromHours(24),
                        ReceiveTimeout = TimeSpan.FromHours(24),
                        ReliableSession = {InactivityTimeout = TimeSpan.FromHours(24)},
                        PortSharingEnabled = true,
                        MaxReceivedMessageSize = int.MaxValue,
                        MaxBufferSize = int.MaxValue
                    };

                var busEndpoint = new EndpointAddress(string.Format("net.tcp://{0}:{1}/esb/bus", configuration.WcfTcpHost, configuration.WcfTcpPort));
                var dupChannelFactory = new DuplexChannelFactory<IBusService>(new InstanceContext(this), tcpBinding, busEndpoint);
                _busService = dupChannelFactory.CreateChannel(busEndpoint);

                ((ICommunicationObject) _busService).Opened += (sender, args) => { _wasOpened = true; };

                ((ICommunicationObject) _busService).Faulted += (sender, args) =>
                    {
                        ((ICommunicationObject) sender).Abort();

                        if (sender is IBusService)
                        {
                            if (_wasOpened)
                            {
                                StopPulse();
                                _wasOpened = false;
                                SetupChannel(configuration);
                                StartPulse();
                            }
                        }
                    };

                // announce
                _busService.Announce(new ClientAnnounce()
                    {
                        Id = _configuration.Id,
                        Name = "Default TCP",
                        Description = "Default TCP client description.",
                        LastPulse = DateTime.UtcNow
                    });

                _wasConnected = ((ICommunicationObject)_busService).State == CommunicationState.Opened;
                
                return ((ICommunicationObject) _busService).State == CommunicationState.Opened;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void HandleMessage()
        {
            throw new NotImplementedException();
        }

        public bool Shutdown()
        {
            StopPulse();

            if (_busService != null && _wasOpened)
            {
                _busService.ClientShutdown(new UnSubscribe() {ClientId = _configuration.Id});
                ((ICommunicationObject) _busService).Close();
            }

            _asyncMaps = null;
            _busService = null;

            return true;
        }


        private void StartPulse()
        {
            if (_pulseTimer == null)
                _pulseTimer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds); // fire once every 60 seconds

            _pulseTimer.Elapsed += (sender, args) =>
            {
                _pulseTimer.Stop();

                _busService.Pulse(new Pulse() { ClientId = this._configuration.Id });

                _pulseTimer.Start();
            };

            _pulseTimer.Start();

        }

        private void StopPulse()
        {
            if (_pulseTimer != null)
                _pulseTimer.Stop();
        }
    }

    internal class AsyncMap
    {
        public Subscribe Subscribe { get; set; }
        public Delegate Handler { get; set; }
        public Type MessageType { get; set; }
    }
}