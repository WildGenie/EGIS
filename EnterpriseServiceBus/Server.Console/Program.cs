using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Server.Core;
using GeoDecisions.Esb.Server.Core.Tests;
using CommandLine;
using CommandLine.Text;
using System.Text.RegularExpressions;
using GeoDecisions.Esb.Server.Core.Transporters;
using GeoDecisions.Esb.Server.Core.Transporters.Http;


namespace GeoDecisions.Esb.Server.Console
{
    internal class Program
    {
        private const string ServerConfigFile = "EsbServer.config.xml";

        private static void Main(string[] args)
        {
            // stand up http receiver for testing
            // We will use this as our endpoint for our replyTo for http subscriptions
            //var receiverHost = new HttpReceiverHost();
            //var testHost = receiverHost.StartHttpReceiver("http://pc68700/test");

            // configure server
            Configuration config = new Configuration().AddLogger(new ServerLogger()).Compile();

            var c = new EsbConfiguration();
            try
            {
                c = Configuration.ReadFile(ServerConfigFile);
                config.DefaultLogger.Log("Config file read " + ServerConfigFile + "  was read");
            }
            catch (Exception ex)
            {
                config.DefaultLogger.Log("Config file read " + ServerConfigFile + "  could not be read");
                config.DefaultLogger.LogError(ex.Message);
            }

            //config.SetServerHost(c.MainHost)

            config.ConfigureTransporters(t =>
            {
                t.Enabled = false;

                if (t.Name == Names.RedisTransporter)
                {
                    t.Enabled = c.TransporterConfig(t.Name).enabled == "true";
                    t.Configure(new RedisConfig() { Host = c.TransporterConfig(t.Name).host, Port = Convert.ToInt32(c.TransporterConfig(t.Name).port) });
                }
                else if (t.Name == Names.WcfTcpTransporter)
                {
                    t.Enabled = c.TransporterConfig(t.Name).enabled == "true";
                    t.Configure(new WcfTcpConfig() { Host = c.TransporterConfig(t.Name).host, Port = Convert.ToInt32(c.TransporterConfig(t.Name).port) });
                }
                else if (t.Name == Names.HttpTransporter)
                {
                    t.Enabled = c.TransporterConfig(t.Name).enabled == "true";
                    t.Configure(new HttpConfig() { Host = c.TransporterConfig(t.Name).host, Port = Convert.ToInt32(c.TransporterConfig(t.Name).port) });
                }
                config.DefaultLogger.Log(t.Name + " configured to " + t.Enabled + ", Host:" + t.Host + ", Port:" + t.Port );
            });
            
            config.ConfigureDatabases(d =>
            {
                d.IsAvailable = true;

                if (d.Name == Names.Oracle)
                {
                    d.Priority = Convert.ToInt32(c.DatabaseConfig(d.Name).priority);
                    d.Configure(new OracleConfig() { Host = c.DatabaseConfig(d.Name).host, ConnectString = c.DatabaseConfig(d.Name).connectString });
                }
                else if (d.Name == Names.Mongo)
                {
                    d.Priority = Convert.ToInt32(c.DatabaseConfig(d.Name).priority);
                    d.Configure(new MongoConfig() { Host = c.DatabaseConfig(d.Name).host, ConnectString = c.DatabaseConfig(d.Name).connectString, Database = c.DatabaseConfig(d.Name).database });
                }
                else if (d.Name == Names.DefaultStore)
                {
                    d.Priority = 10;
                }

                config.DefaultLogger.Log(d.Name + " configured to priority " + d.Priority);
            });

            var esb = new Bus(config);

            // events
            esb.OnMessage += s => { };
            esb.OnError += s => { };
            esb.OnSubscribe += s => { };
            esb.OnUnsubscribe += s => { };

            esb.Start();
            //esb.StartAndWait();

            var esbConsole = new EsbConsole(esb);

            System.Console.WriteLine("Press <Esc> to exit.");

            bool run = true;

            while (run)
            {
                try
                {
                    System.Console.Write("esb>");

                    var line = System.Console.ReadLine();
                    line = Regex.Replace(line, " {2,}", " ");

                    if (string.IsNullOrEmpty(line))
                        continue;

                    //var cmd = line.Split(' ').FirstOrDefault().ToLower();
                    string[] aCmd = line.Split(' ');


                    switch (aCmd[0].ToLower())
                    {
                        case "?":
                        case "help":
                            CommandHelper.Usage();
                            break;
                        case "info":
                            // Parse the tokens after the first two tokens
                            if (aCmd.Count() > 1)
                            {
                                switch (aCmd[1].ToLower())
                                {
                                    case "commands":
                                        CommandHelper.GetServerCommands();
                                        break;

                                    case "serverstats":
                                        CommandHelper.GetServerStats();
                                        break;

                                    case "storage":
                                        CommandHelper.GetDbStats();
                                        break;

                                    case "transporters":
                                        CommandHelper.GetTransporterStats(esb);
                                        break;

                                    case "users": //List the machines -  Since the communication will be between machine to machine
                                        CommandHelper.AllUsers();
                                        break;

                                    case "messages":

                                        //Process message command the highest time denomination will be used, the others will be ignored
                                        var options = new Options();
                                        CommandLine.Parser.Default.ParseArguments(aCmd, options);
                                        if (options.days != 0)
                                        {
                                            CommandHelper.GetMessagesForDays(options.days);
                                        }
                                        else if (options.hours != 0)
                                        {
                                            CommandHelper.GetMessagesForHours(options.hours);
                                        }
                                        else if (options.minutes != 0)
                                        {
                                            CommandHelper.GetMessagesForMinutes(options.minutes);
                                        }
                                        else //if (options.seconds != 0)
                                        {
                                            CommandHelper.GetMessagesForSeconds(options.seconds);
                                        }
                                        break;

                                    case "subscribers":
                                        CommandHelper.GetSubscribers();
                                        break;

                                    case "publishers":
                                        CommandHelper.GetPublishers();
                                        break;

                                    case "stat":
                                        CommandHelper.ChannelStatistics(aCmd[2]);
                                        break;

                                    case "bandwidth":
                                        System.Console.WriteLine("Total Message Bandwidth (mb): {0}", esb.MessagesBandwidthInMb);
                                        break;
                                   
                                    default:
                                        CommandHelper.Usage();
                                        break;
                                }
                            }
                            else
                            {
                                CommandHelper.Usage();
                                break;
                            }
                            break;

                        case "initstorage":
                            CommandHelper.InitStorage();
                            break;

                        case "clear":
                            System.Console.Clear();
                            break;

                        case "restart":
                            System.Console.WriteLine("Are you sure you want to restart the server (Y/N):");
                            System.ConsoleKeyInfo k = System.Console.ReadKey();
                            if (k.Key == ConsoleKey.Y)
                            {
                                System.Console.Clear();
                                CommandHelper.Restart();
                            }
                            break;

                        case "start":
                            System.Console.Clear();
                            break;

                        case "quit":
                            run = false;
                            break;

                        case "shutdown":
                            run = false;
                            break;

                        case "stat":
                            esbConsole.PrintStat();
                            break;

                        case "show":
                            esbConsole.Show(line);
                            break;

                        case "error":
                            esb.Error();
                            break;

                        default:
                            CommandHelper.Usage();
                            break;

                    }

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
            }

            //broadcast test
            //esb.Broadcast("Hello all!");

            esb.Stop();

            esb.FlushCache();

            // stop test host
            //testHost.Stop();

            //Thread.Sleep(100);

            System.Console.ReadKey();
        }
    }
}