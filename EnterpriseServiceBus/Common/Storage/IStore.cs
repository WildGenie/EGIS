using System.Collections.Generic;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Utility.Plugins;

namespace GeoDecisions.Esb.Common.Storage
{
    //This interface will be implemented by an actual database implementation class that will offer permanant storage
    public interface IStore : IPlugin
    {
        int Priority { get; set; }

        bool IsAvailable { get; set; }

        string Name { get; }

        //Initialises the database provider session before hand so that this is not a road block then the messages start flooding
        void InitSession();

        //These are the commands that are going to be supported by the server console interface
        List<string> GetCommands();

        //These are the valid machines names, IPs that will be allowed to issue a subscribe (sub) command
        List<ISubscriber> GetValidSubscribers();

        //These are the valid machine names, IPs that will be allowed to issue a publish command
        List<IPublisher> GetValidPublishers();

        //The messages that are published will be saved to a database using this command
        bool SaveMessage(EsbMessage message);

        //Active subscribers that have issued a subscribe (sub) command will be saved to a database using this command
        bool SaveSubscriber(ISubscriber subscriber);

        //Active publishers that have issued a publish (pub) command will be saved to a database using this command
        bool SavePublisher(IPublisher publisher);

        void Configure(IStoreConfig databaseConfig);
        void PurgeMessages(int nDays);
    }

    public interface IStoreConfig
    {
    }
}