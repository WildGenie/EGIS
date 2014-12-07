using MongoDB.Bson;

namespace GeoDecisions.Esb.Common.Storage
{
    internal class ServerCommands
    {
        public ObjectId _id { get; set; }
        public string Usage { get; set; }
        public string Description { get; set; }
    }
}