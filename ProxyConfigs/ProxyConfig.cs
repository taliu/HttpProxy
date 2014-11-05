using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace HttpProxy.ProxyConfigs
{
    [Serializable]
    public class ProxyConfig
    {
        public Proxy Proxy { get; set; }
        public ParentProxy ParentProxy { get; set; }
        public FilterList Filters { get; set; }
    }
    /// <summary>
    /// 代理设置
    /// </summary>
    [Serializable]
    public class Proxy
    {
        public string Host { get; set; }
        public int Port { get; set; }

    }
    /// <summary>
    /// 代理的代理设置
    /// </summary>
    [Serializable]
    public class ParentProxy : Proxy
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
    }

    [Serializable]
    public class FilterList
    {
        /// <summary>
        /// 是否启用filter
        /// </summary>
        public bool Enable { get; set; }
        public List<Rewrite> RewriteList { get; set; }
        public List<Replace> ReplaceList { get; set; }
        public List<Append> AppendList { get; set; }
    }
    [Serializable]
    public class Rewrite
    {
        /// <summary>
        /// 要重写的url地址(可以启用正则表达式匹配)
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 用来替换的地址
        /// </summary>
        public string MapTo { get; set; }
        /// <summary>
        /// 是否启用正则表达式
        /// </summary>
        public bool EnableRegex { get; set; }
    }
    [Serializable]
    public class Replace
    {
        /// <summary>
        /// 要替换内容的页面地址(可以启用正则表达式匹配)
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 原来的值(可以启用正则表达式匹配)
        /// </summary>
        public string OldValue { get; set; }
        /// <summary>
        /// 新值
        /// </summary>
        public string NewValue { get; set; }
        /// <summary>
        /// 是否启用正则表达式
        /// </summary>
        public bool EnableRegex { get; set; }
    }

    [Serializable]
    public class Append
    {
        /// <summary>
        /// 要追加内容的页面地址(可以启用正则表达式匹配)
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 要追加的内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 是否启用正则表达式
        /// </summary>
        public bool EnableRegex { get; set; }
    }

}
