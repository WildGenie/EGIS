using System;
using System.ServiceModel;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages.Configuration;
using GeoDecisions.Esb.Common.Services;

namespace GeoDecisions.Esb.Client.Core
{
    public class ConfigurationClient
    {
        public ConfigurationClient(Configuration configuration)
        {
            try
            {
                configuration.IsConfigured = false;

                var tcpBinding = new NetTcpBinding(SecurityMode.None);

                var configEndpoint =
                    new EndpointAddress(string.Format("net.tcp://{0}:{1}/esb/services/configuration", configuration.EsbServerIp, configuration.EsbServerPort));

                var channelFactory = new ChannelFactory<IBusConfigService>(tcpBinding);
                IBusConfigService configClient = channelFactory.CreateChannel(configEndpoint);

                ServerConfigMessage clientConfigSettings = configClient.GetConfiguration();

                clientConfigSettings.TransporterSettings.ForEach(ts =>
                    {
                        switch (ts.Name)
                        {
                            case Names.RedisTransporter:
                                configuration.RedisPort = ts.Port;
                                configuration.RedisHost = ts.Host;
                                break;
                            case Names.WcfTcpTransporter:
                                configuration.WcfTcpPort = ts.Port;
                                configuration.WcfTcpHost = ts.Host;
                                break;
                            case Names.WcfHttpRestTransporter:
                                configuration.WcfRestPort = ts.Port;
                                configuration.WcfRestHost = ts.Host;
                                break;
                        }
                    });

                configuration.IsConfigured = true;
            }
            catch (Exception exp)
            {
                configuration.DefaultLogger.Log("An Exception occurred while getting the clientComhandler settings from the server: " + exp.Message);
                configuration.DefaultLogger.LogError("An Exception occurred while getting the clientComhandler settings from the server: " + exp.Message);
                configuration.DefaultLogger.LogFatalStackTraceAndMore("An Exception occurred while getting the clientComhandler settings from the server: " + exp.Message);
                //throw;
            }
        }
    }
}