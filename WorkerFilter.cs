using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpProxy.ProxyConfigs;
using System.Text.RegularExpressions;

namespace HttpProxy
{
    public class WorkerFilter
    {
        private ProxyConfig _config;
        public WorkerFilter(ProxyConfig config)
        {
            _config = config;
        }
        /// <summary>
        /// 根据配置，重写匹配的url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string Rewrite(string url)
        {
            var cf = _config.Filters;
            if (cf.Enable)
            {
                foreach (var item in cf.RewriteList)
                {
                    if (item.EnableRegex)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Url)&&!string.IsNullOrWhiteSpace (item .MapTo))
                        {
                            var match = Regex.Match(url, item.Url);
                            if (match.Success&&match.Index==0)
                            {
                                var rewriteUrl = item.MapTo;
                                if (match.Groups.Count > 1)
                                {
                                    if (rewriteUrl.Contains("$"))
                                    {
                                        for (int i = 1; i < match.Groups.Count; i++)
                                        {
                                            rewriteUrl = rewriteUrl.Replace("$" + i, match.Groups[i].Value);
                                        }
                                    }
                                }
                                return rewriteUrl;
                            }
                        }
                    }
                    else
                    {
                        if (string.Equals(url, item.Url, StringComparison.OrdinalIgnoreCase))
                        {
                            return item.MapTo;
                        }
                    }
                    
                }
            }
            return url;
        }
        /// <summary>
        /// 根据配置，替换和追加内容，然后返回改变后的buffer字节数据
        /// </summary>
        /// <param name="buffer">要替换和追加文本的字节数组</param>
        /// <param name="contentType">buffer的类型，如text/html; charset=utf-8</param>
        /// <param name="url">buffer来源地址</param>
        /// <returns></returns>
        public byte[] ReplaceAndAppend(byte[] buffer, string contentType,string url)
        {
         
            var encoding = Encoding.UTF8;
            ContentType ct = GetContentType(contentType);
            if (ct.IsTextType)
            {
                encoding = Encoding.GetEncoding(ct.Charset);
                var  text= encoding.GetString(buffer);
                ContentType result = GetContentType(text,true);
                if (!string.IsNullOrWhiteSpace (result .Charset))
                {
                    if (result.Charset != ct.Charset)
                    {
                        encoding = Encoding.GetEncoding(result.Charset);
                        text = encoding.GetString(buffer);
                    }
                }
                text = Replace(text,url);
                text = Append(text, url);
                buffer = encoding.GetBytes(text);
            }
            System.Diagnostics.Debug.WriteLineIf(!ct.IsTextType, url+" 不是文本类型，直接返回");
            return buffer;
        }



        /// <summary>
        /// 根据配置，如果url匹配，则向txt替换文本
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public string Replace(string txt, string url)
        {
            if (_config .Filters .Enable)
            {
                var list = _config.Filters.ReplaceList;
                if (list!=null&&list.Count>0)
                {
                    foreach (var item in list)
                    { 
                        if (item.EnableRegex)
                        {
                            if (   !string.IsNullOrWhiteSpace(item.Url)
                                && !string.IsNullOrWhiteSpace(item.OldValue)
                                && IsMatch (url,item .Url))
                            {
                                
                                txt = Regex.Replace(txt, item.OldValue, item.NewValue);
                            }
                        }
                        else
                        {
                            if (string.Equals(url, item.Url, StringComparison.OrdinalIgnoreCase) 
                                 && !string.IsNullOrWhiteSpace(item.OldValue))
                            {
                                txt = txt.Replace(item.OldValue, item.NewValue);
                            }
                        }
                    }
                }
            }
            return txt;
        }
        /// <summary>
        /// 根据配置，如果url匹配，则向txt追加文本
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public string Append(string txt, string url)
        {
            if (_config.Filters.Enable)
            {
                var list = _config.Filters.AppendList;
                if (list != null && list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Url) && !string.IsNullOrWhiteSpace(url))
                        {
                            if (item.EnableRegex)
                            {
                                if(IsMatch (url,item .Url))
                                {
                                    txt = txt + item.Content;
                                }
                            }
                            else
                            {
                                if (string.Equals(url, item.Url, StringComparison.OrdinalIgnoreCase))
                                {
                                    txt = txt + item.Content;
                                }
                            }
                        }
                    }
                }
            }
            return txt;
        }

        private bool IsMatch(string url, string urlPattern)
        {
           var m= Regex.Match(url, urlPattern, RegexOptions.IgnoreCase);
           if (m.Success )
           {
               if (m.Index ==0)
               {
                   return true;
               }
           }
           return false;
        }

        private ContentType GetContentType(string txt,bool isHtml=false)
        {
            var result = new ContentType { IsTextType=false };
            //text/html; charset=utf-8
            //application/x-javascript
            var contentTypeRegex = @"\s*?(?<TypeName>[a-zA-Z0-9\\-]+?)/(?<SubTypeName>[a-zA-Z0-9\\-]+);\s*?charset\s*?=\s*?(?<charset>[a-zA-Z0-9\\-]+)";
            if (!string.IsNullOrWhiteSpace(txt))
            {
                var match = Regex.Match(txt, contentTypeRegex, RegexOptions.IgnoreCase);
                if (!match .Success&&!isHtml)
                {
                    contentTypeRegex = @"\s*?(?<TypeName>[a-zA-Z0-9\\-]+?)/(?<SubTypeName>[a-zA-Z0-9\\-]+)(;\s*?charset\s*?=\s*?(?<charset>[a-zA-Z0-9\\-]+))?";
                    match = Regex.Match(txt, contentTypeRegex, RegexOptions.IgnoreCase);
                }
                if (match.Success)
                {
                    result.TypeName = match.Groups["TypeName"].Value.Trim().ToLower();
                    result.SubTypeName = match.Groups["SubTypeName"].Value.Trim().ToLower();
                    result.Charset = match.Groups["charset"].Value;
                    if (string .IsNullOrWhiteSpace (result .Charset))
                    {
                        result.Charset = "utf-8";
                    }
                    result.Charset = result.Charset.Trim().ToLower();
                    if (result.TypeName == "text")
                    {
                        result.IsTextType = true;
                    }
                    else if (result.TypeName == "application" && (result.SubTypeName.Contains("json") || result.SubTypeName.Contains("javascript")))
                    {
                        result.IsTextType = true;
                    }
                }
            }
            return result;
        }
        class ContentType
        {
            public string Charset { get; set; }
            public string TypeName { get; set; }
            public string SubTypeName { get; set; }
            /// <summary>
            /// 是不是文本类型
            /// </summary>
            public bool IsTextType { get; set; }
        }
    }
}
