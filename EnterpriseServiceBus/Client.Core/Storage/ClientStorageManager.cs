using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Storage.Entities;
using GeoDecisions.Esb.Common.Utility.Plugins;
using System.Reflection;
using NLog;
using Syscon = System.Console;

namespace GeoDecisions.Esb.Client.Core.Storage
{
    public class ClientStorageManager
    {
        private static Configuration _configuration;
        
        internal static List<IClientStore> _dbs = new List<IClientStore>();

        private static ClientStorageManager _clientStorageManager;

        private static ClientStorageManager Instance
        {
            get
            {
                if (_clientStorageManager == null)
                    _clientStorageManager = new ClientStorageManager();

                return _clientStorageManager;
            }
        }

        public static void Init(Configuration config)
        {
            _dbs = new List<IClientStore>();
            var foundDatabases = PluginManager.GetAll<IClientStore>();

            _configuration = config;
            _configuration.DefaultLogger.Log("This log message is coming from the ClientStorageManager in ClientCore");

            _dbs.AddRange(foundDatabases);

            //TODO Write a function which takes the configuration object as a parameter to spin through all DBs and set the priority property and the available property - here
            //TODO The configuration of the client should be available here

            //Order by priortiy
            _dbs = _dbs.OrderBy(db => db.Priority).ToList();

            GetAvailableDb().Init(_configuration);

            ////TODO This is just a test remove the code which inserts the message
            //var msg = new EsbMessage();
            //msg.Id = "Test-" + DateTime.Now;
            //msg.Priority = 1;
            //msg.Uri = "Test message";
            //msg.RetryAttempts = 2;
            //msg.ReplyTo = "Test message";
            //msg.PublishedDateTime = DateTime.Now;
            //GetAvailableDb().SaveFailedMessage(msg);
            ////

            //Write a log to the client database, this will help us determine the instances when the client connected to the bus
            GetAvailableDb().LogMessage("Client connected to bus");

        }

        public static List<EsbMessage> GetFailedMessages()
        {
            List<EsbMessage> failedMessages;
            try
            {
                failedMessages = GetAvailableDb().GetFailedMessages();
            }
            catch (Exception)
            {
                _configuration.DefaultLogger.Log("This log message is coming from the ClientStorageManager in ClientCore");
                return null;
                throw;
            }
            return failedMessages;
        }


        public static IClientStore GetAvailableDb()
        {
            try
            {
                foreach (IClientStore db in _dbs)
                {
                    if (db.IsAvailable)
                    {
                        //Currently it is only SQLite on the client side and the database will be stored in the same folder as the executable assembly and will be called
                        return db;
                    }
                }
            }
            catch (Exception e)
            {
                _configuration.DefaultLogger.Log("There was an error when the client from " + Environment.MachineName + " attempted to connect to the DB");
                _configuration.DefaultLogger.LogFatalStackTraceAndMore("There was an error when the client from " + Environment.MachineName + " attempted to connect to the DB");
                _configuration.DefaultLogger.Log("Exception Message:" + e.Message);
            }
            return null;
        }

        public static bool SaveFailedMessage(EsbMessage message)
        {
            bool returnValue;
            try
            {
                GetAvailableDb().SaveFailedMessage(message);
                returnValue = true;
            }
            catch (Exception)
            {
                returnValue = false;
            }
            return returnValue;
        }

        public static bool DeleteFailedMessage(EsbMessage message)
        {
            bool returnValue;
            try
            {
                GetAvailableDb().DeleteFailedMessage(message);
                returnValue = true;
            }
            catch (Exception)
            {
                returnValue = false;
            }
            return returnValue;
        }


        public static bool SaveFailedSubscriptions(ISubscriber sub)
        {
            bool returnValue;
            try
            {
                //TODO GetAvailableDb().SaveFailedSubscriptions(sub);
                returnValue = true;
            }
            catch (Exception)
            {
                returnValue = false;
            }
            return returnValue;
        }

        public static bool SaveFailedPublications(IPublisher pub)
        {
            bool returnValue;
            try
            {
                //TODO GetAvailableDb().SaveFailedPublications(pub);
                returnValue = true;
            }
            catch (Exception)
            {
                returnValue = false;
            }
            return returnValue;
        }
    }
}
