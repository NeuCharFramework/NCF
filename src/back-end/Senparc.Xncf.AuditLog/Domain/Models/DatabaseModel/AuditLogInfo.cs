using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AuditLog.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AuditLog
{
    /// <summary>
    /// AuditLog 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AuditLogInfo))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AuditLogInfo : EntityBase<string>
    {
        /// <summary>
        /// AuditLogInfo
        /// </summary>
        [Required]
        public int Id { get; set; }

        [MaxLength(200)]
        public string ActionName { get; set; }

        [MaxLength(200)]
        public string IpAddress { get; set; }

        [MaxLength(200)]
        public string ActionTime { get; set; }

        [MaxLength(200)]
        public string UserName { get; set; }

        private AuditLogInfo() { }

        public AuditLogInfo(string userName,string ipAddress, string actionName, string actionTime)
        {
            UserName = userName;
            IpAddress = ipAddress;
            ActionName = actionName;
            ActionTime = actionTime;
        }
    }
}
