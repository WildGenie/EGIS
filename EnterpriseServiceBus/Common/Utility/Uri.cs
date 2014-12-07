using System.Collections.Specialized;

namespace GeoDecisions.Esb.Common.Utility
{
    public static class UriExtensions
    {
        public static NameValueCollection GetQueryString(string uri)
        {
            var queryParameters = new NameValueCollection();
            string qsOnly = uri;
            string[] step1 = uri.Split('?');

            if (step1.Length > 1)
                qsOnly = step1[1];

            qsOnly = qsOnly.TrimStart('?');

            string[] querySegments = qsOnly.Split('&');

            foreach (string segment in querySegments)
            {
                string[] parts = segment.Split('=');
                if (parts.Length > 0)
                {
                    string key = parts[0].Trim(new[] {'?', ' '});
                    string val = parts[1].Trim();

                    queryParameters.Add(key, val);
                }
            }

            return queryParameters;
        }
    }
}