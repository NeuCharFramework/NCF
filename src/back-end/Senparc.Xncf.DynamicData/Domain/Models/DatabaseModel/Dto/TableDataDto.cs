using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class TableDataDto: DtoBase
    {
        /// <summary>  
        /// 关联的表格ID。  
        /// </summary>  
        public int TableId { get; private set; }

        /// <summary>  
        /// 关联的列ID。  
        /// </summary>  
        public int ColumnMetadataId { get; private set; }

        /// <summary>  
        /// 单元格的值。  
        /// </summary>  
        public string CellValue { get; private set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //public TableMetadata TableMetadata { get; set; }

        /// <summary>  
        /// 关联的列元数据。  
        /// </summary>  
        public ColumnMetadata ColumnMetadata { get; set; }

        private TableDataDto() { }

    }
}
