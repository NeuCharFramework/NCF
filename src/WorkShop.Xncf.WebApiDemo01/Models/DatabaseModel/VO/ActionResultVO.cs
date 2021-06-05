
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.VO
{
    public class ActionResultVO : ActionResultVO<object>
    {
    }

    /// <summary>
    /// 通用请求方法返回对象
    /// </summary>
    public class ActionResultVO<T> where T : class
    {
        /// <summary>
        /// 错误码，200 表示成功，-1 表示异常，其它自定义建议从 1 开始累加
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; } = 0;

        /// <summary>
        /// 消息
        /// </summary>
        [JsonProperty("msg")]
        public string Msg { get; set; }

        /// <summary>
        /// 主体数据
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 用时，毫秒（在endTime上面，否则非序列化返回endTime值为null）
        /// </summary>
        [JsonProperty("useTime")]
        public double UseTime
        {
            get
            {
                EndTime ??= DateTime.Now;
                return (EndTime.Value - StartTime).TotalMilliseconds;
            }
        }

        /// <summary>
        /// 结束时间
        /// </summary>
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 设置快捷标签，赋值code、msg
        /// </summary>
        /// <param name="tag">快捷标签枚举</param>
        public void Set(ARTag tag)
        {
            Code = Convert.ToInt32(tag);
            Msg = tag.ToString();

            EndTime = DateTime.Now;
        }

        /// <summary>
        /// 设置快捷标签，赋值code、msg
        /// </summary>
        /// <param name="isyes"></param>
        public void Set(bool isyes)
        {
            if (isyes)
            {
                Set(ARTag.success);
            }
            else
            {
                Set(ARTag.fail);
            }
        }

        /// <summary>
        /// 设置快捷标签，赋值code、msg
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="appendCatch">追加错误消息，默认true</param>
        public void Set(Exception ex, bool appendCatch = true)
        {
            Code = -1;
            Msg = "处理出错";
            if (appendCatch)
            {
                Msg += "，错误消息：" + ex.Message;
            }

            EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 快捷标签枚举
    /// </summary>
    public enum ARTag
    {
        /// <summary>
        /// 成功
        /// </summary>
        success = 200,
        /// <summary>
        /// 失败
        /// </summary>
        fail = 400,
        /// <summary>
        /// 错误
        /// </summary>
        error = 500,
        /// <summary>
        /// 未授权
        /// </summary>
        unauthorized = 401,
        /// <summary>
        /// 拒绝
        /// </summary>
        refuse = 403,
        /// <summary>
        /// 存在
        /// </summary>
        exist = 97,
        /// <summary>
        /// 无效
        /// </summary>
        invalid = 95,
        /// <summary>
        /// 缺省
        /// </summary>
        lack = 94,
        /// <summary>
        /// 异常
        /// </summary>
        exception = -1
    }
}
