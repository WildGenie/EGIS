namespace GeoDecisions.Esb.Common
{
    public class Names
    {
        //ComHandlers
        public const string RedisComHandler = "RedisComHandler";
        public const string WcfTcpComHandler = "WcfTcpComHandler";
        public const string WcfHttpRestComHandler = "WcfHttpRestComHandler";
        public const string HttpComHandler = "HttpComHandler";

        //Transporters
        public const string RedisTransporter = "RedisTransporter";
        public const string WcfTcpTransporter = "WcfTcpTransporter";
        public const string WcfHttpRestTransporter = "WcfHttpRestTransporter";
        public const string HttpTransporter = "HttpTransporter";

        // Redis Channels
        public const string RedisAnnounceCh = "3455hi67w1.announce";
        public const string RedisPulseCh = "e46hjnwq3.pulse";
        public const string RedisShutdownCh = "7dy27akff.Shutdown";
        public const string RedisPublishCh = "1a2s3d4f.publish";

        //Databases
        public const string DefaultStore = "DefaultStore";
        public const string Oracle = "Oracle";
        public const string Mongo = "Mongo";

        //Internal Redis channels
        public const string REDIS_SUB_CHANNEL = "esb/subscribe"; // this is mutually agreed upon
        //public const string REDIS_SUBACK_CHANNEL = "esb/subscribe/ack";  // this is mutually agreed upon

        public const string REDIS_UNSUB_CHANNEL = "esb/unsubscribe"; // this is mutually agreed upon
        //public const string REDIS_UNSUBACK_CHANNEL = "esb/unsubscribe/ack";  // this is mutually agreed upon

        //To receive a replayed message, client should subscribe with the below string prefixed to the original channel
        public const string REPLAY_CHANNEL_PREFIX = "Replay-";
    }
}