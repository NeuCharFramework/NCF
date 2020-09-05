using System;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Xncf.Swagger.Utils
{
    public class SecurityHelper
    {
        #region HMACSHA256
        /// <summary>
        /// HMAC_SHA256 
        /// </summary>
        /// <param name="srcString">The string to be encrypted</param>
        /// <param name="key">encrypte key</param>
        /// <returns></returns>
        public static string HMACSHA256(string srcString, string key = "Qidq_123_abc")
        {
            byte[] secrectKey = Encoding.UTF8.GetBytes(key);
            using (HMACSHA256 hmac = new HMACSHA256(secrectKey))
            {
                hmac.Initialize();

                byte[] bytes_hmac_in = Encoding.UTF8.GetBytes(srcString);
                byte[] bytes_hamc_out = hmac.ComputeHash(bytes_hmac_in);

                string str_hamc_out = BitConverter.ToString(bytes_hamc_out);
                str_hamc_out = str_hamc_out.Replace("-", "");

                return str_hamc_out;
            }
        }
        #endregion
    }
}
