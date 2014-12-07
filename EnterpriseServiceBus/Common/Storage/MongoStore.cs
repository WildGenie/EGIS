using System;
using System.Collections.Generic;
using System.Linq;
using GeoDecisions.Esb.Common.Messages;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace GeoDecisions.Esb.Common.Storage
{
    public class MongoConfig : IStoreConfig
    {
        public string Host { get; set; }
        public string ConnectString { get; set; }
        public string Database { get; set; }
    }

    internal class MongoStore : IStore
    {
        //public string SubscriberClass = "GeoDecisions.Esb.Common.Messages.Subscriber";
        private static MongoClient _client;
        private static MongoServer _server;
        private static MongoDatabase _db;
        //TODO These fields need to come from configuration
        private static bool isAvailable = true;
        internal int _priority = 2;

        private MongoStore()
        {
            isAvailable = true;
            //_priority = 1;
        }

        private static string ConnectString { get; set; }

        private static string Host { get; set; }

        private static string Database { get; set; }

        public static MongoDatabase db
        {
            get { return GetMongoDb(); }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
            set { isAvailable = value; }
        }


        public string Name
        {
            get
            {
                return Names.Mongo;
                ;
            }
        }

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public void InitSession()
        {
            //This just initializes the Mongo database and if this fails it throws an exception which marks this Store as not available in the StorageManager
            //var initDb = db;
            GetCommands();
        }

        //TODO Need to change the way this function is defined, maybe this should be wrapped in a manager class as a static 
        public List<string> GetCommands()
        {
            var commands = new List<string>();
            MongoCollection<ServerCommands> serverCommands = db.GetCollection<ServerCommands>("ServerCommands");
            //var query = Query<ServerCommands>.Matches(g => g.Usage, "info");
            MongoCursor<ServerCommands> results = serverCommands.FindAll();
            //var results = serverCommands.Find(query);
            /*
            foreach (var command in results)
            {
                commands.Add(command.Usage + " : " + command.Description);
            }
            return commands;
            */
            return results.Select(command => command.Usage + " : " + command.Description).ToList();
        }

        //TODO Remove the below three methods after testing
        /*
        public static void AddMessage(EsbMessage message)
        {
            MongoCollection<EsbMessage> messages = db.GetCollection<EsbMessage>("Messages");
            messages.Insert(message);
        }

        public static void AddSubscribers(Subscriber sub)
        {
            MongoCollection<Subscriber> subscriber = db.GetCollection<Subscriber>("ActiveSubscribers");
            subscriber.Insert(sub);
        }

        public static void AddPublisher(Publisher pub)
        {
            MongoCollection<Publisher> publisher =db.GetCollection<Publisher>("ActivePublishers");
            publisher.Insert(pub);
        }
        */

        //TODO
        /*
        public static void AddServerStats(ServerStats stats)
        {
            MongoCollection<ServerStats> serverStats = db.GetCollection<ServerStats>("ActiveServerStats");
            serverStats.Insert(stats);
        }
        */

        public List<ISubscriber> GetValidSubscribers()
        {
            throw new NotImplementedException();
        }

        public List<IPublisher> GetValidPublishers()
        {
            throw new NotImplementedException();
        }

        public bool SaveMessage(EsbMessage message)
        {
            MongoCollection<EsbMessage> messages = db.GetCollection<EsbMessage>("Messages");
            messages.Insert(message);
            return true;
        }

        public bool SaveSubscriber(ISubscriber sub)
        {
            var modifiedSub = new Subscriber();
            modifiedSub.Uri = sub.Uri;
            modifiedSub.MachineName = sub.MachineName;
            modifiedSub.ClientId = sub.ClientId;
            MongoCollection<Subscriber> subscriber = db.GetCollection<Subscriber>("ActiveSubscribers");
            subscriber.Insert(modifiedSub);

            return true;

            //TODO This is a hack, but will do until a better way is found, to correct this the WcfTCPSubscriber class should be moved to the common namespace, probably in the ISubscribe class
            //if (sub.GetType().ToString() == SubscriberClass)
            //{
            //    MongoCollection<Subscriber> subscriber = db.GetCollection<Subscriber>("ActiveSubscribers");
            //    subscriber.Insert(sub);
            //}
            //else
            //{
            //    //For classes other than the Subscriber class that implements ISubscriber, convert it into a Subscriber class for now.
            //    Subscriber modifiedSub = new Subscriber();
            //    modifiedSub.Uri = sub.Uri;
            //    modifiedSub.MachineName = sub.MachineName;
            //    modifiedSub.ClientId = sub.ClientId;
            //    MongoCollection<Subscriber> subscriber = db.GetCollection<Subscriber>("ActiveSubscribers");
            //    subscriber.Insert(modifiedSub);
            //}
        }

        public bool SavePublisher(IPublisher pub)
        {
            MongoCollection<Publisher> publisher = db.GetCollection<Publisher>("ActivePublishers");
            publisher.Insert(pub);
            return true;
        }

        public void Configure(IStoreConfig databaseConfig)
        {
            var mongoConfig = databaseConfig as MongoConfig;

            if (mongoConfig == null)
                throw new Exception("Mongo configuration was invalid.");

            // do stuff with it here...
            Host = mongoConfig.Host;
            ConnectString = mongoConfig.ConnectString;
            Database = mongoConfig.Database;
        }

        public void PurgeMessages(int nDays)
        {
            MongoCollection<EsbMessage> messages = db.GetCollection<EsbMessage>("Messages");

            DateTime today = DateTime.Now;
            DateTime beforeDaysToRetain = today.AddDays(-1 * nDays);

            var query = Query<EsbMessage>.LT(e => e.PublishedDateTime, beforeDaysToRetain);
            messages.Remove(query);
        }

        public static MongoDatabase GetMongoDb()
        {
            try
            {
                if (_db == null)
                {
                    _client = new MongoClient(ConnectString); //Note: As of now Mongo should be running in non authentication mode and a database called ESB should exist with the ServerCommands table
                    _server = _client.GetServer();
                    _db = _server.GetDatabase(Database);
                    isAvailable = true;
                }
            }
            catch (Exception ex)
            {
                //TODO log this
                isAvailable = false;
            }
            return _db;
        }
    }
}