using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Client.Core;

namespace EsbClientConsole2
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine(" ~ Esb Client ~ ");

            System.Console.WriteLine("Esb server (<enter> for localhost):");

            string server = System.Console.ReadLine();

            if (string.IsNullOrEmpty(server))
                server = "localhost";

            System.Console.WriteLine("Esb comhandler (r)edis, (t)cp, (h)ttp, (<enter> for tcp):");

            string comhandler = System.Console.ReadLine();

            if (string.IsNullOrEmpty(comhandler))
                comhandler = "t";

            var config = new Configuration(server, 9911, name: "Test Console Client", description: "Client for testing dll.");

            var bus = new Bus(config);

            config.DefaultLogger.Log("Console target test from logger");

            System.Console.WriteLine("sub, usub, pub, priority, quit");

            bool run = true;
            while (run)
            {
                System.Console.Write("esb>>");

                var line = System.Console.ReadLine();

                if (string.IsNullOrEmpty(line))
                    continue;

                var cmd = line.Split(' ').FirstOrDefault();

                switch (cmd)
                {
                    case "quit":
                        bus.Shutdown();
                        run = false;
                        break;

                    case "priority":
                        bus.ComHandlerPriorities();
                        break;

                    case "sub":
                        var subcmd = line.Split(' ');

                        if (subcmd.Length != 2)
                        {
                            System.Console.WriteLine("");
                            System.Console.WriteLine("usage: sub uri");
                            config.DefaultLogger.Log("Invalid sub command");
                            break;
                        }

                        bus.Subscribe<AddressMessage>(subcmd[1], (message, defaultMsg) =>
                        {
                            if (message != null && message.Address1 != null)
                            {
                                System.Console.WriteLine("client1 got the Address msg: " + message.Address1 + ", " + message.Address2 + ", " + message.Type);
                            }
                            else
                            {
                                System.Console.WriteLine("client1 got generic msg: " + defaultMsg.Uri + ":" + Encoding.UTF8.GetString(defaultMsg.Payload));
                            }
                        });

                        System.Console.WriteLine("ok");
                        break;

                    case "usub":
                        var usubcmd = line.Split(' ');

                        if (usubcmd.Length != 2)
                        {
                            System.Console.WriteLine("");
                            System.Console.WriteLine("usage: usub uri");
                            config.DefaultLogger.Log("Invalid usub command");
                            break;
                        }

                        bus.UnSubscribe(usubcmd[1]);

                        System.Console.WriteLine("ok");
                        break;

                    case "pub":
                        var pubcmd = line.Split(' ');
                        if (pubcmd.Length != 3)
                        {
                            System.Console.WriteLine("");
                            System.Console.WriteLine("usage: pub uri message");
                            break;
                        }

                        bus.Publish(new GenericMessage(pubcmd[1], pubcmd[2]), pubcmd[1]);
                        System.Console.WriteLine("ok");
                        break;

                    case "pub1":
                        var pubcmd1 = line.Split(' ');
                        if (pubcmd1.Length != 3)
                        {
                            System.Console.WriteLine("");
                            System.Console.WriteLine("usage: pub1 uri message");
                            break;
                        }

                        bus.Publish(new AddressMessage("5795 Fawn meadow lane", "Enola", "Addr"), pubcmd1[1]);
                        System.Console.WriteLine("ok");
                        break;
                }
            }

            bus.WaitForShutdown();

            System.Console.Write("Shutting down... ");

            //bus.Shutdown();

            System.Console.WriteLine("done!");

            System.Console.ReadLine();

        }
    }

    internal class GenericMessage
    {
        public string Uri { get; set; }
        public string Payload { get; set; }

        public GenericMessage(string uri, string text)
        {
            this.Uri = uri;
            this.Payload = text;
        }
    }

    internal class AddressMessage
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Type { get; set; }

        public AddressMessage(string address1, string address2, string type)
        {
            Address1 = address1;
            Address2 = address2;
            Type = type;
        }
    }
}
