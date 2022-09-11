using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AuditLog.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AuditLog.Models.DatabaseModel.Dto
{
    public class AuditLogDto : DtoBase
    {
        /// <summary>
        /// AuditLogDto
        /// </summary>
        /// 
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

        private AuditLogDto() { }
    }
}
