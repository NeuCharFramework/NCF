using Senparc.Ncf.Core.Models;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileServer
{
    /// <summary>
    /// 账号信息 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(SysKey))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class SysKey : EntityBase<int>
    {
        //private SysKey() { }
        //public SysKey(SysKeyDto dto)
        //{
        //    Name = dto.Name;
        //    AppId = dto.AppId;
        //    AppKey = dto.AppKey;
        //    AdminRemark = dto.AdminRemark;
        //    Remark = dto.Remark;
        //    SetUpdateTime(SystemTime.Now.UtcDateTime);
        //}

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
        //public DateTime CreateTime { get; set; }

        ///// <summary>
        ///// 修改时间
        ///// </summary>
        //public DateTime? ModifiedTime { get; set; }

        ///// <summary>
        ///// 备注
        ///// </summary>
        //public string Remark { get; set; }

        /// <summary>
        /// 包含的文件列表
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public ICollection<FileRecord> FileRecords { get; set; }
    }
}
