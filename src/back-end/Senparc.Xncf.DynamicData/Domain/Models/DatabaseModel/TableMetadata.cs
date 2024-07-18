using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>  
    /// TableMetadata 实体类，用于存储表格的基本信息。  
    /// </summary>  
    [Table(Register.DATABASE_PREFIX + nameof(TableMetadata))]
    [Serializable]
    public class TableMetadata : EntityBase<int>
    {
        /// <summary>  
        /// 表格名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string TableName { get; private set; }

        /// <summary>  
        /// 表格描述。  
        /// </summary>  
        public string Description { get; private set; }

        /// <summary>  
        /// 关联的列元数据集合。  
        /// </summary>  
        //[InverseProperty(nameof(ColumnMetadata.TableMetadata))]
        public ICollection<ColumnMetadata> ColumnMetadatas { get; set; }

        /// <summary>  
        /// 关联的数据集合。  
        /// </summary>  
        //[InverseProperty(nameof(TableData.TableMetadata))]
        public ICollection<TableData> TableDatas { get; set; }

        private TableMetadata() { }

        public TableMetadata(string tableName, string description)
        {
            TableName = tableName;
            Description = description;
        }

    }
}
