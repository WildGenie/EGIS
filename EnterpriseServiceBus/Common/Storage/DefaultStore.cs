using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common.Messages;

namespace GeoDecisions.Esb.Common.Storage
{
    class DefaultStore : IStore
    {
        public int Priority { get; set; }

        public bool IsAvailable { get; set; }

        public string Name
        {
            get { return Names.DefaultStore; }
        }

        public void InitSession()
        {
            throw new NotImplementedException();
        }

        public List<string> GetCommands()
        {
            return new List<string>();
        }

        public List<ISubscriber> GetValidSubscribers()
        {
            return new List<ISubscriber>();
        }

        public List<IPublisher> GetValidPublishers()
        {
            return new List<IPublisher>();
        }

        public bool SaveMessage(EsbMessage message)
        {
            return false;
        }

        public bool SaveSubscriber(ISubscriber subscriber)
        {
            return false;
        }

        public bool SavePublisher(IPublisher publisher)
        {
            return false;
        }

        public void Configure(IStoreConfig databaseConfig)
        {
            this.IsAvailable = true;
            this.Priority = 10;
        }

        public void PurgeMessages(int nDays)
        {
            return;
        }
    }
}
