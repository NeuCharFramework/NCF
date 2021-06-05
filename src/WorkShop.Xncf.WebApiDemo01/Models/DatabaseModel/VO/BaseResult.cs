
using Senparc.CO2NET.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.VO
{
    public class BaseResult<T>
    {
        public BaseResult(int code,string msg,T data)
        {
            Code = code;
            Msg = msg;
            Data = data;
            RequestTime = $"{ string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)}";
        }

        /// <summary>
        /// 错误码，200 表示成功
        /// </summary>
        public int Code { get; set; } = 0;

        /// <summary>
        /// 消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 主体数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 请求时间
        /// </summary>
        public string RequestTime { get; set; }


    }
}
