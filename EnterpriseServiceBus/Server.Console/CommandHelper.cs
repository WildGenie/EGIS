using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Services;
using GeoDecisions.Esb.Server.Core;
using ServiceStack.Text;
using Syscon = System.Console;
using GeoDecisions.Esb.Server.Core.Utility;
using System.Runtime.Serialization;
using GeoDecisions.Esb.Common.Storage;


namespace GeoDecisions.Esb.Server.Console
{
    //TODO This class is added solely for the purpose of deserializing the message, there is an identical class in Client.Console.Messages, consider relocating this class to the common namespace
    //so that it can be reused here
    //internal class MessageBase : IEsbMessage
    //{
    //    public string Id { get; set; }
    //    public string Uri { get; set; }
    //    public string ReplyTo { get; set; }
    //    public int Priority { get; set; }
    //    public int RetryAttempts { get; set; }
    //    public ExtensionDataObject ExtensionData { get; set; }
    //}

    //TODO This class is added solely for the purpose of deserializing the message, there is an identical class in Client.Console.Messages, consider relocating this class to the common namespace
    //so that it can be reused here
    internal class GenericMessage //: MessageBase
    {
        public GenericMessage(string uri, string text)
        {
            this.Uri = uri;
            this.Text = text;
        }

        public string Uri { get; set; }
        public string Text { get; set; }
    }


    class CommandHelper
    {

        private static readonly string[] aCommands = {
                                                "info commands:  all the available commands", 
                                                "info serverstats: ESB version, start time, subscriber count and messages count since uptime", 
                                                "info subscribers: current subscribers", 
                                                "info stat <author>/<category>/<messageName>: Msg Datetimes, publishers and subscribers", 
                                                "info activity -m <m minutes>: all the activities in the last m minutes", 
                                                "info messages -d <d days>: messages that were sent in the last d day",  
                                                "info messages -h <h hours>: messages that were sent in the last h hours",  
                                                "info messages -m <m minutes>: messages that were sent in the last m minutes",  
                                                "info messages -s <s seconds>: messages that were sent in the last s seconds",
                                                "info badMessages: invalid messages and/or messages that could not be delivered", 
                                                "info categories: Lists current categories for messages", 
                                                "info publishers -m <m minutes>: publishers that have published messages",
                                                "shutdown: stops the server",
                                                "clear: clears the console",
                                                "restart: clears the console and restarts the server"
                                            };

        public static void PrintHeader(string msg)
        {
            Syscon.ForegroundColor = ConsoleColor.Cyan;
            Syscon.WriteLine(msg);
            Syscon.ForegroundColor = ConsoleColor.White;
        }

        private static void PrintFooter(string msg)
        {
            Syscon.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine(msg);
            Syscon.ForegroundColor = ConsoleColor.White;
        }


        public static void Usage()
        {
            PrintHeader("usage <info commands>");
            PrintFooter("ok");
        }

        public static void Restart()
        {
            EsbConsole.Messages.Clear();
            PrintHeader("Messages cleared...");
            EsbConsole.Subscribers.Clear();
            PrintHeader("Subscriber list cleared...");
            EsbConsole.Publishers.Clear();
            PrintHeader("Publisher list cleared...");
            EsbConsole.Restart();
            PrintHeader("Server restarted...");
            GetServerStats();
            PrintFooter("ok");
        }

        public static void InitStorage()
        {
            PrintHeader("initializing storage...");
            //TODO should not need to do this here, this should already be done when the bus is started, remove it
            StorageManager.Init();
            PrintFooter("ok");
        }

        public static void GetServerCommands()
        {
            PrintHeader("All the available commands");
            //List<string> s = new List<string>();
            
            //MongoStore store = new Common.Storage.MongoStore();
            //List<string> s = store.GetCommands();
            //var s = MongoStore.GetCommands();
            var s = StorageManager.GetServerCommands();

            foreach (var cmd in s)
            {
                Syscon.WriteLine(cmd);
                //s.Add(cmd);
            }
            PrintFooter("ok");
            //return s;
        }

        public static void GetDbStats()
        {
            PrintHeader("Priority and availability of server databases");
            StorageManager.DbPrioritiesAndAvailability();
            PrintFooter("ok");
        }

        public static void GetTransporterStats(Bus bus)
        {
            PrintHeader("Enabled Server Transporters");
            var transporters = bus.GetTransporterStats();
            if (transporters != null)
            {
                transporters.ForEach(t => Syscon.WriteLine(t));
            }
            PrintFooter("ok");
        }

        public static void GetServerStats()
        {
            PrintHeader("Server Statistics");
            foreach (var cmd in EsbConsole.getStat())
            {
                Syscon.WriteLine(cmd);
                //s.Add(cmd);
            }

            PrintFooter("ok");
            //return EsbConsole.getStat(); //TODO address this
        }


        public static void GetMessagesForSeconds(int n)
        {
            //List<string> s = new List<string>();

            if (n == 0) n = 600;

            PrintHeader("Messages in the last " + n + " seconds");

            int count = 0;

            foreach (EsbMessage msg in EsbConsole.Messages)
            {
                //s.Add(msg.ToJson());
                double seconds = (DateTime.Now - msg.PublishedDateTime).TotalSeconds;

                if (seconds < n)
                {
                    count++;

                    Syscon.WriteLine(
                        "Id: " + msg.Id + Environment.NewLine +
                        "Datetime: " + msg.PublishedDateTime + Environment.NewLine +
                        "Retry Attempts: " + msg.RetryAttempts + Environment.NewLine +
                        "Reply To: " + msg.ReplyTo + Environment.NewLine +
                        "Uri: " + msg.Uri + Environment.NewLine +
                        "Type: " + msg.Type + Environment.NewLine +
                        "Payload: " + msg.Payload.ToJson() + Environment.NewLine
                        );
                }
            }

            PrintFooter("Total number of messages published in the last " + n + " seconds is " + count);
            PrintFooter("ok");
            //return s;
        }

        public static void GetMessagesForMinutes(int n)
        {
            GetMessagesForSeconds(n * 60);
        }

        public static void GetMessagesForHours(int n)
        {
            GetMessagesForMinutes(n * 60);
        }

        public static void GetMessagesForDays(int n)
        {
            GetMessagesForHours(n * 24);
        }

        public static void GetSubscribers()
        {
            //List<string> s = new List<string>();

            PrintHeader("Subscribers");

            foreach (ISubscriber sub in EsbConsole.Subscribers)
            {
                //s.Add(sub.ToJson());
                Syscon.WriteLine(  
                        "Type: " + sub.GetType() + Environment.NewLine +
                        "Uri: " + sub.Uri + Environment.NewLine +
                        "Machine: " + sub.MachineName + Environment.NewLine +
                        "ClientId: " + sub.ClientId + Environment.NewLine
                     );
            }

            PrintFooter("ok");
            //return s;
        }

        public static void GetPublishers()
        {

            PrintHeader("Publishers");

            foreach (IPublisher pub in EsbConsole.Publishers)
            {
                //s.Add(sub.ToJson());
                Syscon.WriteLine(
                        "Machine: " + pub.MachineName + Environment.NewLine
                     );
            }

            PrintFooter("ok");
        }


        public static void AllUsers()
        {
            //List<string> s = new List<string>();

            PrintHeader("All Users");

            //Add subscribers
            PrintHeader("Subscribers");
            foreach (ISubscriber sub in EsbConsole.Subscribers)
            {
                Syscon.WriteLine("Machine: " + sub.MachineName + Environment.NewLine);
            }

            //Add Publishers
            PrintHeader("Publishers");
            foreach (IPublisher pub in EsbConsole.Publishers)
            {
                Syscon.WriteLine("Machine: " + pub.MachineName + Environment.NewLine);
            }

            PrintFooter("ok");
            //return s;
        }

        public static void ChannelStatistics(string Uri)
        {
            //List<string> s = new List<string>();

            PrintHeader("Messages sent to " + Uri);

            foreach (EsbMessage msg in EsbConsole.Messages)
            {
                if (msg.Uri == Uri)
                {
                    GenericMessage genericMsg = JsonSerializer.DeserializeFromString<GenericMessage>(new System.IO.StreamReader(new System.IO.MemoryStream(msg.Payload), true).ReadToEnd());

                    Syscon.WriteLine(  
                            msg.GetType() + Environment.NewLine +
                            msg.PublishedDateTime + Environment.NewLine +
                            genericMsg.Text + Environment.NewLine +
                            new System.IO.StreamReader(new System.IO.MemoryStream(msg.Payload), true).ReadToEnd() + Environment.NewLine +
                            msg.Payload.ToJson() + Environment.NewLine
                         );
                }
            }

            PrintHeader("Machines subscribing to " + Uri);

            foreach (ISubscriber sub in EsbConsole.Subscribers)
            {
                if (sub.Uri == Uri)
                {
                    Syscon.WriteLine(sub.MachineName + Environment.NewLine);
                }
            }

            PrintFooter("ok");
            //return s;
        }
    }
}
