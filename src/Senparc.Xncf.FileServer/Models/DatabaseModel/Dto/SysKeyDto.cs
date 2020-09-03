using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.FileServer.Models.DatabaseModel.Dto
{
    /// <summary>
    /// 
    /// </summary>
    public class SysKeyDto : DtoBase
    {
        public int Id { get; set; }

        /// <summary>
        /// App标识Id
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// AppKey    密钥
        /// </summary>
        public string AppKey { get; set; }

        /// <summary>
        /// 名称、用户
        /// </summary>
        public string Name { get; set; }

        ///// <summary>
        ///// 创建时间
        ///// </summary>
        //public DateTime CreateTime { get;  set; }

        ///// <summary>
        ///// 修改时间
        ///// </summary>
        //public DateTime? ModifiedTime { get;  set; }

        ///// <summary>
        ///// 备注
        ///// </summary>
        //public string Remark { get;  set; }

        ///// <summary>
        ///// 包含的文件列表
        ///// </summary>
        //[Newtonsoft.Json.JsonIgnore]
        //public ICollection<FileRecord> FileRecords { get;  set; }
    }
}
