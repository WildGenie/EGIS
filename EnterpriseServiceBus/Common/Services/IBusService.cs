using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Messages.Configuration;

namespace GeoDecisions.Esb.Common.Services
{
    /// <summary>
    ///     interface for bus service
    /// </summary>
    [ServiceContract(CallbackContract = typeof (IBusServiceCallback))]
    public interface IBusService
    {
        [OperationContract(IsOneWay = true)]
        void Publish(EsbMessage message);

        //Defining a Generic Type does not work, this errors out when the channel is created from the WcfTcpComHandler ??
        //The conversion to List<T> is done in the WcftcpComHandler's Replay method
        //[OperationContract]
        //List<T> GetCustomMessages<T>(string uri, DateTime dateTime);

        [OperationContract]
        List<EsbMessage> GetMessages(string uri, DateTime dateTime);

        [OperationContract]
        Subscription Subscribe(Subscribe subscribe);

        [OperationContract]
        string UnSubscribe(UnSubscribe unSubscribe);

        [OperationContract]
        void ClientShutdown(UnSubscribe unSubscribe);

        [OperationContract]
        void Pulse(Pulse pulse);

        [OperationContract]
        void Announce(ClientAnnounce client);
    }

    [ServiceContract]
    public interface IBusServiceCallback
    {
        [OperationContract]
        string Receiver(EsbMessage message);
    }

    /// <summary>
    ///     REST interface for bus service
    /// </summary>
    [ServiceContract]
    public interface IBusRestService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "/publish")]
        string Publish(Stream data);

        [WebInvoke(UriTemplate = "/subscribe?uri={uri}")]
        string Subscribe(string uri);
    }

    // need a Config object to send.

    /// <summary>
    ///     WCF TCP interface for bus config service
    /// </summary>
    [ServiceContract]
    internal interface IBusConfigService
    {
        [OperationContract]
        ServerConfigMessage GetConfiguration();
    }

    /// <summary>
    ///     REST interface for bus service
    /// </summary>
    [ServiceContract]
    public interface IBusConfigRestService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/config")]
        Stream GetConfiguration();
    }

    /// <summary>
    /// WCF TCP interface for MessageService, retrieve and replay messages that have been already published
    /// </summary>
    [ServiceContract]
    public interface IBusMessageService
    {
        [OperationContract]
        List<EsbMessage> GetMessages(string uri, DateTime dateTime);
    }

    /// <summary>
    /// REST interface for MessageService, retrieve and replay messages that have already been published
    /// </summary>
    [ServiceContract]
    internal interface IBusMessageRestService
    {
        [OperationContract]
        [WebGet(UriTemplate ="/messages?uri={uri}&dateTime={dateTime}")]
        List<EsbMessage> GetMessages(string uri, DateTime dateTime);
    }
}