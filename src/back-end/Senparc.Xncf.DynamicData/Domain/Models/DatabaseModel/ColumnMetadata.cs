using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// ColumnMetadata 实体类，用于存储列的基本信息。  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(ColumnMetadata))]
    [Serializable]
    public class ColumnMetadata : EntityBase<int>
    {
        /// <summary>  
        /// 关联的表格ID。  
        /// </summary>  
        [ForeignKey(nameof(TableMetadata))]
        public int TableId { get; set; }

        /// <summary>  
        /// 列名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get; set; }

        /// <summary>  
        /// 列的数据类型。  
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get; set; }

        /// <summary>  
        /// 是否允许NULL值。  
        /// </summary>  
        [Required]
        public bool IsNullable { get; set; } = true;

        /// <summary>  
        /// 列的默认值。  
        /// </summary>  
        public string DefaultValue { get; set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        public TableMetadata TableMetadata { get; set; }
    }
}
