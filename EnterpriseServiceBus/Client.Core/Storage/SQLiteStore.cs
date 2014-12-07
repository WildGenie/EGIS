using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AutoMapper;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Storage.Entities;
using GeoDecisions.Esb.Common.Storage.Mappings;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace GeoDecisions.Esb.Client.Core.Storage
{
    internal class SQLiteStore : IClientStore
    {
        //TODO Add public properties for the below database properties so that it can be configured by the client
        private static string _dbName; // = AssemblyDirectory + @"\EbusDB\Ebus-client.db";
        private static string _sqlPath; // = AssemblyDirectory + @"\EbusDB\";
        private static string _sqlFileName; // = "nHibernate.sql";
        private static string _connectString; // = "Data Source=" + _dbName + ";Version=3;New=False;Compress=True;";
        private static ISessionFactory _sessionFactory;
        private bool _isAvailable = true;
        private int _priority = 1;

        public SQLiteStore()
        {
            _isAvailable = true;
            _priority = 1;
        }

        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    CreateTables(true);
                    _sessionFactory = CreateSessionFactory();
                }
                return _sessionFactory;
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public bool IsAvailable
        {
            get { return _isAvailable; }
            set { _isAvailable = value; }
        }

        public void Init(Configuration config)
        {
            _isAvailable = true;
            _priority = 1;
            _dbName = AssemblyDirectory + config.SQLiteDBName;
            _sqlPath = AssemblyDirectory + config.SQLitePath;
            _sqlFileName = config.DDLFileName;
            _connectString = "Data Source=" + _dbName + ";Version=3;New=False;Compress=True;";
        }

        public bool SaveFailedMessage(EsbMessage message)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var msg = new MessageEntity();
                    Mapper.CreateMap<EsbMessage, MessageEntity>();
                    msg = Mapper.Map<EsbMessage, MessageEntity>(message);
                    session.SaveOrUpdate(msg);
                    transaction.Commit();
                }
            }
            return true;
        }

        public bool DeleteFailedMessage(EsbMessage message)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var msg = new MessageEntity();
                    Mapper.CreateMap<EsbMessage, MessageEntity>();
                    msg = Mapper.Map<EsbMessage, MessageEntity>(message);
                    session.Delete(msg);
                    transaction.Commit();
                }
            }
            return true;
        }


        public bool LogMessage(string message)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var log = new LogMessageEntity();
                    log.DateTime = DateTime.Now.ToString();
                    log.MachineName = Environment.MachineName;
                    log.MessageText = message;
                    session.SaveOrUpdate(log);
                    transaction.Commit();
                }
            }
            return true;
        }


        public List<EsbMessage> GetFailedMessages()
        {
            var msgs = new List<EsbMessage>();

            try
            {

                using (ISession session = SessionFactory.OpenSession())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    {
                        //Note that the type returned by NHibernate is MessageEntity, but we want a list of EsbMessage
                        IList<MessageEntity> messages = session.CreateCriteria(typeof (MessageEntity))
                                                               .List<MessageEntity>();

                        Console.WriteLine("Failed messages if any will attempted to be resent and deleted");
                        foreach (MessageEntity msg in messages)
                        {
                            Console.WriteLine(msg.Id + ", " + msg.Payload + ", " + msg.Uri);

                            //Convert from MessageEntity to EsbMessage
                            var esbMsg = new EsbMessage();
                            esbMsg.PublishedDateTime = msg.PublishedDateTime;
                            //Mapper.CreateMap<MessageEntity, EsbMessage>();
                            //esbMsg = Mapper.Map<MessageEntity, EsbMessage>(msg);
                            //TODO The automapper mapping fails because the MessageEntity does not have the Publisher class but the EsbMessage does, can this be rectified by adding the Publisher class to MessageEntity
                            esbMsg.Id = msg.Id;
                            esbMsg.Payload = msg.Payload;
                            esbMsg.Priority = msg.Priority;
                            esbMsg.HandlerId = msg.HandlerId;
                            esbMsg.PublishedDateTime = msg.PublishedDateTime;
                            esbMsg.Publisher = new Publisher();
                            esbMsg.Publisher.MachineName = msg.Publisher ?? string.Empty; //TODO, should the message structure have a Publisher structure if so handle that in the FluentHibernate mappings
                            esbMsg.ReplyTo = msg.ReplyTo;
                            esbMsg.RetryAttempts = msg.RetryAttempts;

                            msgs.Add(esbMsg);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //log this
            }
            return msgs;
        }

        private static ISessionFactory CreateSessionFactory()
        {
            FluentConfiguration config = Fluently.Configure().
                                                  Database(
                                                      SQLiteConfiguration.Standard.ConnectionString(_connectString)
                //SQLiteConfiguration.Standard.ConnectionString(@"Data Source=C:\E-busclient.db;Version=3;New=False;Compress=True;")
                );

            config.Mappings(m => m.FluentMappings.Add<MessageMap>())
                  .Mappings(m => m.FluentMappings.Add<LogMessageMap>());

            //.Mappings(m => m.FluentMappings.AddFromAssemblyOf<MessageMap>());
            //.Mappings(m => m.FluentMappings.Add<LogMessageMap>());

            return config.BuildSessionFactory();
        }

        private static void CreateTables(bool execute = false)
        {
            NHibernate.Cfg.Configuration config = Fluently.Configure().
                                                           Database(
                                                               SQLiteConfiguration.Standard.ConnectionString(_connectString)
                )
                                                          .Mappings(m => m.FluentMappings.Add<MessageMap>())
                                                          .Mappings(m => m.FluentMappings.Add<LogMessageMap>())
                                                          .BuildConfiguration();

            bool IsExists = Directory.Exists(_sqlPath);

            if (!IsExists)
            {
                Directory.CreateDirectory(_sqlPath);

                new SchemaExport(config).SetOutputFile(_sqlPath + _sqlFileName).Create(false, execute);
            }
        }
    }
}