using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BookSleeve;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Server.Core.Transporters
{
    public class RedisConfig : ITransportConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }


    internal class RedisBsTransporter : Transporter<RedisSubscriber> //IServerTransporter
    {        
        private const string HANDLER_ID = "12gIaw3redis";
        private static readonly object _lock = new object();
        private RedisConnection _redisConnection;
        private RedisSubscriberConnection _redisSubConnection;

        public RedisBsTransporter(ITransportManager transportManager, Configuration configuration) : base(transportManager, configuration)
        {
        }

        public override void Initialize()
        {
            _redisConnection = new RedisConnection(Host, Port); // password: "changeme", syncTimeout: 60000, ioTimeout: 5000);
           
           // _redisConnection.Error += RedisConnectionOnError;
 
            _redisConnection.Open();

            _redisSubConnection = new RedisSubscriberConnection(Host, Port);
            _redisSubConnection.Open();

            // sub/unsub channel handler
            _redisSubConnection.Subscribe(new[]
                {
                    Names.REDIS_SUB_CHANNEL, Names.REDIS_UNSUB_CHANNEL, Names.RedisPublishCh,
                    Names.RedisShutdownCh, Names.RedisPulseCh, Names.RedisAnnounceCh
                }, (subChannel, bytes) =>
                    {
                        switch (subChannel)
                        {
                            case Names.REDIS_SUB_CHANNEL:

                                base.Configuration.DefaultLogger.Log("[redis] : subChannel: " + subChannel);

                                string subMessage = Encoding.UTF8.GetString(bytes);
                                var subMsg = base.Configuration.Serialization.Deserialize<RedisSubscriber>(subMessage); //The sent message is <Subscribe>, but the deserialized message is <RedisSubscriber>, why ??, rxs, 06/14/2013

                                PrivateSubscribe(new RedisSubscriber {Uri = subMsg.Uri, MachineName = subMsg.MachineName, ClientId = subMsg.ClientId, LastHeartbeat = subMsg.LastHeartbeat}); // moved into this shared method

                                break;

                            case Names.REDIS_UNSUB_CHANNEL:

                                //deserialize unsub message
                                var unsubMsg = base.Configuration.Serialization.Deserialize<UnSubscribe>(Encoding.UTF8.GetString(bytes));

                                PrivateUnsubscribe(unsubMsg); // moved into this shared method

                                break;

                            case Names.RedisPublishCh:
                                // publish
                                Publish(bytes);
                                break;

                            case Names.RedisShutdownCh:
                                // client shutdown
                                var unsubClient = base.Configuration.Serialization.Deserialize<UnSubscribe>(Encoding.UTF8.GetString(bytes));
                                PrivateClientShutdown(unsubClient.ClientId);
                                break;

                            case Names.RedisPulseCh:
                                // pulse
                                var now = DateTime.UtcNow;
                                var pulseInfo = base.Configuration.Serialization.Deserialize<Pulse>(Encoding.UTF8.GetString(bytes));
                                //base.Pulses.AddOrUpdate(pulseInfo.ClientId, now, (key, oldTime) => now);
                                base.UpdatePulse(pulseInfo);
                                break;

                            case Names.RedisAnnounceCh:
                                var announceMsg = base.Configuration.Serialization.Deserialize<ClientAnnounce>(Encoding.UTF8.GetString(bytes));
                                base.TransportManager.AddClient(new Client() {Id = announceMsg.Id, Name = announceMsg.Name, Description = announceMsg.Description, LastPulse = DateTime.UtcNow});
                                break;

                            default:
                                throw new NotImplementedException(subChannel);
                        }
                    });
        }

        //private void RedisConnectionOnError(object sender, ErrorEventArgs errorEventArgs)
        //{
        //    Trace.WriteLine(errorEventArgs.Dump());
        //}

        public override void RePublish(string channel, EsbMessage message)
        {
            if (message.HandlerId == HANDLER_ID) // we need a way to skip republishing to the original handler.
                return;

            string json = message.ToJson();

            _redisConnection.Publish("esb://" + channel, json); //TODO verify that this works
        }

        public override bool Shutdown()
        {
            base.Shutdown();  // handles clearing out subscribers

            // close connections

            if (_redisConnection != null && _redisConnection.State == RedisConnectionBase.ConnectionState.Open)
            {
                _redisConnection.Close(true);
                _redisConnection.Dispose();
            }

            if (_redisSubConnection != null && _redisSubConnection.State == RedisConnectionBase.ConnectionState.Open)
            {
                _redisSubConnection.Unsubscribe(new[]
                    {
                        Names.REDIS_SUB_CHANNEL, Names.REDIS_UNSUB_CHANNEL, Names.RedisPublishCh,
                        Names.RedisShutdownCh, Names.RedisPulseCh, Names.RedisAnnounceCh
                    });

                _redisSubConnection.Close(true);
                _redisSubConnection.Dispose();
            }

            _redisConnection = null;
            _redisSubConnection = null;


            return true;
        }

        public override string Name
        {
            get { return Names.RedisTransporter; }
        }

        public override void Configure(ITransportConfig transportConfig)
        {
            var redisConfig = transportConfig as RedisConfig;

            if (redisConfig == null)
                throw new Exception("Redis configuration was invalid.");

            Host = redisConfig.Host;
            Port = redisConfig.Port;
        }

        //Note: To receive a replayed message, client should subscribe with the below string prefixed to the original channel
        public override void RePlay(string channel, EsbMessage message)
        {
            //if (message.HandlerId == HANDLER_ID) // we need a way to skip republishing to the original handler.
            //    return;

            base.Configuration.DefaultLogger.Log("Inside Replay from " + Name);

            string json = message.ToJson();

            _redisConnection.Publish("esb://" + Names.REPLAY_CHANNEL_PREFIX + channel, json);
        }

        private void PrivateSubscribe(ISubscriber subMsg)
        {
            //_configuration.DefaultLogger.Log("[redis] : subChannel: " + subMsg.Uri);

            subMsg.LastHeartbeat = DateTime.Now;

            base.Configuration.DefaultLogger.Log("[redis] subscribe :" + subMsg.Uri + " ," + subMsg.MachineName);

            bool anyoneListening = false;

            lock (_lock)
            {
                anyoneListening = base.TransportManager.CurrentSubscribers.Any(s => s.GetType() == typeof (RedisSubscriber) && s.Uri == subMsg.Uri);

                if (!anyoneListening)
                {
                    //add subscriber to the manager
                    base.TransportManager.AddSubscriber(this, subMsg);
                }
                else
                {
                    bool shouldAddSub = base.TransportManager.CurrentSubscribers.Any(s => s.Uri == subMsg.Uri && (s.MachineName + s.ClientId) != (subMsg.MachineName + subMsg.ClientId));

                    if (shouldAddSub)
                        base.TransportManager.AddSubscriber(this, subMsg);
                }
            }


            if (!anyoneListening)
            {
                //Subscribe
                _redisSubConnection.Subscribe(subMsg.Uri, (channel, msgBytes) =>
                    {
                        string message = Encoding.UTF8.GetString(msgBytes);
                        //1.) decode data -> message
                        var esbMessage = base.Configuration.Serialization.Deserialize<EsbMessage>(message);

                        // add handler id to message
                        esbMessage.HandlerId = HANDLER_ID;

                        //2.) tell others
                        base.TransportManager.RePublish(channel, esbMessage); // republishes message to other transporters

                        // add message to the manager, rxs, 04/12/2013
                        base.TransportManager.AddMessage(channel, esbMessage);

                        // add publisher to the manager, rxs, 04/18/2013
                        base.TransportManager.AddPublisher(esbMessage.Publisher);

                        _redisConnection.Publish("esb://" + channel, message);
                    });
            }
        }

        private void Publish(byte[] msgBytes)
        {
            string message = Encoding.UTF8.GetString(msgBytes);
            //1.) decode data -> message
            var esbMessage = base.Configuration.Serialization.Deserialize<EsbMessage>(message);

            // add handler id to message
            esbMessage.HandlerId = HANDLER_ID;

            //2.) tell others
            base.TransportManager.RePublish(esbMessage.Uri, esbMessage); // republishes message to other transporters

            // add message to the manager, rxs, 04/12/2013
            base.TransportManager.AddMessage(esbMessage.Uri, esbMessage);

            // add publisher to the manager, rxs, 04/18/2013
            base.TransportManager.AddPublisher(esbMessage.Publisher);

            _redisConnection.Publish("esb://" + esbMessage.Uri, message);
        }

        private void PrivateUnsubscribe(UnSubscribe unSubscribe)
        {
            base.Configuration.DefaultLogger.Log("[redis] UnSubscribe :" + unSubscribe.Uri + " ," + unSubscribe.MachineName + ", " + unSubscribe.ClientId);

            var redisSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof(RedisSubscriber));

            //var foundSubs = _redisSubscribers.Where(sub => sub.Uri == unsubMsg.Uri && sub.MachineName == unsubMsg.MachineName).ToList(); // get this specific subscription
           var foundSubs = redisSubs.Where(sub => sub.Uri == unSubscribe.Uri && (sub.MachineName + sub.ClientId) == (unSubscribe.MachineName + unSubscribe.ClientId)).ToList(); // get this specific subscription

            foundSubs.ForEach(sub =>
                {
                    base.TransportManager.RemoveSubscriber(this, sub);
                    //_redisSubscribers.Remove(sub);
                });

            //var subCount = _redisSubscribers.Count(s => s.Uri == unsubMsg.Uri); // count the rest of the subs to the uri only.
            int subCount = redisSubs.Count(s => s.Uri == unSubscribe.Uri);

            if (subCount == 0)
            {
                _redisSubConnection.Unsubscribe("esb://" + unSubscribe.Uri);
            }
        }

        private void PrivateClientShutdown(string clientId)
        {
            base.Configuration.DefaultLogger.Log("[redis] Client shutdown: " + clientId);

            EvictClient(clientId);

            var redisSubs = base.TransportManager.CurrentSubscribers.Where(sub => sub.GetType() == typeof (RedisSubscriber) && sub.ClientId == clientId).ToList();

            var urisRemoved = new List<string>();

            redisSubs.ForEach(sub =>
            {
                base.TransportManager.RemoveSubscriber(this, sub);
                urisRemoved.Add(sub.Uri);
                //_redisSubscribers.Remove(sub);
            });


            // check if any we removed are the last for that uri... I THINK this makes sense.
            urisRemoved.Distinct().ToList().ForEach(uri =>
                {
                    int subCount = redisSubs.Count(s => s.Uri == uri);

                    if (subCount == 0)
                    {
                        _redisSubConnection.Unsubscribe("esb://" + uri);
                    }
                });

        }
    }
}