using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Configuration;
using System.Collections.Generic;
using HttpProxy.ProxyConfigs;
using System.Text.RegularExpressions;
using System.IO.Compression;
namespace HttpProxy
{
    class Worker
    {

       private HttpListenerContext client;
        //pass through headers
       private string[] headers;
       private ProxyConfig config;
       private WorkerFilter filter;
        public Worker(HttpListenerContext context)
        {
            this.client = context;
            this.config = Config.Settings;
         
            this.filter = new WorkerFilter(config);
            headers = new string[] { "Cookie", "Accept", "Referrer", "Accept-Language" };
        }

        private void SetParentProxy(HttpWebRequest request)
        {
            WebProxy parent=null;
            //init proxy
            if (!string.IsNullOrEmpty(config.ParentProxy.Host) && config.ParentProxy.Port > 0)
            {
                parent = new WebProxy(config.ParentProxy.Host, config.ParentProxy.Port);

                if (!string.IsNullOrEmpty(config.ParentProxy.UserName))
                {
                    parent.Credentials = new NetworkCredential(config.ParentProxy.UserName,
                        config.ParentProxy.Password,
                        config.ParentProxy.Domain);
                }
                else
                {
                    parent.UseDefaultCredentials = true;
                }
            }
            if (parent != null)
            {
                request.Proxy = parent;
            }
        }

        private byte[] GetBytesFromStream(Stream stream)
        {
            byte[] result;
            byte[] buffer = new byte[256];

            BinaryReader reader = new BinaryReader(stream);
            MemoryStream memoryStream = new MemoryStream();

            int count = 0;
            while (true)
            {
                count = reader.Read(buffer, 0, buffer.Length);
                memoryStream.Write(buffer, 0, count);

                if (count == 0)
                    break;
            }

            result = memoryStream.ToArray();
            memoryStream.Close();
            reader.Close();
            stream.Close();

            return result;
        }

        private string GetPortFromQueryString()
        {
            var replacePortStr = client.Request.QueryString["__port__"];
            int replacePort;

            if (!string.IsNullOrWhiteSpace(replacePortStr))
            {
                if (int.TryParse(replacePortStr, out replacePort))
                {
                    replacePortStr = ":" + replacePort;
                }
            }
            else
            {
                replacePortStr = string.Empty;
            }
            return replacePortStr;
        }
        /// <summary>
        /// gzip或deflate解压
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="contentEncoding"></param>
        /// <returns></returns>
        private byte[] Decompress(byte[] buffer,string contentEncoding)
        {
            if (!string.IsNullOrWhiteSpace(contentEncoding))
            {
                if (contentEncoding == "gzip")
                {
                    var gzip = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress);
                    return GetBytesFromStream(gzip);
                }
                else if (contentEncoding == "deflate")
                {
                    var deflate = new DeflateStream(new MemoryStream(buffer), CompressionMode.Decompress);
                    return GetBytesFromStream(deflate);
                }
            }
            return buffer;
        }

        private void SetCookies(HttpWebRequest request)
        {
            try
            {
                var host = new Uri(request.RequestUri.ToString()).Host;//www.baidu.com
                var index = host.IndexOf('.');
                var domain= host.Substring(index);// .baidu.com
                request.CookieContainer = new CookieContainer();
                for (int i = 0; i < client.Request.Cookies.Count; i++)
                {
                    var c = client.Request.Cookies[i];
                    c.Domain = domain;
                    request.CookieContainer.Add(c);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }

        public void ProcessRequest()
        {
            string rawUrl=client.Request.Url.ToString();
            string beforeRewriteUrl = rawUrl.Replace(":" + config.Proxy.Port, string.Empty);
            string url = client.Request.Url.ToString().Replace(":" + config.Proxy .Port , GetPortFromQueryString());
            string afterRewriteUrl = string.Empty;
            string msg = DateTime.Now.ToString("hh:mm:ss") + " " + client.Request.HttpMethod + " " + url;
            Console.WriteLine(msg);

            byte[] result;
            try
            {
                afterRewriteUrl = filter.Rewrite(url);
                if (url!=afterRewriteUrl)//被重写了
                {
                    Console.WriteLine("\n\n--------------------------\n{0}\n重写为:\n{1}\n--------------------------\n\n", beforeRewriteUrl, afterRewriteUrl);
                }
                var request = WebRequest.Create(afterRewriteUrl) as HttpWebRequest;


                SetParentProxy(request);
                SetCookies(request);
                request.UserAgent = client.Request.UserAgent;
                request.Method = client.Request.HttpMethod;
                request.ContentType = client.Request.ContentType;
                request.ContentLength = client.Request.ContentLength64;
                if (client.Request.ContentLength64 > 0 && client.Request.HasEntityBody)
                {
                    using (System.IO.Stream body = client.Request.InputStream)
                    {
                        byte[] requestdata = GetBytesFromStream(body);
                        request.ContentLength = requestdata.Length;
                        Stream s = request.GetRequestStream();
                        s.Write(requestdata, 0, requestdata.Length);
                        s.Close();
                    }
                }
                //request processing
                WebResponse response = request.GetResponse() as HttpWebResponse;
                result = GetBytesFromStream(response.GetResponseStream());
                client.Response.ContentType = response.ContentType; 
                client.Response.AppendHeader("Set-Cookie", response.Headers.Get("Set-Cookie"));
                var contentEncoding= (response.Headers["Content-Encoding"]??"").Trim ().ToLower ();//压缩类型
                result = Decompress(result, contentEncoding);
                response.Close();
            }
            catch (WebException wex)
            {
                result = Encoding.UTF8.GetBytes(wex.Message);
                HttpWebResponse resp = (HttpWebResponse)wex.Response;
                client.Response.StatusCode = (int)resp.StatusCode;
                client.Response.StatusDescription = resp.StatusDescription;
                Console.WriteLine("ERROR:" + wex.Message);
            }
            catch (Exception ex)
            {
                result = Encoding.UTF8.GetBytes(ex.Message);
                Console.WriteLine("ERROR:" + ex.Message);
            }
            try
            {
                //response
                byte[] buffer = result;
                buffer = filter.ReplaceAndAppend(buffer, client.Response.ContentType, beforeRewriteUrl);
                client.Response.ContentLength64 = buffer.Length;
                client.Response.OutputStream.Write(buffer, 0, buffer.Length);
                client.Response.OutputStream.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
           
        }
    }
}
