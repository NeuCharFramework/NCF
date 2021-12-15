
using System;

namespace Senparc.Areas.Admin.Domain.Models.VD
{
    /// <summary>
    /// 全局所有的Error_ExceptionVD必须实现这个接口
    /// </summary>
    public interface IError_BaseVD : IBaseVD
    {
        ///// <summary>
        ///// 给Error_ExceptionVD用
        ///// </summary>
        //HandleErrorInfo HandleErrorInfo { get; set; }

        /// <summary>
        /// 给Error_Error404VD用
        /// </summary>
        string Url { get; set; }
    }


    public class Error_BaseVD : BaseVD, IError_BaseVD
    {
        /// <summary>
        /// 给Error_ExceptionVD用
        /// </summary>
        //public HandleErrorInfo HandleErrorInfo { get; set; }

        /// <summary>
        /// 给Error_Error404VD用
        /// </summary>
        public string Url { get; set; }
    }

    public class Error_ExceptionVD : Error_BaseVD
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }


    public class Error_Error404VD : Error_BaseVD
    {
    }
}