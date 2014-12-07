using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using AutoMapper;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Storage.Entities;
using GeoDecisions.Esb.Common.Storage.Mappings;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace GeoDecisions.Esb.Common.Storage
{
    public class OracleConfig : IStoreConfig
    {
        public string Host { get; set; }
        public string ConnectString { get; set; }
        public int Priority { get; set; }
        public int IsAvailable { get; set; }
    }

    internal class OracleStore : IStore
    {
        //TODO
        //private static OracleDatabase _db;

        private static bool isAvailable = true;

        //CreateTables();

        private static ISessionFactory _sessionFactory;
        internal int _priority = 1;

        public OracleStore()
        {
            isAvailable = true;
            //_priority = 1;
        }

        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    _sessionFactory = CreateSessionFactory();
                    CreateTables();
                }
                return _sessionFactory;
            }
        }

        private static string ConnectString { get; set; }

        private static string Host { get; set; }

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public string Name
        {
            get { return Names.Oracle; }
        }

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
            set { isAvailable = value; }
        }

        public void InitSession()
        {
            var cmds = new List<string>();

            ISession session = SessionFactory.OpenSession();
        }

        public List<string> GetCommands()
        {
            var cmds = new List<string>();

            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    IList<ServerCommandsEntity> commands = session.CreateCriteria(typeof(ServerCommandsEntity))
                                                                  .List<ServerCommandsEntity>();

                    foreach (ServerCommandsEntity cmd in commands)
                    {
                        cmds.Add(cmd.Usage + " : " + cmd.Description);
                    }
                }
            }
            return cmds;
        }



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
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var msg = new MessageEntity();
                    Mapper.CreateMap<EsbMessage, MessageEntity>().ForMember( prop => prop.Publisher, opt => opt.MapFrom(src => src.Publisher.MachineName));
                    msg = Mapper.Map<EsbMessage, MessageEntity>(message);
                    
                    /*
                    msg.Id = message.Id;
                    msg.HandlerId = message.HandlerId;
                    msg.Payload = message.Payload;
                    msg.Priority = message.Priority;
                    msg.Publisher = message.Publisher.MachineName;
                    msg.ReplyTo = message.ReplyTo;
                    msg.RetryAttempts = message.RetryAttempts;
                    msg.PublishedDateTime = message.PublishedDateTime;
                    msg.Uri = message.Uri;
                    msg.Type = message.Type;
                    */

                    session.SaveOrUpdate(msg);
                    transaction.Commit();
                }
            }
            return true;
        }

        public void PurgeMessages(int nDays)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {

                    session.CreateQuery("delete from MessageEntity where PublishedDateTime < sysdate - " + nDays).ExecuteUpdate();
                    transaction.Commit();
                }
            }
            return;
        }

        public bool SaveSubscriber(ISubscriber subscriber)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var sub = new SubscriberEntity();

                    //Id cannot be set and needs to be auto generated, so ignore it
                    Mapper.CreateMap<ISubscriber, SubscriberEntity>().ForMember(prop => prop.Id, opt => opt.Ignore());
                    sub = Mapper.Map<ISubscriber, SubscriberEntity>(subscriber);
                    
                    /*
                    sub.LastHeartbeat = subscriber.LastHeartbeat;
                    sub.ClientId = subscriber.ClientId;
                    sub.MachineName = subscriber.MachineName;
                    sub.Uri = subscriber.Uri;
                    */

                    session.SaveOrUpdate(sub);
                    transaction.Commit();
                }
            }
            return true;
        }

        public bool SavePublisher(IPublisher publisher)
        {
            using (ISession session = SessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var pub = new PublisherEntity();
                    //Id cannot be set and needs to be auto generated, so ignore it
                    Mapper.CreateMap<IPublisher, PublisherEntity>().ForMember(prop => prop.Id, opt => opt.Ignore());
                    pub = Mapper.Map<IPublisher, PublisherEntity>(publisher);

                    /*pub.MachineName = publisher.MachineName;*/

                    session.SaveOrUpdate(pub);
                    transaction.Commit();
                }
            }
            return true;
        }

        public void Configure(IStoreConfig databaseConfig)
        {
            var oracleConfig = databaseConfig as OracleConfig;

            if (oracleConfig == null)
                throw new Exception("Oracle configuration was invalid.");

            // do stuff with it here...
            Host = oracleConfig.Host;
            ConnectString = oracleConfig.ConnectString;
        }

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure().
                            Database(
                                OracleClientConfiguration.Oracle10.ConnectionString(ConnectString)
                )
                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MessageMap>())
                           .BuildSessionFactory();
        }


        private static void CreateTables(bool execute = false)
        {
            Configuration config = Fluently.Configure().
                                            Database(OracleClientConfiguration.Oracle10.ConnectionString(ConnectString))
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MessageMap>())
                                           //.Mappings(m => m.FluentMappings.AddFromAssemblyOf<PublisherMap>())
                                           //.Mappings(m => m.FluentMappings.AddFromAssemblyOf<SubscriberMap>())
                                           //.Mappings(m => m.FluentMappings.AddFromAssemblyOf<ServerCommandsMap>())
                                           .BuildConfiguration();


            //new SchemaExport(config).SetOutputFile(@"C:\Projects\Military\EGIS\EnterpriseServiceBus\Common\bin\Debug\nhibernate.sql").Create(false, execute);
            new SchemaExport(config).SetOutputFile(AssemblyDirectory + @"\nhibernate.sql").Create(false, execute);
        }
    }
}