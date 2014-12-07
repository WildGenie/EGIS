using System;
using System.Collections.Generic;
using GeoDecisions.Esb.Client.Core.Interfaces;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Services;

namespace GeoDecisions.Esb.Client.Core
{
    public class Configuration
    {
        //SQLite Database configuration
        private Action<IComHandler> _comHandlerConfigAction;
        private string _ddlFileName = "nHibernate.sql";
        private IDetailLogger _defaultLogger;
        private string _sqliteDBName = @"\EbusDB\Ebus-client.db";
        private string _sqlitePath = @"\EbusDB\";

        public Configuration(string ip, int port, string id = null, string name = null, string description = null)
        {
            Id = id ?? Guid.NewGuid().ToString();
            EsbServerIp = ip;
            EsbServerPort = port;

            Name = string.IsNullOrEmpty(name) ? "Default Name" : name;
            Description = string.IsNullOrEmpty(description) ? "Default Description" : description;

            AddLogger(new ClientLogger());
            DefaultLogger.Log(string.Format("Preparing to get configuration from Server {0} and Port {1}", EsbServerIp, EsbServerPort));
            var configClient = new ConfigurationClient(this);

            if (IsConfigured)
            {
                DefaultLogger.Log(string.Format("Finished getting configuration from Server {0} and Port {1}", EsbServerIp, EsbServerPort));
                DefaultLogger.Log(string.Format(
                    "RedisServer Ip:{0}, RedisPort:{1}, TcpServerIp:{2}, TcpPort:{3}, RestServerIp:{4}, RestPort:{5}",
                    RedisHost, RedisPort, WcfTcpHost, WcfTcpPort, WcfRestHost, WcfRestPort
                                      )
                    );
            }

            ComHandlers = new List<IComHandler>();
        }

        public Configuration(IDetailLogger logger = null)
        {
            // setup logger ahead of time to log config errors ??
            DefaultLogger = logger;

            // defaults
            //Ports = new List<int> {9001, 9002, 9003, 9004, 9005};

            ComHandlers = new List<IComHandler>();
        }

        //private string _sqliteConnectString = "Data Source=" + _sqliteDBName + ";Version=3;New=False;Compress=True;";
        internal List<IComHandler> ComHandlers { get; private set; }

        public string SQLiteDBName
        {
            get { return _sqliteDBName; }
            set { _sqliteDBName = value; }
        }

        public string SQLitePath
        {
            get { return _sqlitePath; }
            set { _sqlitePath = value; }
        }

        public string DDLFileName
        {
            get { return _ddlFileName; }
            set { _ddlFileName = value; }
        }

        //


        public string Id { get; set; }

        public int EsbServerPort { get; set; }

        public string EsbServerIp { get; set; }


        public Uri MetadataUrl { get; set; }

        internal IMetadataService MetadataService { get; set; }

        internal IBusService BusService { get; set; }

        internal IBusRestService BusRestService { get; set; }

        public Uri PermSubUri { get; set; }

        //public List<int> Ports { get; private set; }

        //If this flag is set the failed messages that are stored in the sqlite (Messages) table Ebus-client.db database will be resent when client reconnects
        public bool AutoResendFailedMessages { get; set; }

        //internal List<IMessageDispatcher> Dispatchers { get; set; }

        //internal MessageCollector CollectorManager { get; set; }

        //internal MessageDispatcher DispatchManager { get; set; }

        public IDetailLogger DefaultLogger
        {
            get
            {
                //TODO Consider changing this to ClientLogger instead of EmptyLogger so that the client console does not have to do an AddLogger to modify the DefaultLogger like it is currently doing
                if (_defaultLogger == null)
                    _defaultLogger = (IDetailLogger) new EmptyLogger(); // does nothing but prevents null refs

                return _defaultLogger;
            }

            private set { _defaultLogger = value; }
        }

        public string RedisHost { get; set; }
        public int RedisPort { get; set; }

        public string WcfTcpHost { get; set; }
        public int WcfTcpPort { get; set; }
        public string WcfRestHost { get; set; }
        public int WcfRestPort { get; set; }
        public bool IsConfigured { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Configuration UseMetadataServer(Uri uri)
        {
            // do something
            return this;
        }

        public Configuration ExposeTypes(IEnumerable<Type> types)
        {
            // do something

            return this;
        }

        //public Configuration AddPort(int port)
        //{
        //    Ports.Add(port);
        //    return this;
        //}

        //public Configuration AddDispatcher(IMessageDispatcher dispatcher)
        //{
        //    Dispatchers.Add(dispatcher);
        //    return this;
        //}


        public Configuration AddLogger(IDetailLogger logger)
        {
            DefaultLogger = logger;
            return this;
        }

        //Hookup for comHandler configuration

        public Configuration ConfigureComHandlers(Action<IComHandler> action)
        {
            if (_comHandlerConfigAction == null)
                _comHandlerConfigAction = action;

            return this;
        }

        internal void ConfigureComHandlers()
        {
            if (_comHandlerConfigAction != null)
                ComHandlers.ForEach(_comHandlerConfigAction);
        }
    }
}