//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.ServiceModel.Channels;
//using System.ServiceModel.Description;
//using System.ServiceModel.Dispatcher;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml;

//namespace Esb.Common.SharedMessages
//{
//    class BusMessageDispatcher
//    {

//    }

//    class NewtonsoftJsonDispatchFormatter : IDispatchMessageFormatter
//    {
//        OperationDescription operation;
//        Dictionary<string, int> parameterNames;

//        public NewtonsoftJsonDispatchFormatter(OperationDescription operation, bool isRequest)
//        {
//            this.operation = operation;

//            if (isRequest)
//            {
//                int operationParameterCount = operation.Messages[0].Body.Parts.Count;
//                if (operationParameterCount > 1)
//                {
//                    this.parameterNames = new Dictionary<string, int>();
//                    for (int i = 0; i < operationParameterCount; i++)
//                    {
//                        this.parameterNames.Add(operation.Messages[0].Body.Parts[i].Name, i);
//                    }
//                }
//            }
//        }

//        public void DeserializeRequest(Message message, object[] parameters)
//        {
//            object bodyFormatProperty;
//            if (!message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out bodyFormatProperty) ||
//                (bodyFormatProperty as WebBodyFormatMessageProperty).Format != WebContentFormat.Raw)
//            {
//                throw new InvalidOperationException("Incoming messages must have a body format of Raw. Is a ContentTypeMapper set on the WebHttpBinding?");
//            }

//            XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();
//            bodyReader.ReadStartElement("Binary");
//            byte[] rawBody = bodyReader.ReadContentAsBase64();
//            var ms = new MemoryStream(rawBody);

//            //var sr = new StreamReader(ms);

//            //Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
//            //var serializer = ServiceStack.Text.JsonSerializer;

//            if (parameters.Length == 1)
//            {
//                // single parameter, assuming bare
//                //parameters[0] = serializer.Deserialize(sr, operation.Messages[0].Body.Parts[0].Type);
//                parameters[0] = ServiceStack.Text.JsonSerializer.DeserializeFromStream(operation.Messages[0].Body.Parts[0].Type, ms);
//            }
//            else
//            {  
//                var sr = new StreamReader(ms);
//                // multiple parameter, needs to be wrapped
//                //Newtonsoft.Json.JsonReader reader = new Newtonsoft.Json.JsonTextReader(sr);

//                var fullString = sr.ReadToEnd();

//                var obj = ServiceStack.Text.JsonObject.Parse(fullString);

//                foreach (var paramKvp in this.parameterNames)
//                {
//                    var name = paramKvp.Key;

//                    obj.ContainsKey()

//                    string parameterName = reader.Value as string;
//                    int parameterIndex = this.parameterNames[paramName];
//                    parameters[parameterIndex] = serializer.Deserialize(reader, this.operation.Messages[0].Body.Parts[parameterIndex].Type);
//                }

//                //reader.Read();
//                //if (reader.TokenType != Newtonsoft.Json.JsonToken.StartObject)
//                //{
//                //    throw new InvalidOperationException("Input needs to be wrapped in an object");
//                //}

//                //reader.Read();
//                //while (reader.TokenType == Newtonsoft.Json.JsonToken.PropertyName)
//                //{
//                //    string parameterName = reader.Value as string;
//                //    reader.Read();
//                //    if (this.parameterNames.ContainsKey(parameterName))
//                //    {
//                //        int parameterIndex = this.parameterNames[parameterName];
//                //        parameters[parameterIndex] = serializer.Deserialize(reader, this.operation.Messages[0].Body.Parts[parameterIndex].Type);
//                //    }
//                //    else
//                //    {
//                //        reader.Skip();
//                //    }

//                //    reader.Read();
//                //}

//                //reader.Close();
//            }

//            sr.Close();
//            ms.Close();
//        }

//        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
//        {
//            byte[] body;
//            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
//            using (MemoryStream ms = new MemoryStream())
//            {
//                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
//                {
//                    using (Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sw))
//                    {
//                        writer.Formatting = Newtonsoft.Json.Formatting.Indented;
//                        serializer.Serialize(writer, result);
//                        sw.Flush();
//                        body = ms.ToArray();
//                    }
//                }
//            }

//            Message replyMessage = Message.CreateMessage(messageVersion, operation.Messages[1].Action, new RawBodyWriter(body));
//            replyMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
//            HttpResponseMessageProperty respProp = new HttpResponseMessageProperty();
//            respProp.Headers[HttpResponseHeader.ContentType] = "application/json";
//            replyMessage.Properties.Add(HttpResponseMessageProperty.Name, respProp);
//            return replyMessage;
//        }
//    }


//    public class BusHttpBehavior : WebHttpBehavior
//    {
//        //protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
//        //{
//        //    return base.GetRequestDispatchFormatter(operationDescription, endpoint);
//        //}

//        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
//        {
//            if (operationDescription.Messages.Count == 1 || operationDescription.Messages[1].Body.ReturnValue.Type == typeof(void))
//            {
//                return base.GetReplyDispatchFormatter(operationDescription, endpoint);
//            }
//            else
//            {
//                return new NewtonsoftJsonDispatchFormatter(operationDescription, false);
//            }
//        }
//    }
//}

