using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Services;

namespace GeoDecisions.Esb.Server.Core.Services
{
    public static class SubMgr
    {
        public static List<IBusServiceCallback> subscribers = new List<IBusServiceCallback>();
    }

    internal class SubscriptionService //: ISubscriptionService
    {
        // for perm subscriptions, we need to store them, and actually create a channel to the remoteendpoint(client)
        //perm sub test

        public string UnSubscribe(string msgId)
        {
            throw new NotImplementedException();
        }

        public Subscription Subscribe(Subscribe subscribe)
        {
            // perm or not?
            if (subscribe.SubscriptionType == SubscriptionTypes.Persistant)
            {
                // store sub info in db
                //var client = new RedisClient();
                //client.SetEntryInHash("SubInfo", "", Subscribe.Dump());
                //client.GetValueFromHash("SubInfo", "");

                Binding binding = GetBindingFromAddress(subscribe.Uri);
                IBusServiceCallback proxy = ChannelFactory<IBusServiceCallback>.CreateChannel(binding, new EndpointAddress(subscribe.Uri));
                SubMgr.subscribers.Add(proxy);
                return new Subscription {Status = "ok"};
                //return "permanent:ok";
            }

            if (subscribe.SubscriptionType == SubscriptionTypes.Temporary)
            {
                var busCallback = OperationContext.Current.GetCallbackChannel<IBusServiceCallback>();
                SubMgr.subscribers.Add(busCallback);
                return new Subscription {Status = "ok"};
                //return "temporary:ok";
            }

            return new Subscription {Status = "error"};
            //return "error";
        }

        public string Subscribe(string msgId)
        {
            // get client address
            //var requestUri = OperationContext.Current.Channel.RemoteAddress.Uri;
            string addy = msgId; // requestUri.ToString();
            Binding binding = GetBindingFromAddress(addy);

            IBusServiceCallback proxy = ChannelFactory<IBusServiceCallback>.CreateChannel(binding, new EndpointAddress(addy));

            SubMgr.subscribers.Add(proxy);

            return "ok";
        }

        private static Binding GetBindingFromAddress(string address)
        {
            if (address.StartsWith("http:") || address.StartsWith("https:"))
            {
                var binding = new WSHttpBinding(SecurityMode.None, true);
                binding.ReliableSession.Enabled = true;
                binding.TransactionFlow = true;
                return binding;
            }
            if (address.StartsWith("net.tcp:"))
            {
                var binding = new NetTcpBinding(SecurityMode.None, true);
                binding.ReliableSession.Enabled = true;
                binding.TransactionFlow = true;
                return binding;
            }
            if (address.StartsWith("net.pipe:"))
            {
                var binding = new NetNamedPipeBinding();
                binding.TransactionFlow = true;
                return binding;
            }
            if (address.StartsWith("net.msmq:"))
            {
                var binding = new NetMsmqBinding();
                binding.Security.Mode = NetMsmqSecurityMode.None;
                return binding;
            }
            Debug.Assert(false, "Unsupported protocol specified");
            return null;
        }
    }
}