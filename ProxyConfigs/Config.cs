using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace HttpProxy.ProxyConfigs
{
    public class Config
    {
        private static string proxyConfigPath;
        private static string Trim(string val)
        {
            return (val ?? "").Trim();
        }
        public static ProxyConfig Settings
        {
            get
            {
                string fileFullName = GetConfigFilePath();
                try
                {
                  
                    if (IsOutOfDate(fileFullName) || _proxyConfig == null)
                    {
                        _proxyConfig = Deserialize<ProxyConfig>(fileFullName);
                        _proxyConfig.Filters.RewriteList.ForEach(o => {
                            o.MapTo = Trim(o.MapTo);
                            o.Url = Trim(o.Url);
                        });
                        _proxyConfig.Filters.AppendList.ForEach(o =>
                        {
                            o.Url = Trim(o.Url);
                        });
                        _proxyConfig.Filters.ReplaceList.ForEach(o =>
                        {
                            o.OldValue = Trim(o.OldValue);
                            o.Url = Trim(o.Url);
                        });
                        Console.WriteLine("\n加载配置Proxy.config成功.\n");
                    }
                }
                catch (Exception err)
                {
                    Console .WriteLine(err.ToString ());
                }
                if (_proxyConfig == null)
                {
                    System.Diagnostics.Debug.WriteLine("{0}反序列化失败", fileFullName);
                }
                return _proxyConfig;
            }
        }

        private static T Deserialize<T>(string fileFullName) where T : class
        {
            using (StreamReader sr = new StreamReader(fileFullName))
            {
                return new XmlSerializer(typeof(T)).Deserialize(sr) as T;
            }
        }
        private static string GetConfigFilePath()
        {
            if (string.IsNullOrWhiteSpace (proxyConfigPath))
            {
                proxyConfigPath= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Proxy.config");
            }
            return proxyConfigPath;
        }
        /// <summary>
        /// 文件是否过期
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        public static bool IsOutOfDate(string fileFullName)
        {
            if ((DateTime.Now - _lastCheckTime).TotalMinutes < 1)//一分钟检查一次
            {
                return false;
            }
            else
            {
                _lastCheckTime = DateTime.Now;
            }

            bool result = true;
            var date = System.IO.File.GetLastWriteTime(fileFullName);
            if (!_lastWriteTimeDic.ContainsKey(fileFullName))
            {
                _lastWriteTimeDic.Add(fileFullName, date);
            }
            else
            {
                result = _lastWriteTimeDic[fileFullName] != date;
                _lastWriteTimeDic[fileFullName] = date;
            }
            return result;
        }
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static Dictionary<string, DateTime> _lastWriteTimeDic = new Dictionary<string, DateTime>();//文件最后更新时间 集合
        private static ProxyConfig _proxyConfig;
    }
}
