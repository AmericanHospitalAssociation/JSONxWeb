#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Serialization;
using Avectra.netForum.Data;
using Avectra.netForum.Data.Interfaces;
using Avectra.netForum.xWeb.xWebSecure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace JSONxWeb
{
    using System.Xml.Linq;

    /// <summary>
    /// The jsonx web.
    /// </summary>
    [ScriptService]
    public class JSONxWeb : netForumXMLSecure
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JSONxWeb"/> class.
        /// </summary>
        public JSONxWeb()
        {
            var serviceMethodName = GetMethodName();
            InterceptJsonMethodRequest(serviceMethodName);
        }

        /// <summary>
        /// The get method name.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetMethodName()
        {
            return Context.Request.Url.Segments[Context.Request.Url.Segments.Length - 1];
        }

        /// <summary>
        /// The process message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void ProcessMessage(SoapMessage message)
        {
            ;
        }

        /// <summary>
        /// The intercept json method request.
        /// </summary>
        /// <param name="serviceMethodName">
        /// The service method name.
        /// </param>
        private void InterceptJsonMethodRequest(string serviceMethodName)
        {
            var service = GetType();
            var jsonMethod = service.GetMethod(serviceMethodName);
            if (jsonMethod == null) return;
            var jsonMethodParameters = jsonMethod.GetParameters();
            var callParameters = new object[jsonMethodParameters.Length];

            string jsonString;

            HttpContext.Current.Request.InputStream.Position = 0;
            using (var inputStream = new StreamReader(HttpContext.Current.Request.InputStream))
                jsonString = inputStream.ReadToEnd();

            var o = JObject.Parse(jsonString);

            var token = (string) o["token"];
            if (!string.IsNullOrEmpty(token))
                base.AuthToken.Token = token;

            for (var i = 0; i < jsonMethodParameters.Length; i++)
            {
                var targetParameter = jsonMethodParameters[i];
                var rawParameter = o[targetParameter.Name];

                // parameter filtering
                switch (targetParameter.ParameterType.Name)
                {
                    case "XmlNode":
                        callParameters[i] = JsonConvert.DeserializeXmlNode(rawParameter.ToString());
                        break;

                    case "Guid":
                        callParameters[i] = new Guid(rawParameter.ToString());
                        break;

                    default:
                        callParameters[i] = JsonDeserialize(rawParameter.ToString(), targetParameter.ParameterType);
                        break;
                }
            }

            LogRequest(serviceMethodName, callParameters);

            var jsonMethodReturnValue = jsonMethod.Invoke(this, callParameters);

            var rvalue = new Dictionary<string, object> {{"token", AuthToken.Token}};


            if (jsonMethodReturnValue is IXmlSerializable)
            {
                var builder = WriteXml(jsonMethodReturnValue);
                var xml = new XmlDocument();
                xml.LoadXml("<IXmlSerializable>" + builder + "</IXmlSerializable>");
                rvalue.Add("result", xml);
            }
            else
            {
                rvalue.Add("result", jsonMethodReturnValue);
            }

            Context.Response.Write(JsonSerialize(rvalue));
            Context.Response.Flush();
            Context.Response.End();
        }

        private void LogRequest(string serviceMethodName, object[] callParameters)
        {
            //using (var file = new System.IO.StreamWriter("c:\\temp\\JsonxWeb_log.txt", true))
            //{
            //    file.WriteLine(string.Format("--- {0} -----------------------------", DateTime.Now));

            //    file.WriteLine(string.Format("{0}: {1}", "Method:", serviceMethodName));

            //    foreach (var callParameter in callParameters)
            //    {
            //        var xml = callParameter as XmlDocument;
            //        if (xml != null)
            //        {
            //            file.WriteLine(string.Format("{0}: {1}", "Param:", PrettyXml(XmlDocumentToString(xml))));
            //        }
            //        else
            //            file.WriteLine(string.Format("{0}: {1}", "Param:", callParameter));
            //    }
            //}
        }

        static string XmlDocumentToString(XmlDocument xml)
        {
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xml.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        static string PrettyXml(string xml)
        {
            var stringBuilder = new StringBuilder();

            var element = XElement.Parse(xml);

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }


        /// <summary>
        /// The write xml.
        /// </summary>
        /// <param name="jsonMethodReturnValue">
        /// The json method return value.
        /// </param>
        /// <returns>
        /// The <see cref="StringBuilder"/>.
        /// </returns>
        private static StringBuilder WriteXml(object jsonMethodReturnValue)
        {
            var builder = new StringBuilder();
            var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    CloseOutput = false
                };
            var writer = XmlWriter.Create(builder, settings);
            ((IXmlSerializable) jsonMethodReturnValue).WriteXml(writer);
            writer.Flush();
            writer.Close();
            return builder;
        }

        /// <summary>
        /// The json serialize.
        /// </summary>
        /// <param name="serializationTarget">
        /// The serialization target.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string JsonSerialize(object serializationTarget)
        {
            return JsonConvert.SerializeObject(serializationTarget);
        }

        /// <summary>
        /// The json deserialize.
        /// </summary>
        /// <param name="datum">
        /// The datum.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        private static object JsonDeserialize(string datum, Type targetType)
        {
            try
            {
                if (typeof (IXmlSerializable).IsAssignableFrom(targetType))
                {
                    var obj = Activator.CreateInstance(targetType);
                    var xml = JsonConvert.DeserializeXmlNode(string.Format(@"{{ root:{0}}}", datum));

                    using (TextReader tr = new StringReader(XmlNodeToString(xml, 0)))
                    {
                        var xr = XmlReader.Create(tr);
                        ((IXmlSerializable) obj).ReadXml(xr);
                        return obj;
                    }
                }
                else
                {
                    var deserialize = JsonConvert.DeserializeObject(datum, targetType);
                    var json = JObject.Parse(datum);
                    var baseType = deserialize.GetType().BaseType;
                    if (baseType != null && typeof (FacadeClass).IsAssignableFrom(baseType))
                    {
                        if (string.IsNullOrEmpty(((FacadeClass) deserialize).CurrentKey) &&
                            !string.IsNullOrEmpty(json["CurrentKey"].ToString()))
                            ((FacadeClass) deserialize).CurrentKey = json["CurrentKey"].ToString();

                        if (!string.IsNullOrEmpty(((FacadeClass) deserialize).CurrentKey))
                            ((FacadeClass) deserialize).SelectByKey();

                        Fill((FacadeClass) deserialize, datum);
                    }
                    return deserialize;
                }
            }
            catch (Exception ex)
            {
                return datum;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="facade"></param>
        /// <param name="datum"></param>
        private static void Fill(IFacade facade, string datum)
        {
            var json = JObject.Parse(datum);

            foreach (var item in json)
            {
                if (item.Key.ToLower() == "currentkey") continue;

                var key = item.Key;
                var val = item.Value.ToString();

                if (val.StartsWith("{"))
                {
                    try
                    {
                        var jsonTwo = JObject.Parse(val);
                        Fill(facade, val);
                    }
                    catch (Exception)
                    {
                        ;
                    }
                }

                facade.SetValue(key, val);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="indentation"></param>
        /// <returns></returns>
        private static string XmlNodeToString(XmlNode node, int indentation)
        {
            using (var sw = new System.IO.StringWriter())
            {
                using (var xw = new XmlTextWriter(sw))
                {
                    xw.Formatting = System.Xml.Formatting.Indented;
                    xw.Indentation = indentation;
                    node.WriteContentTo(xw);
                }
                return sw.ToString();
            }
        }
    }
}