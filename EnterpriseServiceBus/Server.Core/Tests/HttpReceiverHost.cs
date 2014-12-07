using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace GeoDecisions.Esb.Server.Core.Tests
{
    //http://pc65905:9050/test/msgReceiver

    public class HttpReceiverHost
    {
        // file writer used by test
        private static StreamWriter _writer;
        private ServiceHost _host;

        public HttpReceiverHost StartHttpReceiver(string baseUri, string logPath = null)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                logPath = Path.Combine(Directory.GetCurrentDirectory(), "esb-receiver.txt");
            }

            var baseAddresses = new[] {new Uri(baseUri)};

            var serviceHost = new WebServiceHost(typeof (HttpReceiver), baseAddresses);

            // hook events
            serviceHost.Closing += (sender, args) =>
                {
                    lock (_writer)
                    {
                        _writer.WriteLine(" ~~~~~~~~~~~~~~~~~~~~~ Stopping service ~~~~~~~~~~~~~~~~~~~~~");
                        _writer.WriteLine(" {0}", DateTime.UtcNow);
                    }
                };

            serviceHost.Closed += (sender, args) =>
                {
                    _writer.WriteLine(" ~~~~~~~~~~~~~~~~~~~~~ Stopped service  ~~~~~~~~~~~~~~~~~~~~~");
                    _writer.Flush();
                    _writer.Close();
                    _writer = null;
                };

            serviceHost.Opening += (sender, args) =>
                {
                    _writer = new StreamWriter(logPath, true);

                    lock (_writer)
                    {
                        _writer.WriteLine(" ~~~~~~~~~~~~~~~~~~~~~ Starting service ~~~~~~~~~~~~~~~~~~~~~");
                        _writer.WriteLine(" {0}", DateTime.UtcNow);
                    }
                };

            serviceHost.Opened += (sender, args) =>
                {
                    lock (_writer)
                    {
                        foreach (ServiceEndpoint addr in serviceHost.Description.Endpoints)
                        {
                            // log to a file
                            _writer.WriteLine("The service is ready at {0}", addr.Address);
                        }
                        _writer.WriteLine(" ~~~~~~~~~~~~~~~~~~~~~ Started service  ~~~~~~~~~~~~~~~~~~~~~");
                        _writer.Flush();
                    }
                };

            var webBusMetadata = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = false,
                    MetadataExporter = {PolicyVersion = PolicyVersion.Policy15},
                };

            serviceHost.Description.Behaviors.Add(webBusMetadata);

            var busRestBind = new WebHttpBinding(WebHttpSecurityMode.None);

            ServiceEndpoint busEndpoint = serviceHost.AddServiceEndpoint(typeof (IHttpReceiver), busRestBind, "rest");
            busEndpoint.Behaviors.Add(new WebHttpBehavior {AutomaticFormatSelectionEnabled = true});

            serviceHost.Open();

            _host = serviceHost;

            return this;
        }

        public void Stop()
        {
            _host.Close();
        }

        internal class HttpReceiver : IHttpReceiver
        {
            public string Receiver(Stream data)
            {
                // here is where we will get POSTed data from a subscription for http
                lock (_writer)
                {
                    // let's log it to a file
                    _writer.WriteLine(new StreamReader(data).ReadToEnd());
                    _writer.Flush();
                }

                return "ok";
            }
        }

        /// <summary>
        ///     REST interface for bus service
        /// </summary>
        [ServiceContract]
        internal interface IHttpReceiver
        {
            [OperationContract]
            [WebInvoke(UriTemplate = "/receiver")]
            string Receiver(Stream data);
        }
    }
}