using System.IO;
using GeoDecisions.Esb.Common;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Server.Core.Utility
{
    //public static class Serialization
    //{
    //    public static T DeserializeAs<T>(this string source)
    //    {
    //        //todo: use serialization interface for this
    //        return JsonSerializer.DeserializeFromString<T>(source);
    //    }
    //}


    public class JsonSerialization : ISerialization
    {
        public JsonSerialization()
        {
            JsConfig.EmitCamelCaseNames = true;
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
        }

        public string Serialize<T>(T obj)
        {
            return JsonSerializer.SerializeToString(obj);
        }

        public string Serialize(object obj)
        {
            return JsonSerializer.SerializeToString(obj);
        }

        public T Deserialize<T>(string str)
        {
            return JsonSerializer.DeserializeFromString<T>(str);
        }

        public object Deserialize(string str)
        {
            return JsonObject.Parse(str);
        }

        public T Deserialize<T>(Stream stream)
        {
            return JsonSerializer.DeserializeFromStream<T>(stream);
        }
    }
}