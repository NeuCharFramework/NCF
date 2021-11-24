using Senparc.CO2NET;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Service
{
    //public partial interface IEncryptionService : IEncryptionServiceBase
    //{
    //    string CommonEncrypt(string str);
    //    string CommonDecrypt(string str);

    //    string GetEncodedUserAvatar(int accountId, string encodeKey);
    //    int GetDecodedUserAvatar(string code, string encodeKey);

    //    string GetEncodedWeixinTokenPass(int weixinApp, string token, string encodeKey);
    //    Dictionary<int, string> CheckDecodedWeixinTokenPass(string code, string encodeKey);

    //    string GetEncodedQyAppKey(int qyAppId, DateTime qyAppAddTime, string encodeKey);
    //    //QyApp CheckDecodedQyAppKey(string code, string encodeKey);

    //    string EncodeAppCode(int appId, int neuralAppId, string encodeKey);
    //    int[] DecodeAppCode(string appCode, string encodeKey);
    //}

    public partial class EncryptionService : EncryptionServiceBase//, IEncryptionService
    {
        public const string BASE_ENCRYPT_ENCODING_KEY = "3Eddo*8jp";


        public EncryptionService()//(IBaseData baseData): base(baseData)
        { }

        /// <summary>
        /// 通用加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string CommonEncrypt(string str)
        {
            var content = $"{str}|{BASE_ENCRYPT_ENCODING_KEY}";
            return DesUtility.EncryptDES(content, BASE_ENCRYPT_ENCODING_KEY);
        }

        /// <summary>
        /// 通用解密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string CommonDecrypt(string str)
        {
            var content = DesUtility.EncryptDES(str, BASE_ENCRYPT_ENCODING_KEY);
            return str.Split('|')[0];
        }

        #region UserAvatar

        public string GetEncodedUserAvatar(int accountId, string encodeKey)
        {
            var content = $"{DateTime.Now.Ticks}|{accountId}";
            return DesUtility.EncryptDES(content, encodeKey);
        }

        public int GetDecodedUserAvatar(string code, string encodeKey)
        {
            try
            {
                var content = DesUtility.DecryptDES(code, encodeKey);
                var datas = content.Split('|');
                var dateTime = new DateTime(long.Parse(datas[0]));

                if ((DateTime.Now - dateTime).TotalSeconds > 15)
                {
                    return -1;//指定秒后失效
                }

                var accountId = int.Parse(datas[1]);
                return accountId;
            }
            catch
            {
                return -1;
            }
        }

        #endregion
    }
}
