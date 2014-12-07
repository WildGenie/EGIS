//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using GeoDecisions.Esb.Server.Core.Interfaces;

//namespace GeoDecisions.Esb.Server.Core.Transporters
//{
//    public abstract class ServerTransporterBase : IServerTransporter
//    {
//        private Bus _bus;

//        public virtual IServerTransporter CreateTransporter(Bus bus, Configuration config)
//        {
//            throw new NotImplementedException();
//        }

//        public virtual void Initialize()
//        {
//            throw new NotImplementedException();
//        }

//        public virtual Func<string, string> Selector { get; set; }

//        public virtual Func<string, string> MessageReceived { get; set; }

//        public virtual Func<string, string> RegisteredSubscriber { get; set; }


//        protected virtual void HandleSubscription(string s1, string s2)
//        {
//            //_bus.RegisterSubscription();
//            //RegisteredSubscriber.Invoke(s1);
//        }


//        public virtual bool Shutdown()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}


