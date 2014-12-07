using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Services;
using GeoDecisions.Esb.Server.Core;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Transporters;


namespace GeoDecisions.Esb.Server.Core.Services
{
    class BusMessageService : Transporter<WcfTcpSubscriber>, IBusMessageService
    {
        public BusMessageService(ITransportManager transportManager, Configuration configuration) : base(transportManager, configuration)
        {
        }

        public List<EsbMessage> GetMessages(string uri, DateTime dateTime)
        {
                var messages = base.TransportManager.CurrentMessages.Select(msg => new EsbMessage() {
                                                                                Id = msg.Id,
                                                                                Uri = msg.Uri,
                                                                                Type = msg.Type,
                                                                                Publisher = msg.Publisher,
                                                                                PublishedDateTime = msg.PublishedDateTime,
                                                                                ClientId = msg.ClientId,
                                                                                Payload = msg.Payload
                                                                            })
                                           .Where(
                                               msg =>
                                               DateTime.Compare(msg.PublishedDateTime, dateTime) > 0 &&
                                               msg.Uri == uri
                                               );

            return messages.ToList();
        }
    }
}
