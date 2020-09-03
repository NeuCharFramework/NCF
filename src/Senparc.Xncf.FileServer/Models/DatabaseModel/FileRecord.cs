using Senparc.Ncf.Core.Models;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileServer
{
    /// <summary>
    /// FileRecord 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(FileRecord))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class FileRecord : EntityBase<int>
    {
        /// <summary>
        /// 所属账号Id
        /// </summary>
        public int SysKeyId { get; set; }

        /// <summary>
        /// 所属账号
        /// </summary>
        public SysKey SysKey { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileContentType { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 前缀路径
        /// 需要使用下划线“_”代替斜杠“\”
        /// </summary>
        public string PrefixPath { get; set; }

        /// <summary>
        /// 文件存储路径，不包含设定的RootDir
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件Hash值
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// 文件大小，单位B
        /// 最大支持2GB
        /// </summary>
        public int FileSize { get; set; }

        ///// <summary>
        ///// 创建时间
        ///// </summary>
        //public DateTime CreateTime { get; set; }

        ///// <summary>
        ///// 修改时间
        ///// </summary>
        //public DateTime? ModifiedTime { get; set; }

        ///// <summary>
        ///// 文件备注
        ///// </summary>
        //public string Remark { get; set; }
    }
}
