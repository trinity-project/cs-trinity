using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Neo.IO.Json;
using Trinity.Wallets.Templates.Messages;
using Trinity.Wallets;

namespace Trinity.Network.RPC
{
    public class TrinityRpcRequest
    {
        public static string Post<T>(string gateWayUri, string messageType, T TRequest)
        {
            string result = null;

            List<T> parameters = new List<T>() { TRequest };
            JObject request = new JObject();
            request["jsonrpc"] = "2.0";
            request["id"] = 1;
            request["method"] = messageType;
            request["params"] = JObject.Parse(parameters.Serialize());

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(gateWayUri);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Timeout = 10000; //timeout is 10 seconds

            #region add parameters           
            byte[] data = Encoding.UTF8.GetBytes(request.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }
            #endregion

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            //获取响应内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        public static string PostIgnoreException<T>(string gateWayUri, string messageType, T TRequest)
        {
            try
            {
                return Post(gateWayUri, messageType, TRequest);
            }
            catch (Exception ExpInfo)
            {
                Log.Warn("Exception occurred during RPC call for {0}. Exception: {1}", messageType, ExpInfo);
                return null;
            }
        }
    }
}