using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoDecisions.Esb.Client.Core.Interfaces;
using GeoDecisions.Esb.Client.Core.Storage;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Utility.Plugins;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Client.Core
{
    /// <summary>
    ///     Manager for message dispatching
    /// </summary>
    internal class ComManager
    {
        private List<IComHandler> _comHandlers;
        private Configuration _configuration;
        private ILogger _logger;

        public Action<IComHandler, string> BeforeDispatch { get; set; }
        public Action<IComHandler, string, string> AfterDispatch { get; set; }

        public void Init(Configuration config)
        {
            _configuration = config;

            _logger = config.DefaultLogger;

            _logger.Log("Initializing Comhandlers");

            _comHandlers = new List<IComHandler>();

            IEnumerable<IComHandler> foundComHandlers = PluginManager.GetAll<IComHandler>(_configuration);

            _configuration.ComHandlers.AddRange(foundComHandlers);

            //This will call the <Action> that is setup in the program.main for each of the ComHandlers
            _configuration.ConfigureComHandlers();

            _comHandlers.AddRange(_configuration.ComHandlers);

            // order by priortiy
            _comHandlers = _comHandlers.OrderBy(md => md.Priority).ToList();

            // set successors
            _comHandlers.ForEach(md =>
                {
                    int nextIdx = _comHandlers.IndexOf(md) + 1;

                    if (_comHandlers.Count > nextIdx)
                        md.Successor = _comHandlers[nextIdx];
                });

            // call connect on first comhandler
            InitConnect(_comHandlers.FirstOrDefault());

            var task = new Task(() => { ClientStorageManager.Init(config); });
            task.Start();

            _logger.Log("Initializing Comhandlers done");
        }

        private void InitConnect(IComHandler handler)
        {
            if (handler == null)
                return;

            if (!handler.Connect())
            {
                InitConnect(handler.Successor);
            }
        }


        public void ComHandlerPriorities()
        {
            foreach (IComHandler com in _comHandlers)
            {
                Console.WriteLine("Name=" + com.Name + ", Priority=" + com.Priority);
            }
        }

        public string DispatchMessage(EsbMessage message, string uri = null)
        {
            // find appropriate com handler

            // take first to handle
            var dispatchRef = _comHandlers.FirstOrDefault(md =>
                {
                    try
                    {
                        return md.CanHandle(message);
                    }
                    catch (Exception ex)
                    {
                        //TODO: log
                    }
                    return false;
                });

            //if (BeforeDispatch != null)
            //    BeforeDispatch.Invoke(dispatchRef, message);

            if (dispatchRef != null)
            {
                try
                {
                    _configuration.DefaultLogger.Log("DispatchMessage:" + message.Uri + ", Type:" + message.Type + ", Handler:" + dispatchRef.Name);
                    string success = DispatchInternal(message, dispatchRef);
                    if (success != "ok")
                    {
                        _logger.Log(success);
                        ClientStorageManager.SaveFailedMessage(message);
                    }
                    else
                    {
                        return success;
                    }
                }
                catch (Exception ex) // this is last error handler for dispatch
                {
                    _logger.Log(ex.Dump());
                    ClientStorageManager.SaveFailedMessage(message);
                }
            }

            return "dispatcher-not-found";
        }

        /// <summary>
        ///     Does the dispatching.  Recursively defaults to successors
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        private string DispatchInternal(EsbMessage message, IComHandler comHandler, string uri = null)
        {
            if (comHandler != null)
            {
                try
                {
                    if (comHandler.Connect())
                    {
                        _configuration.DefaultLogger.Log("DispatchInternal:" + message.Uri + ", Type:" + message.Type + ", Handler:" + comHandler.Name);
                        string result = comHandler.DispatchMessage(message, uri);
                        return result;
                    }

                    // try successors
                    return DispatchInternal(message, comHandler.Successor, uri);

                    //if (AfterDispatch != null)
                    //    AfterDispatch.Invoke(comHandler, message, result);
                }
                catch (Exception ex)
                {
                    // log
                    _logger.Log(ex.Dump());

                    // try successors
                    return DispatchInternal(message, comHandler.Successor, uri);
                }
            }

            return "dispatching-error-ocurred";
        }

        private string DispatchSubscribeMessage<T>(Subscribe subscribeMsg, Action<T, EsbMessage> callback, IComHandler comHandler)
        {
            // find appropriate MessageDispatcher
            string json = subscribeMsg.ToJson();

            // take first to handle
            //if (comHandler == null)
            //    return "";

            //if (BeforeDispatch != null)
            //    BeforeDispatch.Invoke(comHandler, message);

            if (comHandler != null)
            {
                try
                {
                    if (comHandler.Connect())
                    {
                        _configuration.DefaultLogger.Log("DispatchSubscribeMessage from comManager:" + comHandler.Name + ", SubscribeMsg:" + subscribeMsg.Uri + ", Type:" + subscribeMsg.Type + ", MachineName:" + subscribeMsg.MachineName + ", ClientId:" + subscribeMsg.ClientId);
                        comHandler.DispatchSubscribe(subscribeMsg, callback);
                        return "ok";
                    }

                    return DispatchSubscribeMessage(subscribeMsg, callback, comHandler.Successor);
                }
                catch (Exception ex) // this is last error handler for dispatch
                {
                    _logger.Log(ex.Dump());
                    return DispatchSubscribeMessage(subscribeMsg, callback, comHandler.Successor);
                }
            }

            return "dispatcher-not-found";
        }


        internal Subscription DispatchSubscribe<T>(Subscribe subscribeMsg, Action<T, EsbMessage> callback)
        {
            var comHandler = _comHandlers.FirstOrDefault(md =>
                {
                    try
                    {
                        return md.CanHandle(subscribeMsg);
                    }
                    catch (Exception ex)
                    {
                        //log
                    }
                    return false;
                });

            if (comHandler != null)
            {
                _configuration.DefaultLogger.Log("Dispatching subscribe message from comManager:" + comHandler.Name + ", SubscribeMsg:" + subscribeMsg.Uri + ", Type:" + subscribeMsg.Type + ", MachineName:" + subscribeMsg.MachineName + ", ClientId:" + subscribeMsg.ClientId);
                DispatchSubscribeMessage(subscribeMsg, callback, comHandler);
            }
            else
            {
                _configuration.DefaultLogger.Log("ComHandler is null, Subscription is not dispatched, SubscribeMsg:" + subscribeMsg.Uri + ", Type:" + subscribeMsg.Type);
            }

            return new Subscription {Status = "ok", Uri = subscribeMsg.Uri};
        }

        internal void Replay<T>(string uri, double dDays, Action<List<T>, List<EsbMessage>> callback)
        {
            var comHandler = _comHandlers.FirstOrDefault(com => com.Name == Names.WcfTcpComHandler );
            if (comHandler != null)
            {
                comHandler.Replay(uri, dDays, callback);
            }
        }


        private string DispatchUnSubscribeMessage<T>(UnSubscribe subscribeMsg, Action<T, EsbMessage> callback, IComHandler comHandler)
        {
            if (comHandler != null)
            {
                try
                {
                    if (comHandler.Connect())
                    {
                        //_configuration.DefaultLogger.Log("DispatchUnSubscribeMessage from comManager:" + comHandler.Name + ", SubscribeMsg:" + subscribeMsg.Uri + ", Type:" + subscribeMsg.Type + ", MachineName:" + subscribeMsg.MachineName + ", ClientId:" + subscribeMsg.ClientId);
                        comHandler.DispatchUnSubscribe(subscribeMsg);
                        return "ok";
                    }

                    return DispatchUnSubscribeMessage(subscribeMsg, callback, comHandler.Successor);
                }
                catch (Exception ex) // this is last error handler for dispatch
                {
                    _logger.Log(ex.Dump());
                    return DispatchUnSubscribeMessage(subscribeMsg, callback, comHandler.Successor);
                }
            }

            return "dispatcher-not-found";
        }


        internal Subscription DispatchUnSubscribe<T>(UnSubscribe unSubscribe, Action<T, EsbMessage> callback = null)
        {
            var comHandler = _comHandlers.FirstOrDefault(md =>
                {
                    try
                    {
                        return md.CanHandle(new Subscribe());
                    }
                    catch (Exception ex)
                    {
                        //TODO: log
                    }

                    return false;
                }); // always returns true for Redis....

            if (comHandler != null)
            {
                _configuration.DefaultLogger.Log("Dispatching subscribe message from comManager:" + comHandler.Name + ", SubscribeMsg:" + unSubscribe.Uri + ", Type:" + unSubscribe.Type + ", MachineName:" + unSubscribe.MachineName + ", ClientId:" + unSubscribe.ClientId);
                DispatchUnSubscribeMessage(unSubscribe, callback, comHandler);
            }
            else
            {
                _configuration.DefaultLogger.Log("ComHandler is null, Subscription is not dispatched, SubscribeMsg:" + unSubscribe.Uri + ", Type:" + unSubscribe.Type);
            }

            return new Subscription {Status = "ok", Uri = unSubscribe.Uri};
        }


        internal void Shutdown()
        {
            _comHandlers.ForEach(ch =>
                {
                    // call shutdown on all comhandlers
                    ch.Shutdown();
                });

            _comHandlers.Clear();

            _comHandlers = null;
        }
    }
}
