using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BookSleeve;
using GeoDecisions.Esb.Client.Core.Interfaces;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Client.Core.ComHandlers
{
    internal class RedisBsComHandler : IComHandler
    {
        private readonly Configuration _configuration;
        private readonly RedisConnection _redisConnection;
        private readonly RedisSubscriberConnection _redisSubConnection;
        private Timer _pulseTimer;
        private bool _wasConnected;
        private bool _triedConnect;

        private RedisBsComHandler(Configuration configuration)
        {
            _configuration = configuration;

            _redisSubConnection = new RedisSubscriberConnection(_configuration.RedisHost, _configuration.RedisPort);
            _redisConnection = new RedisConnection(_configuration.RedisHost, _configuration.RedisPort);
        }

        public IComHandler CreateHandler(Configuration config)
        {
            return new RedisBsComHandler(config);
        }

        public bool Connect()
        {
            try
            {
                if (_redisSubConnection.State == RedisConnectionBase.ConnectionState.Open && _redisConnection.State == RedisConnectionBase.ConnectionState.Open)
                    return true;

                if (_triedConnect || _wasConnected) // if it was connected; always return false TODO: we need to make this a little less one-and-done...
                    return false;


                _triedConnect = true;

                _redisSubConnection.Open().Wait();

                _redisConnection.Open().Wait();

                if (_redisSubConnection.State == RedisConnectionBase.ConnectionState.Open && _redisConnection.State == RedisConnectionBase.ConnectionState.Open)
                {
                    // Announce
                    Announce();

                    // Start pulse
                    StartPulse();

                    _wasConnected = true;
                    return true;
                }

            }
            catch (Exception ex)
            {
                _configuration.DefaultLogger.LogError(ex.Dump());
            }

            return false;
        }


        public int Priority { get; set; }

        public IComHandler Successor { get; set; }

        public string Name
        {
            get { return Names.RedisComHandler; }
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
            string json = message.ToJson();
            _redisConnection.Publish(Names.RedisPublishCh, json);
            return "ok";
        }

        public void DispatchSubscribe<T>(Subscribe subscribe, Action<T, EsbMessage> callback)
        {
            // add subscription
            _redisSubConnection.Subscribe("esb://" + subscribe.Uri, (s, bytes) =>
                {
                    Type messageType = typeof (T);
                    string esbString = Encoding.UTF8.GetString(bytes);

                    var esbMessage = esbString.FromJson<EsbMessage>();

                    if (messageType == typeof (string))
                    {
                        string payload = Encoding.UTF8.GetString(esbMessage.Payload);
                        callback((T) (object) payload, esbMessage);
                    }
                    else if (messageType == typeof (EsbMessage))
                    {
                        callback((T) (object) esbMessage, esbMessage);
                    }
                    else
                    {
                        try
                        {
                            var hydrated = Encoding.UTF8.GetString(esbMessage.Payload).FromJson<T>();
                            callback(hydrated, esbMessage);
                        }
                        catch (Exception ex)
                        {
                            callback((T) (object) null, esbMessage);
                        }
                    }
                });

            string jsonMsg = subscribe.ToJson();
            // send message over sub channel
            _configuration.DefaultLogger.Log("DispatchSubscribe from comHandler:" + Name + ", SubscribeMsg:" + subscribe.Uri + ", Type:" + subscribe.Type + ", MachineName:" + subscribe.MachineName + ", ClientId:" + subscribe.ClientId);
            _redisConnection.Publish(Names.REDIS_SUB_CHANNEL, jsonMsg);
        }

        public void DispatchUnSubscribe(UnSubscribe unSubscribe)
        {
            string json = unSubscribe.ToJson();
            _redisConnection.Publish(Names.REDIS_UNSUB_CHANNEL, json);
            _redisSubConnection.Unsubscribe("esb://" + unSubscribe.Uri);
        }

        public bool Shutdown()
        {
            StopPulse();

            // we want to send a shutdown message to the server to let it know we are audi.
            var json = new UnSubscribe() {ClientId = this._configuration.Id, MachineName = Environment.MachineName}.ToJson();

            if (_redisConnection.State == RedisConnectionBase.ConnectionState.Open)
            {
                _redisConnection.Publish(Names.RedisShutdownCh, json);

                // we can shut down all our stuff now...
                var tasks = new[]
                    {
                        _redisSubConnection.CloseAsync(false),
                        _redisConnection.CloseAsync(false)
                    };

                Task.WaitAll(tasks);
            }
            return true;
        }

        public void Replay<T>(string uri, double dDays, Action<List<T>, List<EsbMessage>> callback)
        {

            throw new NotImplementedException();

            //_redisSubConnection.Subscribe("esb://" + Names.REPLAY_CHANNEL_PREFIX + uri, (s, bytes) =>
            //{
            //    Type messageType = typeof(T);

            //    string esbString = Encoding.UTF8.GetString(bytes);

            //    var esbMessage = esbString.FromJson<List<EsbMessage>>();

            //    if (messageType == typeof(string))
            //    {
            //        //string payload = Encoding.UTF8.GetString(esbMessage.Payload);
            //        callback((List<T>)((object)esbMessage.ToList()), esbMessage);
            //    }
            //    else 
            //    if (messageType == typeof(EsbMessage))
            //    {
            //        callback((List<T>) ((object)esbMessage.ToList()), esbMessage);
            //    }
            //    else
            //    {
            //        try
            //        {
            //            esbString = Encoding.UTF8.GetString(bytes);
            //            var hydrated = Encoding.UTF8.GetString(bytes).FromJson<List<T>>();
            //            callback(hydrated, esbMessage);
            //        }
            //        catch (Exception ex)
            //        {
            //            callback((List<T>)(object)null, esbMessage);
            //        }
            //    }
            //});


            ////Now that we are subscribed to the Replay-URI, publish the message to the Redis Replay- channel, ie esb://Replay-uri

            //string jsonMsg = subscribe.ToJson();
            //// send message over sub channel
            //_configuration.DefaultLogger.Log("DispatchSubscribe from comHandler:" + Name + ", SubscribeMsg:" + subscribe.Uri + ", Type:" + subscribe.Type + ", MachineName:" + subscribe.MachineName + ", ClientId:" + subscribe.ClientId);
            //_redisConnection.Publish(Names.REDIS_SUB_CHANNEL, jsonMsg);
        }

        private void StartPulse()
        {
            var json = new Pulse { ClientId = this._configuration.Id }.ToJson();
            _redisConnection.Publish(Names.RedisPulseCh, json);

            if (_pulseTimer == null)
                _pulseTimer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds); // fire once every 60 seconds

            _pulseTimer.Elapsed += (sender, args) =>
            {
                _pulseTimer.Stop();

                _redisConnection.Publish(Names.RedisPulseCh, json);

                _pulseTimer.Start();
            };

            _pulseTimer.Start();
        }

        private void StopPulse()
        {
            if (_pulseTimer != null)
                _pulseTimer.Stop();
        }


        private void Announce()
        {
            var json = new ClientAnnounce()
                {
                    Id = this._configuration.Id,
                    Name = "Redis default",
                    Description = "Redis Default description"
                }.ToJson();

            _redisConnection.Publish(Names.RedisAnnounceCh, json);
        }
    }
}