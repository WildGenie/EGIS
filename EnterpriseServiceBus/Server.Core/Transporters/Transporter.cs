using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;
using Microsoft.Win32;

namespace GeoDecisions.Esb.Server.Core.Transporters
{
    public abstract class Transporter<TSub> : IServerTransporter where TSub : ISubscriber
    {
        private readonly Configuration _configuration;
        private readonly ITransportManager _transportManager;

        // create a private list to store the pulses
        private readonly ConcurrentDictionary<string, DateTime> _pulses;
        readonly object _updatePulseLock = new object();

        protected Transporter(ITransportManager transportManager, Configuration configuration)
        {
            _updatePulseLock = new object();
            _transportManager = transportManager;
            _configuration = configuration;
            _pulses = new ConcurrentDictionary<string, DateTime>();

            // handle pulse checker, expired clients should be cleaned up
            PulseHandler = (dateTime) =>
                {
                    try
                    {
                        //Configuration.DefaultLogger.Log(string.Format("Checking pulse timeouts @ {0}", DateTime.UtcNow.ToLongTimeString()));

                        // check for expired pulses
                        var expiredPulses = Pulses.Where(kvp => kvp.Value.AddMinutes(5) <= dateTime).ToList(); // pulses older than 5 minutes...
                        //var expiredPulses = _transportManager.Clients.Where(kvp => kvp.Value.Id kvp.Value.LastPulse.AddMinutes(5) <= dateTime).ToList();

                        expiredPulses.ForEach(kvp =>
                            {
                                Client client;
                                DateTime lastPulse;
                                var clientId = kvp.Key;
                                _pulses.TryRemove(clientId, out lastPulse);
                                //_transportManager.Clients.TryRemove(clientId, out client);
                                EvictClient(clientId);
                                
                                Configuration.DefaultLogger.Log(string.Format("Pulse timeout for client: {0};  Last pulse received @ {1}", clientId, lastPulse)); 
                            });
                    }
                    catch (Exception ex)
                    {
                    }
                };


            StartPulseTimer();
        }

        protected ITransportManager TransportManager
        {
            get { return _transportManager; }
        }

        protected Configuration Configuration
        {
            get { return _configuration; }
        }

        protected ConcurrentDictionary<string, DateTime> Pulses
        {
            get { return _pulses; }
        }

        public virtual IServerTransporter CreateTransporter(ITransportManager manager, Configuration config)
        {
            return null;
        }

        public virtual bool InterestedIn(string msgUri)
        {
            return _transportManager.CurrentSubscribers.Where(s => s.GetType() == typeof (TSub)).Any(s2 => s2.Uri == msgUri);
        }

        public virtual void Initialize()
        {
            throw new NotImplementedException();
        }

        public virtual void RePublish(string channel, EsbMessage message)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Client evictions;  Occures when the server decides that the client needs to be cleaned up.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="client"></param>
        protected virtual void EvictClient(string clientId = null, Client client = null)
        {
            var cId = client == null ? clientId : client.Id;

            if (string.IsNullOrEmpty(cId))
            {
                Configuration.DefaultLogger.LogError("Evict called with invalid arguments; No valid client identifier.");
                return;
            }

            var foundClient = TransportManager.CurrentClients.SingleOrDefault(cli => cli.Id == cId);
            if (foundClient != null)
            {
                //log
                TransportManager.CurrentClients.Remove(foundClient);
            }


            var foundSubs = TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof(TSub) && sub.ClientId == cId).ToList();

            foundSubs.ForEach(sub =>
            {
                TransportManager.CurrentSubscribers.Remove(sub);
            });
        }

        public virtual bool Shutdown()
        {
            // clear subs
            var redisSubs = TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof(TSub)).ToList();
            var urisRemoved = new List<string>();

            redisSubs.ForEach(sub =>
            {
                TransportManager.RemoveSubscriber(this, sub);
                urisRemoved.Add(sub.Uri);
            });

            return true;
        }

        public virtual string Name { get; protected set; }

        public virtual string Host { get; protected set; }

        public virtual int Port { get; protected set; }

        public virtual void HandleSubscribe(IServerTransporter srcTransporter, ISubscriber subscriber)
        {
        }

        public virtual void HandleUnSubscribe(IServerTransporter srcTransporter, ISubscriber subscriber)
        {
        }

        public virtual bool Enabled { get; set; }

        public virtual void Configure(ITransportConfig transportConfig)
        {
        }

        public virtual void RePlay(string channel, EsbMessage message)
        {
            throw new NotImplementedException();
        }

        public virtual void HandleClientShutdown(IServerTransporter srcTransporter, string clientId)
        {
        }

        private void StartPulseTimer()
        {
            //start timer to check the pulses of the clients
            var pulseTimer = new System.Timers.Timer(TimeSpan.FromSeconds(60).TotalMilliseconds); // fire once every 60 seconds

            pulseTimer.Elapsed += (sender, args) =>
            {
                pulseTimer.Stop();

                if (PulseHandler != null)
                    PulseHandler.Invoke(DateTime.UtcNow);
                
                pulseTimer.Start();
            };

            pulseTimer.Start();
        }

        protected Action<DateTime> PulseHandler { get; set; }

       

        protected virtual void UpdatePulse(Pulse pulse)
        {
            var now = DateTime.UtcNow;
            Pulses.AddOrUpdate(pulse.ClientId, now, (key, oldTime) => now);

            lock (_updatePulseLock)
            {
                var updateClients = _transportManager.CurrentClients.Where(c => c.Id == pulse.ClientId).ToList();
                updateClients.ForEach(c => c.LastPulse = now);
            }
        }
    }
}