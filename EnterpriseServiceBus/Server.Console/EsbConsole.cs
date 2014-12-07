using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoDecisions.Esb.Server.Core;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using Syscon = System.Console;

namespace GeoDecisions.Esb.Server.Console
{
    /// <summary>
    /// This is the main helper class for driving our Console server.  It handles input, output, and other command line tasks.
    /// </summary>
    public class EsbConsole
    {
        //Changed this to a static, rxs, 04/10/2013
        private static Bus _bus;

        public EsbConsole(Bus bus)
        {
            _bus = bus;
        }

        public static List<EsbMessage> Messages
        {
            get
            {
                return _bus.Messages;
            }
        }

        public static List<ISubscriber> Subscribers
        {
            get
            {
                return _bus.Subscribers;
            }
        }

        public static List<IPublisher> Publishers
        {
            get
            {
                return _bus.Publishers;
            }
        }


        public void PrintStat()
        {
            Syscon.WriteLine("");
            //Syscon.WriteLine("----------------------------------------------------------------");
            Syscon.WriteLine("server stats:");
            Syscon.WriteLine("  # subs: {0}", _bus.SubscriberCount);
            Syscon.WriteLine("  # msgs: {0}", 1);
            //Syscon.WriteLine("----------------------------------------------------------------");
            Syscon.WriteLine("");
            
        }

        //Added this function to return the serverstats, rxs, 04/10/2013
        public static List<string> getStat()
        {
            List<string> stats = new List<string>();
            stats.Add("server stats:");
            stats.Add("Starttime: " + _bus.StartDateTime);
            stats.Add("  # subs: " + _bus.SubscriberCount);
            stats.Add("  # msgs: " + _bus.Messages.Count());
            return stats;
        }

        public static void Restart()
        {
            _bus.Restart();

            //_bus.Start();
        }

        internal void Show(string line)
        {
            var lineInfo = line.Split(' ');

            if (lineInfo.Count() == 1)
            {
                // show usage
                Syscon.WriteLine("usage: show <item> <params>");
                return;
            }

            var showItem = lineInfo[1];

            if (showItem == "sub")
            {
                if (lineInfo.Count() != 3)
                {
                    // show usage
                    Syscon.WriteLine("usage: show <item> <params>");
                    return;
                }

                // get message uri
                var msgUri = lineInfo[2];

                //var subInfo = _bus.SubscriptionInfo(msgUri);
           
            Syscon.WriteLine("");
            //Syscon.WriteLine("----------------------------------------------------------------");
            Syscon.WriteLine("show: {0} with uri {1}", showItem, msgUri);
            Syscon.WriteLine("  # subs: {0}", _bus.SubscriberCount);
            Syscon.WriteLine("  # msgs: {0}", 1);
            //Syscon.WriteLine("----------------------------------------------------------------");
            Syscon.WriteLine("");
            }

        }
    }
}
