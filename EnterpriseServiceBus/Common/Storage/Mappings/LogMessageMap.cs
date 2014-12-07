using FluentNHibernate.Mapping;
using GeoDecisions.Esb.Common.Storage.Entities;

namespace GeoDecisions.Esb.Common.Storage.Mappings
{
    internal class LogMessageMap : ClassMap<LogMessageEntity>
    {
        public LogMessageMap()
        {
            Table("LogMessage");
            LazyLoad();

            Id(x => x.Id).Column("ID").GeneratedBy.UuidHex("N");
            Map(x => x.DateTime);
            Map(x => x.MachineName);
            Map(x => x.MessageText);
        }
    }
}