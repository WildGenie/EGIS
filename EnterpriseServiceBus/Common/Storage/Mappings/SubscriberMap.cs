using FluentNHibernate.Mapping;
using GeoDecisions.Esb.Common.Storage.Entities;

namespace GeoDecisions.Esb.Common.Storage.Mappings
{
    internal class SubscriberMap : ClassMap<SubscriberEntity>
    {
        public SubscriberMap()
        {
            Table("Subscribers");
            LazyLoad();

            Id(x => x.Id).Column("ID").GeneratedBy.UuidHex("N");
            Map(x => x.Uri);
            Map(x => x.MachineName);
            Map(x => x.ClientId);
            Map(x => x.LastHeartbeat);
        }
    }
}