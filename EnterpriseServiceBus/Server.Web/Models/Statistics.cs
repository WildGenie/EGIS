using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;

namespace GeoDecisions.Esb.Server.Web.Models
{
    public class Statistics
    {
        public List<Client> Clients { get; private set; }
        public List<ISubscriber> Subscribers { get; private set; }
        public List<IPublisher> Publishers { get; private set; }
        public List<EsbMessage> Messages1m
        {
            get
            {
                int count = 0;
                var msgs = new List<EsbMessage>();
                foreach (EsbMessage msg in Messages)
                {
                    //s.Add(msg.ToJson());
                    double seconds = (DateTime.Now - msg.PublishedDateTime).TotalSeconds;

                    if (seconds < 60)
                    {
                        count++;
                        msgs.Add(msg);
                    }
                }

                return msgs;
            }
            private set { }
        }

        public List<EsbMessage> Messages1h
        {
            get
            {
                int count = 0;
                var msgs = new List<EsbMessage>();
                foreach (EsbMessage msg in Messages)
                {
                    //s.Add(msg.ToJson());
                    double seconds = (DateTime.Now - msg.PublishedDateTime).TotalSeconds;

                    if (seconds < 3600)
                    {
                        count++;
                        msgs.Add(msg);
                    }
                }

                return msgs;
            }
            private set { }
        }
        public List<EsbMessage> Messages { get; private set; }
        public List<EsbMessage> MessagesToReplay { get; set; }
        public DateTime ServerStartTime { get; private set; }
        public List<IServerTransporter> Transporters { get; private set; }
        public List<IStore> Databases { get; private set; }


        public void Initialize(DateTime startDateTime, List<ISubscriber> subscribers, List<IPublisher> publishers, List<EsbMessage> messages, List<Client> clients, 
                            List<IServerTransporter> transporters,
                            List<IStore> databases
                            )
        {
            ServerStartTime = startDateTime;
            Subscribers = subscribers;
            Publishers = publishers;
            Messages = messages;
            Clients = clients;
            Transporters = transporters;
            Databases = databases;
        }
    }
}
