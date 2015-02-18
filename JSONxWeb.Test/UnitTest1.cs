#region

using System;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

#endregion

// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute

namespace JSONxWeb.Test
{
    [TestClass]
    public class UnitTest1
    {
        private const string XwebUserName = "xWebUser";
        private const string XwebUserPass = "111";
        private const string WebUserName = "q@q.com";
        private const string WebUserPass = "111";
        private const string CustomerKey = "6eec50a5-a742-4654-a78b-9b844eb21613";
        private const string APMKeyCreditCard = "181BB1A4-70E0-4D74-8B78-27DCC986F453";

        private JObject OrderEntry = null;

        //public string BaseUri = "http://dev2.ai.local:8888/xWeb/JSONxWeb.asmx";
        public string BaseUri = "http://localhost:49392/JSONxWeb.asmx";
        private string _authToken = string.Empty;

        public UnitTest1()
        {
            TestAuthenticate();
        }

        [TestMethod]
        public void TestWEBCentralizedShoppingCartMembershipOpenInvoiceAdd()
        {
            if (string.IsNullOrEmpty(_authToken))
                TestAuthenticate();

            OrderEntry = WEBCentralizedShoppingCartGet();

            var Invoice = WEBCentralizedShoppingCartMembershipOpenInvoiceGet();

            try
            {
                const string datum = @"{{
                              token:'{0}',
                              oCentralizedOrderEntry:{1},
                              oOpenInvoice:{2} 
                            }}";

                var payload = string.Format(datum, _authToken, OrderEntry, Invoice);
                var response = DoPost("WEBCentralizedShoppingCartMembershipOpenInvoiceAdd", payload);
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }
        }

        [TestMethod]
        public void TestWEBCentralizedShoppingCartGet()
        {
            OrderEntry = WEBCentralizedShoppingCartGet();
        }

        public JObject WEBCentralizedShoppingCartGet()
        {
            if (string.IsNullOrEmpty(_authToken))
                TestAuthenticate();
            JObject response = null;
            try
            {
                const string datum = @"{{
                              token:'{0}',
                              CustomerKey:'{1}'
                            }}";

                var payload = string.Format(datum, _authToken, CustomerKey);
                response = DoPost("WEBCentralizedShoppingCartGet", payload);
                _authToken = response["token"].ToString();
                Assert.IsTrue(response != null);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }

            return JObject.Parse(response["result"]["IXmlSerializable"].ToString());
        }

        [TestMethod]
        public void TestWEBCentralizedShoppingCartMembershipOpenInvoiceGet()
        {
            var response = WEBCentralizedShoppingCartMembershipOpenInvoiceGet();
        }

        public JObject WEBCentralizedShoppingCartMembershipOpenInvoiceGet()
        {
            var list = WEBCentralizedShoppingCartMembershipOpenInvoiceGetList();

            var InvoiceKey = list["result"]["Results"]["Result"][0]["inv_key"];

            if (string.IsNullOrEmpty(_authToken))
                TestAuthenticate();

            JObject response = null;

            try
            {
                const string datum = @"{{
                                          token:'{0}',
                                          Key:'{1}'
                                        }}";

                var payload = string.Format(datum, _authToken, InvoiceKey);
                response = DoPost("WEBCentralizedShoppingCartMembershipOpenInvoiceGet", payload);
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }

            return JObject.Parse(response["result"]["IXmlSerializable"].ToString()); ;
        }

        public JObject WEBCentralizedShoppingCartMembershipOpenInvoiceGetList()
        {
            if (string.IsNullOrEmpty(_authToken))
                TestAuthenticate();

            JObject response = null;

            try
            {
                const string datum = @"{{
                                          token:'{0}',
                                          CustomerKey:'{1}'
                                        }}";

                var payload = string.Format(datum, _authToken, CustomerKey);
                response = DoPost("WEBCentralizedShoppingCartMembershipOpenInvoiceGetList", payload);
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }

            return response;
        }

        [TestMethod]
        public void SetIndividualInformation()
        {
            if (string.IsNullOrEmpty(_authToken))
                TestAuthenticate();

            try
            {
                 const string datum = @"{{
                              token:'{0}',
                              IndividualKey:'{1}',
                              oUpdateNode: {{
                                datum: {{
                                   IndividualObject: {{
                                     ind_first_name: 'Jackson',
                                     adr_city: 'South Bend'
                                  }}
                                }}
                              }}
                            }}";

                var payload = string.Format(datum, _authToken, CustomerKey);
                var response = DoPost("SetIndividualInformation", payload);
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }
        }

        [TestMethod]
        public void TestWEBWebUserLogin()
        {
            try
            {
                var response = WEBWebUserLogin();
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32 && response["result"]["IXmlSerializable"]["CurrentKey"].ToString().Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }
        }

        public JObject WEBWebUserLogin()
        {
            var datum = string.Format("{{token:'{0}',LoginOrEmail:'{1}',password:'{2}'}}", _authToken, WebUserName, WebUserPass);
            return DoPost("WEBWebUserLogin", datum);
        }

        [TestMethod]
        public void TestAuthenticate()
        {
            try
            {
                var datum = string.Format("{{userName:'{0}',password:'{1}'}}", XwebUserName, XwebUserPass);
                var response = DoPost("Authenticate", datum);
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }
        }

        [TestMethod]
        public void TestConnection()
        {
            try
            {
                const string datum = @"{token:'xxx'}";
                var response = DoPost("TestConnection", datum);
                Assert.AreEqual("Success", response["result"]);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }
        }

        [TestMethod]
        public void TestGetIndividualInformation()
        {
            try
            {
                var response = GetIndividualInformation();
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32 && response["result"]["IXmlSerializable"]["CurrentKey"].ToString().Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }

            
        }

        public JObject GetIndividualInformation()
        {
            if (string.IsNullOrEmpty(_authToken))
                TestAuthenticate();

            JObject response = null;

            try
            {
                const string datum = @"{{
                                          token:'{0}',
                                          IndividualKey:'{1}'
                                        }}";

                var payload = string.Format(datum, _authToken, CustomerKey);
                response = DoPost("GetIndividualInformation", payload);
                _authToken = response["token"].ToString();
                Assert.IsTrue(_authToken.Length >= 32);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message + ex.InnerException);
            }

            return response;
        }


        public JObject DoPost(string method, string datum)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(BaseUri + "/" + method);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(datum);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();
                return JObject.Parse(responseText);
            }
        }
    }
}