using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.FileServer.Utils
{
    /// <summary>
    /// 生成唯一标识
    /// </summary>
    public class UniqueHelper
    {
        /// <summary>
        /// 根据Guid获取唯一数字序列，19位
        /// </summary>
        /// <returns></returns>
        public static long LongId()
        {
            byte[] value = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt64(value, 0);
        }
    }
}
