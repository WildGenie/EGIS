using FluentNHibernate.Mapping;
using GeoDecisions.Esb.Common.Storage.Entities;

namespace GeoDecisions.Esb.Common.Storage.Mappings
{
    internal class PublisherMap : ClassMap<PublisherEntity>
    {
        public PublisherMap()
        {
            Table("Publishers");
            LazyLoad();

            Id(x => x.Id).Column("ID").GeneratedBy.UuidHex("N");
            Map(x => x.MachineName);
            Map(x => x.ClientId);
        }
    }
}