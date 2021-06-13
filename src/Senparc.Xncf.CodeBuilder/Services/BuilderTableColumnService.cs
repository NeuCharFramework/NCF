using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.CodeBuilder.Models.DatabaseModel.Dto;
using Senparc.Xncf.CodeBuilder.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.CodeBuilder.Services
{
    public class BuilderTableColumnService : ServiceBase<BuilderTableColumn>
    {
        public BuilderTableColumnService(IRepositoryBase<BuilderTableColumn> repo, IServiceProvider serviceProvider)
           : base(repo, serviceProvider)
        {
        }

        ///// <summary>
        ///// 加载列表
        ///// </summary>
        //public async Task<TableResp<BuilderTableColumn>> Load(BuilderTableColumnDto request)
        //{
        //    if (string.IsNullOrEmpty(request.BuilderTableId))
        //    {
        //        throw new Exception($"缺少必要的参数BuilderTableId");
        //    }
        //    var result = new TableResp<BuilderTableColumn>();
        //    var objs = UnitWork.Find<BuilderTableColumn>(u => u.TableId == request.BuilderTableId);
        //    if (!string.IsNullOrEmpty(request.key))
        //    {
        //        objs = objs.Where(u => u.ColumnName.Contains(request.key));
        //    }

        //    result.data = objs.OrderBy(u => u.ColumnName)
        //        .Skip((request.page - 1) * request.limit)
        //        .Take(request.limit).ToList();
        //    result.count = objs.Count();
        //    return result;
        //}

        public void Update(AddOrUpdateBuilderTableColumnReq obj)
        {
            var objDto = this.GetObject(z => z.Id == obj.Id);
            if (obj == null)
            {
                throw new Exception("信息不存在！");
            }
            objDto.TableName = obj.TableName;
            objDto.ColumnName = obj.ColumnName;
            objDto.Comment = obj.Comment;
            objDto.ColumnType = obj.ColumnType;
            objDto.EntityType = obj.EntityType;
            objDto.EntityName = obj.EntityName;
            objDto.IsKey = obj.IsKey;
            objDto.IsIncrement = obj.IsIncrement;
            objDto.IsRequired = obj.IsRequired;
            objDto.IsInsert = obj.IsInsert;
            objDto.IsEdit = obj.IsEdit;
            objDto.IsList = obj.IsList;
            objDto.IsQuery = obj.IsQuery;
            objDto.QueryType = obj.QueryType;
            objDto.HtmlType = obj.HtmlType;
            objDto.EditType = obj.EditType;
            objDto.Sort = obj.Sort;
            objDto.EditRow = obj.EditRow;
            objDto.EditCol = obj.EditCol;
            objDto.MaxLength = obj.MaxLength;
            objDto.DataSource = obj.DataSource;
            objDto.UpdateTime = DateTime.Now;
            SaveObject(objDto);
        }
        /// <summary>
        /// 删除字段明细
        /// </summary>
        /// <param name="ids"></param>
        public void Delete(int[] ids)
        {
            DeleteAllAsync(u => ids.ToList().Contains(u.Id));
        }

        internal Task Load(QueryBuilderTableColumnListReq request)
        {
            throw new NotImplementedException();
        }

        public List<BuilderTableColumn> Find(int tableId)
        {
            var tableColumnList = GetFullList(_ => _.TableId == tableId, z => z.Id, OrderingType.Ascending);
            return tableColumnList;
        }
    }

}
