using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Server.Core.Interfaces;

//using Esb.Common.Interfaces.Messages;

namespace GeoDecisions.Esb.Server.Core.Services
{
    internal class BusContextBehavior
    {
    }

    public class BusContext : IExtension<OperationContext>
    {
        private static readonly SynchronizedCollection<BusContext> _busContexts;
        //private string _id;

        static BusContext()
        {
            _busContexts = new SynchronizedCollection<BusContext>();
        }

        //private BusContext()
        //{

        //}

        //The "current" custom context
        public static BusContext Current
        {
            get
            {
                if (OperationContext.Current != null)
                    return OperationContext.Current.Extensions.Find<BusContext>();
                else
                {
                }
                return null;
            }
        }

        //You can have lots more of these -- this is the stuff that you
        //want to store on your custom context
        //public string Id
        //{
        //    get { return _id; }
        //}

        //public string Func<string>

        public string AuthToken { get; set; }

        public EsbMessage EsbMessage { get; set; }

        internal ITransportManager TransportManager { get; set; }

        public string RemoteIp { get; set; }

        public static BusContext Create(string contextId, Dictionary<string, object> optionalParams = null)
        {
            var instance = new BusContext();
            //instance = 
            _busContexts.Add(instance);

            return instance;
        }

        #region IExtension<OperationContext> Members

        public void Attach(OperationContext owner)
        {
            //no-op
        }

        public void Detach(OperationContext owner)
        {
            //no-op
        }

        #endregion
    }


    internal class ParamInsp : IParameterInspector
    {
        //private static SynchronizedCollection<BusContext> _contexts;
        private readonly ITransportManager _transportManager;

        public ParamInsp()
        {
        }

        public ParamInsp(ITransportManager transportManager)
        {
            _transportManager = transportManager;
        }

        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
            if (OperationContext.Current != null)
                OperationContext.Current.Extensions.Remove(BusContext.Current);

            Console.WriteLine("IParameterInspector.AfterCall called for {0}.", operationName);
        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            var context = new BusContext();

            if (inputs.Length == 1)
            {
                // check for post?
                if (OperationContext.Current != null && OperationContext.Current.RequestContext != null)
                {
                    MessageHeaders headers = OperationContext.Current.IncomingMessageHeaders;

                    context.RemoteIp = ParseRequestIp();
                }

                object input1 = inputs.FirstOrDefault();

                if (input1 is Message)
                {
                }

                context.EsbMessage = new EsbMessage();
            }
            // setup our context for this call
            Console.WriteLine("IParameterInspector.BeforeCall called for {0}.", operationName);

            context.TransportManager = _transportManager;

            if (OperationContext.Current != null)
                OperationContext.Current.Extensions.Add(context);


            //_contexts.Add(new BusContext());
            return null;
        }


        public string ParseRequestIp()
        {
            OperationContext context = OperationContext.Current;
            MessageProperties prop = context.IncomingMessageProperties;
            var endpoint = prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            string ip = endpoint.Address;

            return ip;
        }
    }

    public class BusServiceContextBehavior : IServiceBehavior
    {
        private readonly ITransportManager _transportManager;
        //private SynchronizedCollection<BusContext> _contexts;

        public BusServiceContextBehavior()
        {
        }

        public BusServiceContextBehavior(ITransportManager transportManager)
        {
            _transportManager = transportManager;
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints)
                {
                    //endpointDispatcher.DispatchRuntime.MessageInspectors.Add()

                    foreach (DispatchOperation dispatchOperation in endpointDispatcher.DispatchRuntime.Operations)
                    {
                        dispatchOperation.ParameterInspectors.Add(new ParamInsp(_transportManager));
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            return;
        }
    }

    //public class OperationProfilerEndpointBehavior : IEndpointBehavior
    //{
    //    private OperationProfilerManager manager;
    //    public OperationProfilerEndpointBehavior(OperationProfilerManager manager)
    //    {
    //        this.manager = manager;
    //    }

    //    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    //    {
    //    }

    //    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
    //    {
    //        foreach (ClientOperation operation in clientRuntime.Operations)
    //        {
    //            operation.ParameterInspectors.Add(new OperationProfilerParameterInspector(this.manager, operation.IsOneWay));
    //        }
    //    }

    //    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
    //    {
    //        foreach (DispatchOperation operation in endpointDispatcher.DispatchRuntime.Operations)
    //        {
    //            operation.ParameterInspectors.Add(new OperationProfilerParameterInspector(this.manager, operation.IsOneWay));
    //        }
    //    }

    //    public void Validate(ServiceEndpoint endpoint)
    //    {
    //    }
    //}
}