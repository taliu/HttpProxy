using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Configuration;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using HttpProxy.ProxyConfigs;

namespace HttpProxy
{
   public  class Program
    {
        
        static void Main()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://" + Config.Settings.Proxy.Host + ":" + Config.Settings.Proxy.Port + "/");
            listener.Start();
            Console.WriteLine("Listening on " + Config.Settings.Proxy.Host + ":" + Config.Settings.Proxy.Port);
            while (true)
            {
                //as soon as there is a connection request
                HttpListenerContext ctx = listener.GetContext();
                Task.Factory.StartNew(new Worker(ctx).ProcessRequest);
            }
        }

    }
}
