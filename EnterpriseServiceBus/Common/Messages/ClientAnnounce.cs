using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoDecisions.Esb.Common.Messages
{
    public class ClientAnnounce
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime LastPulse { get; set; }
    }
}
