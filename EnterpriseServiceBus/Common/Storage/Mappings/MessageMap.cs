using FluentNHibernate.Mapping;
using GeoDecisions.Esb.Common.Storage.Entities;

namespace GeoDecisions.Esb.Common.Storage.Mappings
{
    internal class MessageMap : ClassMap<MessageEntity>
    {
        public MessageMap()
        {
            Table("Messages");
            LazyLoad();

            //Id(x => x.Id).Column("ID").GeneratedBy.UuidHex("N");
            Id(x => x.Id).Column("ID").GeneratedBy.Assigned();
            Map(x => x.Uri);
            Map(x => x.Type);
            Map(x => x.ReplyTo);
            Map(x => x.Priority);
            Map(x => x.RetryAttempts);
            Map(x => x.PublishedDateTime);
            Map(x => x.Publisher);
            Map(x => x.Payload);
            Map(x => x.HandlerId);
        }
    }
}