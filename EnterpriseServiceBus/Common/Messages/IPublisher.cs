namespace GeoDecisions.Esb.Common.Messages
{
    public interface IPublisher
    {
        string MachineName { get; set; }
    }

    public class Publisher : IPublisher
    {
        public string MachineName { get; set; }
    }
}