using System.IO;

namespace GeoDecisions.Esb.Common
{
    public interface ISerialization
    {
        string Serialize<T>(T obj);
        string Serialize(object obj);

        T Deserialize<T>(Stream stream);
        T Deserialize<T>(string str);
        object Deserialize(string str);
    }
}