using FluentNHibernate.Mapping;
using GeoDecisions.Esb.Common.Storage.Entities;

namespace GeoDecisions.Esb.Common.Storage.Mappings
{
    internal class ServerCommandsMap : ClassMap<ServerCommandsEntity>
    {
        public ServerCommandsMap()
        {
            Table("ServerCommands");

            LazyLoad();
            Id(x => x._id).Column("ID").GeneratedBy.UuidHex("N");
            Map(x => x.Usage);
            Map(x => x.Description);
        }
    }
}