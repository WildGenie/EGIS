using System;
using System.Runtime.Serialization;
using System.Xml;
using GeoDecisions.Esb.Common;

namespace GeoDecisions.Esb.Client.Core
{
    public class EsbSerializationResolver2 : DataContractResolver
    {
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            if (typeName == "ClientHelloMessage")
            {
                return typeof (EsbMessage);
            }

            return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            // we need to use our stored metadata here to get the type

            if (type.Name == "ClientHelloMessage")
            {
                var dictionary = new XmlDictionary();
                typeName = dictionary.Add("ClientHelloMessage");
                typeNamespace = dictionary.Add("http://tempuri.com");
                return true;
            }
            else
            {
                return knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
            }
        }
    }
}