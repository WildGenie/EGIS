using System.IO;
using System.Text;

namespace GeoDecisions.Esb.Server.Core.Utility
{
    public static class StringExtensions
    {
        public static Stream AsStream(this string instance, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var memStream = new MemoryStream();
            var writer = new StreamWriter(memStream, encoding);
            writer.Write(instance);
            writer.Flush();
            memStream.Position = 0;
            return memStream;
        }
    }
}