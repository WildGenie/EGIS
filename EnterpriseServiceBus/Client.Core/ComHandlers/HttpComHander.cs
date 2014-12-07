using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using GeoDecisions.Esb.Client.Core.Interfaces;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Client.Core.ComHandlers
{
    class HttpComHander : IComHandler
    {
        private readonly Configuration _configuration;
        private HubConnection _hubConnection;
        private IHubProxy _proxy;
        private bool _wasConnected;
        private bool _triedConnect;
        private Timer _pulseTimer;
        private SynchronizedCollection<AsyncMap> _asyncMaps;

        public HttpComHander(Configuration configuration)
        {
            _asyncMaps = new SynchronizedCollection<AsyncMap>();
            _configuration = configuration;
            _hubConnection = new HubConnection("http://localhost:9010/esbdyn");
        }

        public string Name
        {
            get { return Names.HttpComHandler; }
        }

        public int Priority { get; set; }

        public IComHandler Successor { get; set; }

        public IComHandler CreateHandler(Configuration config)
        {
            throw new NotImplementedException();
        }

        public bool Connect()
        {
            if (_hubConnection.State == ConnectionState.Connected)
                return true;

            if (_triedConnect || _wasConnected)
                return false;

            try
            {
                _triedConnect = true;

                _proxy = _hubConnection.CreateHubProxy("EsbHub");

                _hubConnection.Start().Wait();

                if (_hubConnection.State == ConnectionState.Connected)
                {
                    // hook listener; we want to listen for incoming messages here.
                    _proxy.On<EsbMessage>("pushMessage", message =>
                        {
                            if (message == null)
                                return;

                            var maps = _asyncMaps.Where(am => am.Subscribe.Uri == message.Uri).ToList();

                            string payload = Encoding.UTF8.GetString(message.Payload);

                            maps.ForEach(am =>
                            {
                                if (am.MessageType == typeof(EsbMessage))
                                {
                                    am.Handler.DynamicInvoke(message, message);
                                }
                                else if (am.MessageType == typeof(string))
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

                        });

                    _wasConnected = true;

                    var clientAnnounce = new ClientAnnounce()
                        {
                            Id = _configuration.Id,
                            Name = _configuration.Name, //"HttpClient",
                            Description = _configuration.Description, //"HttpClient Description",
                            LastPulse = DateTime.UtcNow
                        };

                    _proxy.Invoke("Announce", clientAnnounce).Wait();

                    StartPulse();
                }
            }
            catch (Exception)
            {
            }

            return _hubConnection.State == ConnectionState.Connected;
        }

        public bool CanHandle(Subscribe subscribe)
        {
            return true;
        }

        public bool CanHandle(EsbMessage message)
        {
            return true;
        }

        public string DispatchMessage(EsbMessage message, string uri = null)
        {
            var msgUri = uri ?? message.Uri;

             //public void Publish(string uri, EsbMessage esbMessage)
            _proxy.Invoke("Publish", msgUri, message);

            return "ok";
        }

        public void DispatchSubscribe<T>(Subscribe subscribe, Action<T, EsbMessage> callback)
        {
            //public string Subscribe(Subscriber subscriber)
            _proxy.Invoke("Subscribe", new Subscriber() {ClientId = _configuration.Id, MachineName = subscribe.MachineName, Uri = subscribe.Uri});
            _asyncMaps.Add(new AsyncMap() {MessageType = typeof (T), Handler = callback, Subscribe = subscribe});
        }

        public void DispatchUnSubscribe(UnSubscribe unSubscribe)
        {
            //public void UnSubscribe(Subscriber subscriber)
            _proxy.Invoke("UnSubscribe", new Subscriber() {ClientId = _configuration.Id, Uri = unSubscribe.Uri});
        }

        public bool Shutdown()
        {
            if (_hubConnection.State == ConnectionState.Connected)
            {
                // tell server see ya
                // public void ClientShutdown(string clientId)
                _proxy.Invoke("ClientShutdown", _configuration.Id).Wait(2000); // wait for 2 seconds max

                StopPulse();
                _hubConnection.Stop();
            }

            _hubConnection = null;
            _triedConnect = false;
            _wasConnected = false;

            return true;
        }

        public void Replay<T>(string uri, double dDays, Action<List<T>, List<EsbMessage>> callback)
        {
            throw new NotImplementedException();
        }


        private void StartPulse()
        {
            //var pulse = new Pulse {ClientId = this._configuration.Id};

            //public void Pulse(string clientId, DateTime time)
            _proxy.Invoke("Pulse", _configuration.Id, DateTime.UtcNow);

            if (_pulseTimer == null)
                _pulseTimer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds); // fire once every 60 seconds

            _pulseTimer.Elapsed += (sender, args) =>
            {
                _pulseTimer.Stop();

                _proxy.Invoke("Pulse", _configuration.Id, DateTime.UtcNow);

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
}
