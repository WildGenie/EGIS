namespace GeoDecisions.Esb.Common.Messages
{
    public interface IEsbHeader
    {
        string Id { get; set; }
        string Uri { get; set; }
        //string Name { get; set; }
        //string Type { get; set; }
        //string Author { get; set; }
        //string ReplyTo { get; set; }
        int? Priority { get; set; }
        int? RetryAttempts { get; set; }
    }


    public abstract class EsbHeader : IEsbHeader
    {
        public virtual string Id { get; set; }
        public virtual string Uri { get; set; }
        //public virtual string Name { get; set; }
        //public virtual string Type { get; set; }
        //public virtual string Author { get; set; }
        //public virtual string ReplyTo { get; set; }
        public virtual int? Priority { get; set; }
        public virtual int? RetryAttempts { get; set; }
    }
}