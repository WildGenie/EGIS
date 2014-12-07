using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoDecisions.Esb.Server.Web.Models
{
    class MessageSearchParameters
    {
        public string Uri { get; set; }
        public int? Priority { get; set; }
        public string PublishedDate { get; set; }
        public string PublishedTime { get; set; }
        public DateTime PublishedDateTime
        {
            get
            {
                try
                {
                    return Convert.ToDateTime(PublishedDate + " " + PublishedTime,
                                              new DateTimeFormatInfo
                                              {
                                                  ShortDatePattern = "M/d/yyyy",
                                                  ShortTimePattern = "HH:mm",
                                                  AMDesignator = "AM"
                                              });
                }
                catch (Exception)
                {
                    return DateTime.Now;
                }
            }
        }

        public string ClientId { get; set; }
    }
}
