using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableMetadataDto : DtoBase
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
        public ICollection<ColumnMetadataDto> ColumnMetadatas { get; set; }

        /// <summary>  
        /// 关联的数据集合。  
        /// </summary>  
        public ICollection<TableData> TableDatas { get; set; }


        private TableMetadataDto() { }
    }
}
