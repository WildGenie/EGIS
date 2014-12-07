using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using GeoDecisions.Esb.Common.Messages;

namespace GeoDecisions.Esb.Common.Services
{
    [ServiceContract(Name = "metadata")]
    public interface IMetadataService
    {
        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        string SendMeEvery(int seconds);

        [OperationContract]
        [WebGet(UriTemplate = "getMetadata?lastChecked={lastChecked}")]
        string GetMetadata(DateTime lastChecked);

        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Wrapped)]
        string SetMetadata();

        [OperationContract]
        [WebGet]
        MessageMetadata GetTypeInfo(string type);

        [OperationContract]
        [WebInvoke(UriTemplate = "registerType", BodyStyle = WebMessageBodyStyle.Wrapped)]
        string RegisterType(string type, string somethingElse);


        /////////////////////////////////
        //[WebGet(UriTemplate = "{metadataType}/{metadataItem}")]
        //string GetMetadataItem(string metadataType, string metadataItem);

        //[WebGet(UriTemplate = "{metadataType}/{metadataItem}/{msgCategory}/{msgName}")]
        //string GetMessageInfo(string metadataType, string metadataItem, string msgCategory, string msgName);

        //[WebGet(UriTemplate = "{metadataType}/{metadataItem}/{msgCategory}/{msgName}/metadata")]
        //string GetMessageMetadata(string metadataType, string metadataItem, string msgCategory, string msgName);

        //[WebGet(UriTemplate = "{metadataType}/{metadataItem}")]
        //string GetMessagesBy(string metadataType, string metadataItem);
    }


    //[GET]  esb/metadata
    // -> authors, categories, messages, 

    //[GET]  esb/metadata/{metadata}

    //[GET]  esb/messages/authors  // gets list of authors

    //[GET]  esb/messages/authors/{author}/{category} // gets list of authors public categories

    //[POST] esb/messages/authors/{author}/{category}/msgtype?action=subscribe      // subscribe to message
    //[POST] esb/messages/authors/{author}/{category}/msgtype?action=unsubscribe    // unsubscribe to message

    //[POST] esb/messages/authors/{author}/{category}/msgtype?action=subscribe&subtype=persistant      // persistant subscription to message


    //[GET]  esb/messages/categories  // gets list of cats

    //[GET]  esb/messages/categories/{category}/msgName  // gets list of cats
}