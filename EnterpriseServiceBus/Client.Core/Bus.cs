using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using GeoDecisions.Esb.Client.Core.Storage;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using ServiceStack.Text;

//TODO: using Esb.Common.Service.Interfaces;

namespace GeoDecisions.Esb.Client.Core
{
    public class Bus
    {
        private readonly AutoResetEvent _wait = new AutoResetEvent(false);
        private readonly ComManager _comManager;
        private readonly Configuration _configuration;

        static Bus()
        {
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.EmitCamelCaseNames = true;
            JsConfig.IncludeTypeInfo = false;
        }

        //public Bus() // only to satify the Servicehost for testing
        //{
        //}

        public Bus(Configuration config)
        {
            if (!config.IsConfigured)
            {
                config.DefaultLogger.Log("Unable to load configuration.  Please make sure the Esb server is running @" + config.EsbServerIp + ":" + config.EsbServerPort);
                throw new Exception("Bus configuration error.");
            }

            //Configure the ComHandlers
            config.DefaultLogger.Log("Configure the priorities of the ComHandlers");

            //priorties (this only gets used if they don't specify something explicitly)...
            config.ConfigureComHandlers(ch =>
            {
                if (ch.Name == Names.RedisComHandler)
                {
                    ch.Priority = 1;
                }
                else if (ch.Name == Names.WcfTcpComHandler)
                {
                    ch.Priority = 2;
                }
                else if (ch.Name == Names.HttpComHandler)
                {
                    ch.Priority = 3;
                }
                config.DefaultLogger.Log(ch.Name + " configured to priority " + ch.Priority);
            });

            config.DefaultLogger.Log("Initializing ComManager");
            _comManager = new ComManager();
            _comManager.Init(config);

            _configuration = config;

            //This is where the control will come the client connects to the bus, check for failed messages here and redispatch them
            if (_configuration.AutoResendFailedMessages)
            {
                //Publish the message
                _configuration.DefaultLogger.Log("AutoResending failed messages");
                List<EsbMessage> failedMessages = ClientStorageManager.GetFailedMessages();
                if ( failedMessages != null )
                {
                    foreach (EsbMessage msg in failedMessages)
                    {
                        _configuration.DefaultLogger.Log("Resending message:" + msg.Id + ", publishedDateTime:" +
                                                         msg.PublishedDateTime);
                        string success = _comManager.DispatchMessage(msg);
                        if (success == "ok")
                        {
                            ClientStorageManager.DeleteFailedMessage(msg);
                        }
                        else
                        {
                            _configuration.DefaultLogger.Log("Resend Failed:" + msg.Id + ", publishedDateTime:" +
                                                             msg.PublishedDateTime);
                        }
                    }
                }
            }
        }


        private void Init()
        {
            // check for persistant subs
        }

        /// <summary>
        /// Does the publishing of a message.  It is non-blocking.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The POCO object to publish</param>
        /// <param name="uri">Uri of the channel to publish the message to.  This is required if the object does not inherit from IEsbHeader and properly set the Uri in the message.</param>
        /// <param name="asyncCallback">Not implemented in v1</param>
        /// <returns>A simple status message.</returns>
        public string Publish<T>(T message, string uri = null, Action<T> asyncCallback = null)
        {
            if (!_configuration.IsConfigured)
                return "not_config";

            var msgUri = uri;

            // we need to serialize the message ourselves, send as payload
            var memStream = new MemoryStream();
            JsonSerializer.SerializeToStream(message, typeof(T), memStream);
            memStream.Position = 0;

            var pub = new Publisher();
            pub.MachineName = Environment.MachineName;

            // We wrap it for transport.
            var esbMessage = new EsbMessage
            {
                Id = Guid.NewGuid().ToString(),
                Uri = msgUri,
                Type = typeof(T).FullName,
                PublishedDateTime = DateTime.Now,
                Publisher = pub,
                Payload = memStream.GetBuffer()
            };

            if (message is IEsbHeader)
            {
                var esbHeader = message as IEsbHeader;
                msgUri = msgUri ?? esbHeader.Uri; // select uri from either message or explicit

                esbMessage.Priority = esbHeader.Priority;
                //esbMsg.ReplyTo = esbHeader.ReplyTo;
                esbMessage.RetryAttempts = esbHeader.RetryAttempts;
            }

            // check that the uri of the message is not null
            if (string.IsNullOrEmpty(msgUri))
                throw new Exception("Message uri cannot be determined.  Either call Publish with the Uri, or specify it in the class by implementing the IEsbHeader interface or extending the EsbHeader abstract class.");

            _configuration.DefaultLogger.Log("Publish from:" + pub.MachineName + ", Message:" + esbMessage.Uri + ", Type:" + esbMessage.Type);

            string result = _comManager.DispatchMessage(esbMessage, msgUri);

            return result;
        }

        /// <summary>
        /// This does the subscribing to messages.  You can subscribe to a uri more than once, and specify a different callback for each, although this is not recommeneded.  A call to Unsubscribe to a specific Uri will remove all subscriptions at this time.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The Uri of the channel to listen to.</param>
        /// <param name="asyncCallback">This is the callback that is called when a message is published to the subscribed channel uri.  It expects two parameters, the custom POCO type, and the raw ESB transport message.  In case the POCO object fails to hydrate, you can still inspect the transport message.</param>
        /// <param name="persistant">Unused for v1</param>
        /// <returns>A subscription id, on success.</returns>
        public string Subscribe<T>(string uri, Action<T, EsbMessage> asyncCallback, bool persistant = false)
        {
            if (!_configuration.IsConfigured)
                return "not-configured";

            // should we generate an id for this subscription?
            string subId = Guid.NewGuid().ToString();

            //how to hook callbacks.  Need to map them locally and threaded
            if (asyncCallback == null)
                throw new Exception("");

            // check to see if we already have a subscription to that URI, if we do, just hook it?

            var subTask = new Task(() =>
                {
                    var subMsg = new Subscribe(uri, Environment.MachineName, _configuration.Id)
                        {
                            //Uri = _configuration.PermSubUri + "/bus",
                            SubscriptionType = persistant ? SubscriptionTypes.Persistant : SubscriptionTypes.Temporary
                            //Action = asyncCallback
                        };

                    // send subscribe message to server
                    _configuration.DefaultLogger.Log("Sending subscribe message to server:" + subMsg.Uri + ", Type:" + subMsg.Type);
                    Subscription subscription = _comManager.DispatchSubscribe(subMsg, asyncCallback);
                    Console.WriteLine("Sub done");
                });

            subTask.Start();

            return subId;
        }

        public string Replay<T>(string uri, double nDays, Action<List<T>, List<EsbMessage>> callback)
        {
            _comManager.Replay(uri, nDays, callback);
            return "success";
        }

        /// <summary>
        /// Unsubscribes all subscriptions from the specified uri channel.  If you have multiple subscriptions to the uri, they are all removed.
        /// </summary>
        /// <param name="uri"></param>
        public void UnSubscribe(string uri)
        {
            var unsub = new UnSubscribe
                {
                    Uri = uri,
                    MachineName = Environment.MachineName,
                    ClientId = _configuration.Id
                };

            _comManager.DispatchUnSubscribe<UnSubscribe>(unsub);
        }


        //public void RegisterType(Type typeInfo)
        //{
        //    //IMetadataService channel = _mdChannelFactory.CreateChannel(_mdEndpoint);

        //    //var typeMd = new MessageMetadata
        //    //    {
        //    //        Author = "SDDC",
        //    //        Uri = "SomeMilYype"
        //    //    };

        //    //string data = JsonSerializer.SerializeToString(typeMd, typeof (MessageMetadata));

        //    //string result = channel.RegisterType(typeInfo.FullName, data);

        //    //Console.WriteLine(result);
        //}

        //public MessageMetadata GetTypeMetadata(Type typeInfo)
        //{
        //    //IMetadataService channel = _mdChannelFactory.CreateChannel(_mdEndpoint);

        //    //var typeMd = new MessageMetadata {};
        //    //string data = JsonSerializer.SerializeToString(typeMd, typeof (MessageMetadata));

        //    //MessageMetadata result = channel.GetTypeInfo(typeInfo.FullName);

        //    //Console.WriteLine(result);

        //    //return result;

        //    return null;
        //}

        //public string GetAvailableMessages<T>(Expression<Func<T, bool>> filter) where T : MessageMetadata
        //{
        //    return "";
        //}

        //public string GetAvailableMessages2<T>(ConditionalExpression filter) where T : MessageMetadata
        //{
        //    return "";
        //}

   
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<EsbMessage> GetFailedMessages()
        {
            List<EsbMessage> messages = ClientStorageManager.GetFailedMessages();
            return messages;
        }
        
        /// <summary>
        /// This is a temportary method; 
        /// </summary>
        [Obsolete("Please don't use this in code (for testing only).")]
        public void ComHandlerPriorities()
        {
            _comManager.ComHandlerPriorities();
        }

        /// <summary>
        /// This stops the bus instance from listening to all subscriptions, cleans up any queued messages, and other clean up duties.
        /// </summary>
        public void Shutdown()
        {
            // shutdown the client
            _comManager.Shutdown();

            //TODO: implement reset
            //_configuration.Reset();

            _wait.Set();
        }

        /// <summary>
        /// Waits for the Shutdown() method to be called.  This is handy for controlling program flow.
        /// </summary>
        public void WaitForShutdown()
        {
            _wait.WaitOne();
        }

        /// <summary>
        /// Method will block and wait until it gets a message on the specified channel uri.
        /// </summary>
        /// <typeparam name="T">POCO message type</typeparam>
        /// <param name="uri">Channel uri to listen on</param>
        /// <param name="then">Callback action; can be null.</param>
        public void WaitFor<T>(string uri, Action<T> then = null)
        {
            var waitFor = new AutoResetEvent(false);

            this.Subscribe<T>(uri, (arg1, message) =>
                {
                    if (then != null)
                        then(arg1);

                    waitFor.Set();
                });

            waitFor.WaitOne();
        }

    }
}
