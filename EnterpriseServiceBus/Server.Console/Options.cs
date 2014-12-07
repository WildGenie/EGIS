using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace GeoDecisions.Esb.Server.Console
{
    class Options
    {
        [Option('m', "minutes", DefaultValue = 0, HelpText = "This is an option!")]
        public int minutes { get; set; }

        [Option('s', "seconds", DefaultValue = 0, HelpText = "This is an option!")]
        public int seconds { get; set; }

        [Option('h', "hours", DefaultValue = 0, HelpText = "This is an option!")]
        public int hours { get; set; }

        [Option('d', "days", DefaultValue = 0, HelpText = "This is an option!")]
        public int days { get; set; }
    }
}
