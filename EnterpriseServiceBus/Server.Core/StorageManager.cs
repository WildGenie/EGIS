using System;
using System.Collections.Generic;
using System.Linq;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Common.Utility.Plugins;
using Syscon = System.Console;

namespace GeoDecisions.Esb.Server.Core
{
    public class StorageManager
    {
        internal static List<IStore> _dbs = new List<IStore>();
        //internal static MongoStore Mongo = new MongoStore();
        //private static StorageManager _storageManager;
        private static Configuration _config;

        public static List<IStore> DBs
        {
            get { return _dbs; }
        }

        //public static void Restart()
        //{
        //    _dbs = new List<IStore>();
        //}

        public static void Init(Configuration config = null)
        {
            _config = config;
            _dbs = new List<IStore>();

            IEnumerable<IStore> foundDatabases = PluginManager.GetAll<IStore>();

            _dbs.AddRange(foundDatabases);

            //TODO Write a function which takes the configuration object as a parameter to spin through all DBs and set the priority property and the available property - here
            //TODO Will the configuration of the server be available here?
            //TODO should the Storage manager be a class in the Server.Core namespace?

            //Order by priortiy
            _dbs = _dbs.OrderBy(db => db.Priority).ToList();
        }

        public static IStore SetAvailabilityForDBs()
        {
            _dbs = _dbs.OrderBy(db => db.Priority).ToList();
            foreach (IStore db in _dbs)
            {
                try
                {
                    db.InitSession();
                    db.IsAvailable = true;
                    _config.DefaultLogger.Log(db.GetType() + " is available");
                }
                catch (Exception e)
                {
                    db.IsAvailable = false;
                    _config.DefaultLogger.Log(db.GetType() + " is not available");
                    _config.DefaultLogger.Log(e.Message);
                }
            }
            return null;
        }

        public static IStore GetAvailableDb()
        {
            _dbs = _dbs.OrderBy(db => db.Priority).ToList();
            foreach (IStore db in _dbs)
            {
                if (db.IsAvailable)
                {
                    //_config.DefaultLogger.Log(db.GetType() + " is the active database");
                    return db;
                }
            }
            return null;
        }

        public static void DbPrioritiesAndAvailability()
        {
            _dbs = _dbs.OrderBy(db => db.Priority).ToList();
            foreach (IStore db in _dbs)
            {
                Syscon.WriteLine("Name=" + db.Name + ", Available=" + db.IsAvailable + ", Priority=" + db.Priority);
            }
        }

        public static void InitSession()
        {
            //This function evaluates the availability of the IStores and sets it to true or false
            SetAvailabilityForDBs();

            IStore store = GetAvailableDb();
            try
            {
                store.InitSession();
            }
            catch (Exception)
            {
                store.IsAvailable = false;
                //log("There was a problem accessing the database, the database maybe down");
            }
        }

        public static List<string> GetServerCommands()
        {
            IStore store = GetAvailableDb();

            var s = new List<string>();
            try
            {
                //TODO Get this from the available list of DBs
                s = store.GetCommands();
            }
            catch(Exception e)
            {
                store.IsAvailable = false;
                s.Add("There was a problem accessing the database, the database maybe down");
                _config.DefaultLogger.Log("GetServerCommands failed");
                _config.DefaultLogger.Log(e.Message);
            }
            return s;
        }

        public static bool SaveMessage(EsbMessage message)
        {
            IStore store = GetAvailableDb();

            bool returnValue;
            try
            {
                store.SaveMessage(message);
                returnValue = true;
            }
            catch (Exception ex)
            {
                store.IsAvailable = false;
                returnValue = false;
                _config.DefaultLogger.Log("SaveMessage failed");
                _config.DefaultLogger.Log(ex.Message);
            }
            return returnValue;
        }

        public static bool SaveSubscriber(ISubscriber sub)
        {
            IStore store = GetAvailableDb();

            bool returnValue;
            try
            {
                store.SaveSubscriber(sub);
                returnValue = true;
            }
            catch (Exception e)
            {
                store.IsAvailable = false;
                returnValue = false;
                _config.DefaultLogger.Log("SaveSubscriber failed");
                _config.DefaultLogger.Log(e.Message);
            }
            return returnValue;
        }

        public static bool SavePublisher(IPublisher pub)
        {
            IStore store = GetAvailableDb();

            bool returnValue;
            try
            {
                store.SavePublisher(pub);
                returnValue = true;
            }
            catch (Exception e)
            {
                store.IsAvailable = false;
                returnValue = false;
                _config.DefaultLogger.Log("SavePublisher failed");
                _config.DefaultLogger.Log(e.Message);
            }
            return returnValue;
        }

        public static bool PurgeMessages(int nDays)
        {
            IStore store = GetAvailableDb();

            bool returnValue;
            try
            {
                store.PurgeMessages(nDays);
                returnValue = true;
            }
            catch (Exception e)
            {
                store.IsAvailable = false;
                returnValue = false;
                _config.DefaultLogger.Log("PurgeMessage failed");
                _config.DefaultLogger.Log(e.Message);
            }
            return returnValue;
        }
    }
}