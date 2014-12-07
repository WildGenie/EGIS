using System;
using System.Net;
using System.ServiceModel.Web;

namespace GeoDecisions.Esb.Server.Core.Utility
{
    public class ResponsePreparer
    {
        public const string APP_JAVASCRIPT = "application/javascript";
        public const string APP_JSON = "application/json";
        public const string TEXT_PLAIN = "text/plain";
        public const string IMAGE_PNG = "image/png";
        public const string IMAGE_JPG = "image/jpeg";

        private const string EXPIRES_HEADER = "expires";
        private const string DATE_HEADER = "date";
        private const string CACHE_CONTROL_HEADER = "Cache-Control";
        private const string NO_CACHE = "no-cache, must-revalidate";
        private const string NO_STORE = "no-store";
        private const string PUBLIC_CACHE = "public";
        private const string MAX_AGE_MUST_REVAL = "max-age={0}, must-revalidate";
        private const string EXPIRED_DATE = "Mon, 26 Jan 1997 01:00:00 GMT";

        public static string PrepareNoCacheWith(string response)
        {
            // try to sniff headers/querystrting
            WebOperationContext.Current.OutgoingResponse.ContentType = TEXT_PLAIN;

            string callback = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["callback"];

            if (!string.IsNullOrEmpty(callback))
            {
                // json p stuff
                WebOperationContext.Current.OutgoingResponse.ContentType = APP_JAVASCRIPT;

                if (!string.IsNullOrEmpty(response))
                {
                    response = string.Format("{0}({1});", callback, response);
                }
            }

            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_CACHE);

            return response;
        }

        public static void PrepareNoStore()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = TEXT_PLAIN;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_STORE);
        }

        //public static bool TryPrepareCachedParseMime(int? expiresSeconds = null, int? expiresMinutes = null)
        //{
        //    var time = DateTime.UtcNow;

        //    if (expiresSeconds.HasValue)
        //        time.AddSeconds(expiresSeconds.Value);

        //    if (expiresMinutes.HasValue)
        //        time.AddMinutes(expiresMinutes.Value);

        //    return TryPrepareCachedParseMime(time);
        //}

        //public static bool TryPrepareNoCachedParseMime()
        //{
        //    var mimeType = string.Empty;

        //    if (!QueryStringUtil.GetStringFromQueryString("gearsFmt", out mimeType))
        //        return false;

        //    WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
        //    WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
        //    WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_CACHE);

        //    return true;
        //}

        //public static bool TryPrepareCachedParseMime(DateTime expiresUtc)
        //{
        //    var mimeType = string.Empty;

        //    if (!QueryStringUtil.GetStringFromQueryString("gearsFmt", out mimeType))
        //        return false;

        //    var now = DateTime.Now.ToUniversalTime();

        //    var diff = expiresUtc - now;
        //    var seconds = (int)diff.TotalSeconds;

        //    string rfc1123 = expiresUtc.ToString("R");

        //    WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
        //    WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, rfc1123);
        //    //WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, PUBLIC_CACHE);
        //    WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, string.Format(MAX_AGE_MUST_REVAL, seconds));

        //    return true;
        //}

        public static void PrepareCached(int? expiresSeconds = null, int? expiresMinutes = null, string contentType = TEXT_PLAIN)
        {
            DateTime time = DateTime.UtcNow;

            if (expiresSeconds.HasValue)
                time.AddSeconds(expiresSeconds.Value);

            if (expiresMinutes.HasValue)
                time.AddMinutes(expiresMinutes.Value);

            PrepareCached(time, contentType);
        }

        public static void PrepareCached(DateTime expires, string contentType = TEXT_PLAIN)
        {
            DateTime now = DateTime.UtcNow;

            TimeSpan diff = expires - now;
            var seconds = (int) diff.TotalSeconds;

            string rfc1123 = expires.ToString("R");

            WebOperationContext.Current.OutgoingResponse.ContentType = contentType;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, rfc1123);
            //WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, PUBLIC_CACHE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, string.Format(MAX_AGE_MUST_REVAL, seconds));
        }

        public static void PrepareNoCache(string mimeType)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_CACHE);
        }

        public static void PrepareNoCache(string mimeType, long length)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
            WebOperationContext.Current.OutgoingResponse.ContentLength = length;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_CACHE);
        }

        public static void PrepareNoStore(string mimeType)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_STORE);
        }

        public static void PrepareNoStore(string mimeType, long length)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
            WebOperationContext.Current.OutgoingResponse.ContentLength = length;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(EXPIRES_HEADER, EXPIRED_DATE);
            WebOperationContext.Current.OutgoingResponse.Headers.Add(CACHE_CONTROL_HEADER, NO_STORE);
        }

        public static void PrepareError(int errorCode = 500)
        {
            PrepareNoCache(TEXT_PLAIN);
            if (errorCode == 400)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
            }
            else if (errorCode == 404)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
        }

        public static void AddStatusCode(HttpStatusCode code)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = code;
        }
    }
}