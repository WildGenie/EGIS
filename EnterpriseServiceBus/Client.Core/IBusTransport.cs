//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.ServiceModel;
//using System.Text;
//using System.Threading.Tasks;
//using Esb.Common.SharedMessages;

//namespace Esb.Client.Core
//{
//    interface IBusTransport
//    {
//        string CompileMetadata();

//        void RegisterType(Type typeInfo);

//        void ListenFor<T>();
//    }

//    public class BusTransport : IBusTransport
//    {
//        public BusTransport()
//        {
//            var _mdEndpoint = new EndpointAddress("net.tcp://localhost:9001/esb/metadata"); //config.MetadataUrl);
//            var tcpBinding = new NetTcpBinding(SecurityMode.None);
//            var _mdChannelFactory = new ChannelFactory<IMetadataService>(tcpBinding, _mdEndpoint);
//            var channel = _mdChannelFactory.CreateChannel(_mdEndpoint);
//        }

//        public string CompileMetadata()
//        {
//            throw new NotImplementedException();
//        }

//        public void RegisterType(Type typeInfo)
//        {
//            throw new NotImplementedException();
//        }

//        public void ListenFor<T>()
//        {
//            throw new NotImplementedException();
//        }
//    }

//    public class DefaultBusTransport  :  BusTransport, IBusTransport
//    {

//    }
//}

