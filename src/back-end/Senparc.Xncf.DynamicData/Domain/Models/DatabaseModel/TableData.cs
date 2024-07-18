using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// TableData 实体类，用于存储单元格的数据。  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableData))]
    [Serializable]
    public class TableData : EntityBase<int>
    {
        /// <summary>  
        /// 关联的表格ID。  
        /// </summary>  
        [ForeignKey(nameof(TableMetadata))]
        public int TableId { get; set; }

        /// <summary>  
        /// 关联的列ID。  
        /// </summary>  
        [ForeignKey(nameof(ColumnMetadata))]
        public int ColumnId { get; set; }

        /// <summary>  
        /// 单元格的值。  
        /// </summary>  
        public string CellValue { get; set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// 关联的列元数据。  
        /// </summary>  
        public ColumnMetadata ColumnMetadata { get; set; }
    }
}
