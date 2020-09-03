using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.FileServer.Models.DatabaseModel.Dto
{
    /// <summary>
    /// 
    /// </summary>
    public class FileRecordDto : DtoBase
    {
        public int Id { get; set; }

        /// <summary>
        /// 所属账号Id
        /// </summary>
        public int SysKeyId { get; set; }

        /// <summary>
        /// 所属账号
        /// </summary>
        [AutoMapper.Configuration.Conventions.MapTo("")]
        public SysKeyDto SysKey { get; set; }

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
        //public DateTime CreateTime { get;  set; }

        ///// <summary>
        ///// 修改时间
        ///// </summary>
        //public DateTime? ModifiedTime { get;  set; }

        ///// <summary>
        ///// 文件备注
        ///// </summary>
        //public string Remark { get;  set; }
    }
}
