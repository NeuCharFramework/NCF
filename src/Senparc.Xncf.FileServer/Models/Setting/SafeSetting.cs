using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.FileServer
{
    /// <summary>
    /// 安全相关配置
    /// </summary>
    public class SafeSetting
    {
        /// <summary>
        /// 是开发环境，可使用开发环境的接口；警告：正式环境需设为 false
        /// </summary>
        public bool IsDev { get; set; }

        /// <summary>
        /// 创建App的密码
        /// </summary>
        public string CreateAppPassword { get; set; }

        /// <summary>
        /// Token有效期，不能小于Token缓存，单位：分
        /// </summary>
        public int TokenExpired { get; set; }

        /// <summary>
        /// Token缓存，不能大于Token有效期，缓存期内保证Token有效，单位：分
        /// </summary>
        public int TokenCache { get; set; }

        /// <summary>
        /// JWT加密密钥
        /// </summary>
        public string Secret { get; set; }
    }
}
