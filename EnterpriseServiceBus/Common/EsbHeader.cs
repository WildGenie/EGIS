using System;

namespace GeoDecisions.Esb.Common
{
    public class EsbHeader //: IEsbHeader
    {
        public string Id { get; set; }

        public string ReplyTo
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int Priority
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int RetryAttempts
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        public string Uri
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}