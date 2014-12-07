namespace GeoDecisions.Esb.Common.Utility
{
    public static class Serialization
    {
        public static T DeserializeAs<T>(this string source)
        {
            //todo: use serialization interface for this
            return default(T);
        }
    }
}