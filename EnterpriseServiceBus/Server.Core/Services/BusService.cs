using System;
using System.Collections.Generic;
using System.IO;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages.Configuration;
using GeoDecisions.Esb.Common.Services;
using GeoDecisions.Esb.Server.Core.Utility;

namespace GeoDecisions.Esb.Server.Core.Services
{
    /// <summary>
    ///     TCP WCF Config Service
    /// </summary>
    internal class BusConfigService : IBusConfigService
    {
        private readonly Configuration _configuration;
        private ISerialization _serialization;

        // inject
        public BusConfigService(Configuration configuration, ISerialization serialization)
        {
            _serialization = serialization;
            _configuration = configuration;
        }

        public ServerConfigMessage GetConfiguration()
        {
            //_configuration.
            var message = new ServerConfigMessage
                {
                    ServerName = "ESB Server",
                    Timestamp = DateTime.Now,
                    TransporterSettings = new List<TransporterSettings>()
                };

            _configuration.DefaultLogger.Log("Inside call to GetConfiguration Tcp service method");

            _configuration.Transporters.ForEach(tp =>
                {
                    message.TransporterSettings.Add(new TransporterSettings
                        {
                            Host = tp.Host,
                            Port = tp.Port,
                            Enabled = tp.Enabled,
                            Name = tp.Name
                        });
                });

            _configuration.DefaultLogger.Log("Returning from call to GetConfiguration Tcp service method");

            return message;
        }
    }

    /// <summary>
    ///     TCP WCF Config Service
    /// </summary>
    internal class BusConfigRestService : IBusConfigRestService
    {
        private readonly Configuration _configuration;
        private readonly ISerialization _serialization;

        // inject
        public BusConfigRestService(Configuration configuration, ISerialization serialization)
        {
            _serialization = serialization;
            _configuration = configuration;
        }

        /// <summary>
        ///     Returns the main configuration object for the client auto-config
        /// </summary>
        /// <returns></returns>
        public Stream GetConfiguration()
        {
            //_configuration.
            var message = new ServerConfigMessage
                {
                    ServerName = "ESB Server",
                    Timestamp = DateTime.Now,
                    TransporterSettings = new List<TransporterSettings>()
                };

            _configuration.DefaultLogger.Log("Inside call to GetConfiguration Rest service method");

            _configuration.Transporters.ForEach(tp =>
                {
                    message.TransporterSettings.Add(new TransporterSettings
                        {
                            Host = tp.Host,
                            Port = tp.Port,
                            Enabled = tp.Enabled,
                            Name = tp.Name
                        });
                });


            string serializedMsg = _serialization.Serialize(message);

            _configuration.DefaultLogger.Log("Inside call to GetConfiguration Rest service method");

            serializedMsg = ResponsePreparer.PrepareNoCacheWith(serializedMsg); // jsonp support

            return serializedMsg.AsStream();
        }
    }
}