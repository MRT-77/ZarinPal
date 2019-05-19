using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace ZarinPal
{
    public static class Payment
    {
        private const string TestMerchantId = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
        private const int AuthorityLength = 36;

        public static PayResponse Request(string merchantId, long amount, string description, string callbackUrl)
        {
            bool sandBoxMode = merchantId.Equals(TestMerchantId);
            HttpCore httpCore = new HttpCore(Urls.GetPaymentRequestUrl(sandBoxMode), "POST",
                JsonConvert.SerializeObject(new PayRequest(merchantId, amount, description, callbackUrl)));
            var res = JsonConvert.DeserializeObject<PayResponse>(httpCore.GetResponse());
            res.Authority = res.Authority.TrimStart('0');
            return res;
        }

        public static PayVerifyResponse Verify(string merchantId, long amount, string authority)
        {
            string z = "";
            int count = AuthorityLength - authority.Length;
            for (int i = 0; i < count; i++) z += "0";
            authority = z + authority;

            bool sandBoxMode = merchantId.Equals(TestMerchantId);
            HttpCore httpCore = new HttpCore(Urls.GetVerificationUrl(sandBoxMode), "POST",
                JsonConvert.SerializeObject(new PayVerify(merchantId, amount, authority)));
            return JsonConvert.DeserializeObject<PayVerifyResponse>(httpCore.GetResponse());
        }

        public static string GetPaymentGatewayUrl(string merchantId, string authority)
        {
            bool sandBoxMode = merchantId.Equals(TestMerchantId);
            return Urls.GetPaymentGatewayUrl(authority, sandBoxMode);
        }

        internal class HttpCore
        {
            public HttpCore(string url, string method, string data)
            {
                Url = url;
                Method = method;
                Data = data;
            }

            public string Url { get; set; }
            public string Method { get; set; }
            public string Data { get; set; }

            public string GetResponse()
            {
                var webRequest = WebRequest.CreateHttp(Url);
                webRequest.Method = Method;
                if (Method.Equals("POST", StringComparison.CurrentCultureIgnoreCase))
                {
                    webRequest.ContentType = "application/json";
                    using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                    {
                        streamWriter.Write(Data);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                }

                Stream responseStream = null;
                try
                {
                    var webResponse = webRequest.GetResponse();
                    responseStream = webResponse.GetResponseStream();
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                        responseStream = e.Response.GetResponseStream();
                }
                if (responseStream == null) return null;

                string result;
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    result = streamReader.ReadToEnd();
                    streamReader.Close();
                }
                return result;
            }
        }

        internal static class Urls
        {
            private const string PaymentReqUrl = "https://{0}zarinpal.com/pg/rest/WebGate/PaymentRequest.json";
            private const string PaymentPgUrl = "https://{0}zarinpal.com/pg/StartPay/{1}/ZarinGate";
            private const string PaymentVerificationUrl = "https://{0}zarinpal.com/pg/rest/WebGate/PaymentVerification.json";
            private const string SandBox = "sandbox.";
            private const string Www = "www.";

            public static string GetPaymentRequestUrl(bool sandBoxMode = false)
            {
                return string.Format(PaymentReqUrl, sandBoxMode ? SandBox : Www);
            }

            public static string GetPaymentGatewayUrl(string authority, bool sandBoxMode = false)
            {
                return string.Format(PaymentPgUrl, sandBoxMode ? SandBox : Www, authority);
            }

            public static string GetVerificationUrl(bool sandBoxMode = false)
            {
                return string.Format(PaymentVerificationUrl, sandBoxMode ? SandBox : Www);
            }
        }

        internal class PayVerify
        {
            public PayVerify(string merchantId, long amount, string authority)
            {
                MerchantID = merchantId;
                Amount = amount;
                Authority = authority;
            }

            public string MerchantID { get; set; }
            public long Amount { get; set; }
            public string Authority { get; set; }
        }

        internal class PayRequest
        {
            public PayRequest(string merchantId, long amount, string description, string callbackUrl)
            {
                MerchantID = merchantId;
                Amount = amount;
                Description = description;
                CallbackURL = callbackUrl;
            }

            public string MerchantID { get; set; }
            public long Amount { get; set; }
            public string Description { get; set; }
            public string CallbackURL { get; set; }
            public string Mobile { get; set; }
            public string Email { get; set; }
        }
    }

    public class PayVerifyResponse
    {
        public bool IsSuccess => Status == 100 || Status == 101;
        public string RefID { get; set; }
        public int Status { get; set; }
        public object Errors { get; set; }
        public string ErrorsStr => JsonConvert.SerializeObject(Errors);
    }

    public class PayResponse
    {
        public bool IsSuccess => Status == 100 || Status == 101;
        public string Authority { get; set; }
        public int Status { get; set; }
        public object Errors { get; set; }
        public string ErrorsStr => JsonConvert.SerializeObject(Errors);
    }
}