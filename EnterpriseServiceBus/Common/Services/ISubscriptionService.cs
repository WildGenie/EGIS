using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using GeoDecisions.Esb.Common.Messages;

namespace GeoDecisions.Esb.Common.Services
{
    //[ServiceContract]
    //public interface ISubscriptionService
    //{
    //    [OperationContract]
    //    Subscription Subscribe(Subscribe subscribe);

    //    [OperationContract]
    //    string UnSubscribe(string msgId);
    //}


    [ServiceContract]
    public interface ISubscriptionRestService
    {
        // What info do we need for a subscription??
        [OperationContract]
        [WebInvoke(UriTemplate = "/{metadataType}/{metadataItem}/{msgCategory}/{msgName}/subscribe")]
        string Subscribe(string msgId, Stream data);

        [OperationContract]
        [WebInvoke(UriTemplate = "subscribe")]
        string Subscribe(Subscribe subscribe, Stream data);

        [OperationContract]
        [WebGet(UriTemplate = "unsubscribe")]
        string UnSubscribe(string msgId);
    }
}