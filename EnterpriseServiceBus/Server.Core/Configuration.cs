using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Utility;

namespace GeoDecisions.Esb.Server.Core
{
    public class Configuration
    {
        //net.tcp://localhost:9000/esb

        private string _host = "localhost";
        private readonly List<Uri> _mdUris;
        private int _port = 9911;
        private Action<IStore> _databaseConfigAction;
        private IDetailLogger _defaultLogger;
        //private string _esbEndpoint = "esb";
        private string _httpScheme = "http";
        private bool _isCompiled;
        private string _metadataEndpoint = "metadata";
        private ISerialization _serialization;
        //private string _tcpScheme = "net.tcp";
        private Action<IServerTransporter> _transporterConfigAction;
        private MessageSettings _settings;
        private Action<MessageSettings> _saveSettingsAction;

        public Configuration(string host = "localhost", int port = 9911, IDetailLogger logger = null)
        {
            _defaultLogger = logger;
            _isCompiled = false;
            //_host = Environment.MachineName;
            //_port = 9000;
            _mdUris = new List<Uri>();
            Transporters = new List<IServerTransporter>();
            _serialization = new JsonSerialization(); // default

            _host = host;
            _port = port;
        }

        public string ServerHost {
            get { return _host; }
        }

        public int ServerPort {
            get { return _port; }
        }

        internal ISerialization Serialization
        {
            get { return _serialization; }
        }

        internal string MetadataEndpoint
        {
            get { return _metadataEndpoint; }
        }

        //internal Uri HttpBaseUrl
        //{
        //    get
        //    {
        //        string uriString = string.Format("{0}://{1}:{2}/{3}", _httpScheme, _host, _port, _esbEndpoint);
        //        return new Uri(uriString);
        //    }
        //}

        //internal Uri TcpBaseUri
        //{
        //    get
        //    {
        //        string uriString = string.Format("{0}://{1}:{2}/{3}", _tcpScheme, _host, _port + 1, _esbEndpoint);
        //        return new Uri(uriString);
        //    }
        //}

        public Uri BusEndpoint { get; set; }

        internal List<IServerTransporter> Transporters { get; private set; }

        public IDetailLogger DefaultLogger
        {
            get
            {
                //The EmptyLogger is cast to a derived type so that we do not have to change what the EmptyLogger returns, 
                //this way ILogger does not need to implement the additional methods in IDetailLogger, rxs, 03/29/2013
                if (_defaultLogger == null)
                    _defaultLogger = new EmptyLogger(); // does nothing but prevents null refs

                return _defaultLogger;
            }

            private set { _defaultLogger = value; }
        }

        internal List<IStore> Databases { get; set; }

        public Configuration SetSerialization(ISerialization serialization)
        {
            if (_isCompiled)
                return this;

            _serialization = serialization;
            return this;
        }

        public Configuration SetMetadataServer(Uri uri)
        {
            if (_isCompiled)
                return this;

            _mdUris.Add(uri);
            return this;
        }

        public Configuration SetMetadataServer(string host = "localhost", int? port = null)
        {
            if (_isCompiled)
                return this;

            string uriString = string.Format("{0}://{1}:{2}", _httpScheme, host, port);

            _mdUris.Add(new Uri(uriString));

            return this;
        }

        public Configuration ExposeTypes(IEnumerable<Type> types)
        {
            if (_isCompiled)
                return this;

            // do something
            return this;
        }

        public Configuration EnableTcp(bool enabled)
        {
            if (_isCompiled)
                return this;

            // do something
            return this;
        }

        public Configuration EnableHttp(bool enabled)
        {
            if (_isCompiled)
                return this;

            // do something
            return this;
        }

        public Configuration AddLogger(IDetailLogger logger)
        {
            DefaultLogger = logger;
            return this;
        }

        public Configuration AddTransporter(IServerTransporter transporter)
        {
            Transporters.Add(transporter);
            return this;
        }

        public Configuration Compile()
        {
            //TODO: validate configuration;

            _isCompiled = true;

            return this;
        }

        public static EsbConfiguration ReadFile(string fileName)
        {
            var fullPath = fileName;

            var isRooted = Path.IsPathRooted(fullPath);

            if (!isRooted)
            {
                var exeLoc = Assembly.GetExecutingAssembly().Location;
                var rootDir = Path.GetDirectoryName(exeLoc);
                fullPath = Path.Combine(rootDir, fullPath);
            }

            var xs = new XmlSerializer(typeof (EsbConfiguration));
            var sr = new StreamReader(fullPath);
            var c = (EsbConfiguration) xs.Deserialize(sr);
            sr.Close();
            c.BuildConfigClasses();
            return c;
            //c.Display();
            //Console.ReadKey();
            //c.DisplayTransporterConfigs();
            //Console.ReadKey();
            //c.DisplayDatabaseConfigs();
            //Console.ReadKey();
        }

        //Hookup for Transporter configuration

        public Configuration ConfigureTransporters(Action<IServerTransporter> action)
        {
            _transporterConfigAction = action;
            return this;
        }

        internal void ConfigureTransporters()
        {
            if (_transporterConfigAction != null)
                Transporters.ForEach(_transporterConfigAction);
        }

        //Hookup for database configuration

        public Configuration ConfigureDatabases(Action<IStore> action)
        {
            _databaseConfigAction = action;
            return this;
        }

        public Configuration SaveSettings(Action<MessageSettings> action)
        {
            _saveSettingsAction = action;
            return this;
        }

        internal void CallSaveSettingsAction(MessageSettings s)
        {
            _saveSettingsAction.Invoke(s);
        }


        internal void ConfigureDatabases()
        {
            if (_databaseConfigAction != null)
                Databases.ForEach(_databaseConfigAction);
        }

        public Configuration SetServerHost(string host)
        {
            _host = host;
            return this;
        }

        public Configuration SetServerPort(int port)
        {
            _port = port;
            return this;
        }

        public void ReInit()
        {
            //_isCompiled = false;
            if (_mdUris != null)
                _mdUris.Clear();
            
            if (this.Databases != null)
                this.Databases.Clear();
            
            if (this.Transporters != null)
                this.Transporters.Clear();
        }

        public MessageSettings MessageSettings { get; set; }
    }

    public class MessageSettings
    {
        public int DaysToRetainMessages
        {
            get;
            set;
        }
    }

}