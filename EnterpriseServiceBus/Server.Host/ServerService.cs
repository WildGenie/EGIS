using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Server.Core;
using GeoDecisions.Esb.Server.Core.Transporters;
using GeoDecisions.Esb.Server.Core.Transporters.Http;
using Topshelf;
using Configuration = GeoDecisions.Esb.Server.Core.Configuration;

namespace Server.Host
{
    internal class ServerService
    {
        private const string ServerConfigFile = "EsbServer.config.xml";

        private const string MessageSettingsFile = "Message.settings.xml";

        private readonly Bus _bus;

        public ServerService()
        {
            // configure server
            Configuration configuration = new Configuration().AddLogger(new ServerLogger()).Compile();

            var c = new EsbConfiguration();
            try
            {
                c = Configuration.ReadFile(ServerConfigFile);
                configuration.DefaultLogger.Log("Config file read " + ServerConfigFile + "  was read");
            }
            catch (Exception ex)
            {
                configuration.DefaultLogger.Log("Config file read " + ServerConfigFile + "  could not be read");
                configuration.DefaultLogger.LogError(ex.Message);
            }

            configuration.ConfigureTransporters(t =>
                {
                    t.Enabled = false;

                    if (t.Name == Names.RedisTransporter)
                    {
                        t.Enabled = c.TransporterConfig(t.Name).enabled == "true";
                        t.Configure(new RedisConfig {Host = c.TransporterConfig(t.Name).host, Port = Convert.ToInt32(c.TransporterConfig(t.Name).port)});
                    }
                    else if (t.Name == Names.WcfTcpTransporter)
                    {
                        t.Enabled = c.TransporterConfig(t.Name).enabled == "true";
                        t.Configure(new WcfTcpConfig {Host = c.TransporterConfig(t.Name).host, Port = Convert.ToInt32(c.TransporterConfig(t.Name).port)});
                    }
                    else if (t.Name == Names.HttpTransporter)
                    {
                        t.Enabled = c.TransporterConfig(t.Name).enabled == "true";
                        t.Configure(new HttpConfig {Host = c.TransporterConfig(t.Name).host, Port = Convert.ToInt32(c.TransporterConfig(t.Name).port)});
                    }
                    configuration.DefaultLogger.Log(t.Name + " configured to " + t.Enabled + ", Host:" + t.Host + ", Port:" + t.Port);
                });


            configuration.ConfigureDatabases(d =>
                {
                    d.IsAvailable = true;

                    if (d.Name == Names.Oracle)
                    {
                        d.Priority = Convert.ToInt32(c.DatabaseConfig(d.Name).priority);
                        d.Configure(new OracleConfig {Host = c.DatabaseConfig(d.Name).host, ConnectString = c.DatabaseConfig(d.Name).connectString});
                    }
                    else if (d.Name == Names.Mongo)
                    {
                        d.Priority = Convert.ToInt32(c.DatabaseConfig(d.Name).priority);
                        d.Configure(new MongoConfig {Host = c.DatabaseConfig(d.Name).host, ConnectString = c.DatabaseConfig(d.Name).connectString, Database = c.DatabaseConfig(d.Name).database});
                    }
                    else if (d.Name == Names.DefaultStore)
                    {
                        d.Priority = 10;
                    }
                    configuration.DefaultLogger.Log(d.Name + " configured to priority " + d.Priority);
                });

            configuration.AddLogger(new ServerLogger());

            // Initializing an instance of the MessageSettings inside the configuration instance, this needs to happen in the server.console too
            //configuration.MessageSettings = GetMessageSettings(MessageSettingsFile); //line1: uncomment this line and line2 below if it is decided to use an XMLSerializer instead of Property.Settings
            configuration.MessageSettings = new MessageSettings
                {
                    //DaysToRetainMessages = Convert.ToInt32(ConfigurationManager.AppSettings["DaysToRetainMessages"]), cannot use this concept because we cannot save into the appSettings
                    DaysToRetainMessages = Convert.ToInt32(Properties.Settings.Default.DaysToRetainMessages)
                };

            //Save the configuration message settings to the local settings file of the server.host or serialize to a local file
            configuration.SaveSettings(s =>
                {
                    //SetMessageSettings(s,MessageSettingsFile); //line2: uncomment this line and line1 above if it is decided to use an XMLSerializer instead of Property.Settings
                    configuration.MessageSettings.DaysToRetainMessages = s.DaysToRetainMessages;
                    Properties.Settings.Default.DaysToRetainMessages = s.DaysToRetainMessages.ToString();
                    Properties.Settings.Default.Save();
                });

            _bus = new Bus(configuration);
        }

        private MessageSettings GetMessageSettings(string messageSettingsFile)
        {
            var s = new MessageSettings();
            var xReader = new XmlSerializer(s.GetType());
            try
            {
                var file = new StreamReader(messageSettingsFile);
                s = (MessageSettings)xReader.Deserialize(file);

                file.Close();
            }
            catch (Exception)
            {
            }
            
            return s;
        }

        private void SetMessageSettings(MessageSettings s, string messageSettingsFile)
        {
            var xWriter = new XmlSerializer(s.GetType());

            var file = new StreamWriter(messageSettingsFile);
            xWriter.Serialize(file, s);
        }

        public bool Continue()
        {
            return true;
        }

        public bool Pause()
        {
            return true;
        }

        public bool Start(HostControl control)
        {
            var task = new Task(() =>
                {
                    while (_bus.RetryCount < 5)
                    {
                        try
                        {
                            _bus.Start();

                            var host = new GeoDecisions.Esb.Server.Web.Host(_bus);
                            host.Start();

                            break;
                        }
                        catch (Exception ex)
                        {
                            string e = ex.Message;
                            //_bus.HandleUnhandledException(ex);

                            //todo: log
                        }
                    }
                });

            task.RunSynchronously();

            if (_bus.RetryCount >= 5)
            {
                control.Stop();
                return false;
            }

            return true;
        }

        public bool Stop()
        {
            _bus.Stop();
            return true;
        }
    }
}