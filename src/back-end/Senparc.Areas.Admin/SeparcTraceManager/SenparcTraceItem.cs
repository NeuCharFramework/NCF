using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Areas.Admin.SenparcTraceManager
{
    public class SenparcTraceItem
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public string DateTime { get; set; }
        /// <summary>
        /// 线程
        /// </summary>
        public int ThreadId { get; set; }
        /// <summary>
        /// 在日志中的行数
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// 日志类型
        /// </summary>
        public SenparcTraceType weixinTraceType { get; set; } = SenparcTraceType.Normal;
        /// <summary>
        /// 是否是一条异常日志
        /// </summary>
        public bool IsException { get; set; }
        /// <summary>
        /// 结果
        /// </summary>
        public WeicinTraceItemContent Result { get; set; } = new WeicinTraceItemContent();

        public string ResultStr => Result?.ToString();

    }

    public class WeicinTraceItemContent
    {
        public string AccessTokenOrAppId { get; set; }
        public string Url { get; set; }
        /// <summary>
        /// 返回结果，通常为JSON
        /// </summary>
        public string Result { get; set; }
        public string PostData { get; set; }
        public string TotalResult { get; set; }

        public string ExceptionMessage { get; set; }
        public string ExceptionAccessTokenOrAppId { get; set; }
        public string ExceptionStackTrace { get; set; }

        public override string ToString()
        {
            return TotalResult.Trim().Replace("\r\n", "<br/>");
        }

    }
}
