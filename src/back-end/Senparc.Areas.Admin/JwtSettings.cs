namespace Senparc.Areas.Admin
{
    public class JwtSettings
    {

        /// <summary>
        /// 前台jwt
        /// </summary>
        public const string Position_MiniPro = "MiniProJwt";

        /// <summary>
        /// 管理后台jwt
        /// </summary>
        public const string Position_Backend = "BackendJwt";

        /// <summary>
        /// token是谁颁发的
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// token可以给哪些客户端使用
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// 加密的key
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// 过期时间，单位【小时】
        /// </summary>
        public double Expires { get; set; }
    }
}
