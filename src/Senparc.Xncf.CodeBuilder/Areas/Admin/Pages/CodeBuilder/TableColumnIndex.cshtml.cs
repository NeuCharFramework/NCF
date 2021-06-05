using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.CodeBuilder.Models.DatabaseModel.Dto;
using Senparc.Xncf.CodeBuilder.Request;
using Senparc.Xncf.CodeBuilder.Services;

namespace Senparc.Xncf.CodeBuilder.Areas.Admin.Pages.Template
{
    public class TableColumnIndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public MultipleDatabaseType MultipleDatabaseType { get; set; }
        private readonly IServiceProvider _serviceProvider;
        private readonly BuilderTableColumnService _builderTableColumnService;

        public TableColumnIndexModel(IServiceProvider serviceProvider, BuilderTableColumnService builderTableColumnService, Lazy<XncfModuleService> xncfModuleService)
            : base(xncfModuleService)
        {
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            MultipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;
            _serviceProvider = serviceProvider;
            _builderTableColumnService = builderTableColumnService;
        }
        //ÐÞ¸Ä
        /// <summary>
        /// Handler=Update
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="orderField">XXX DESC,YYY ASC</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-search")]
        public async Task<IActionResult> OnPostUpdateAsync([FromBody] AddOrUpdateBuilderTableColumnReq obj)
        {
            var result = new Response();
            try
            {
                _builderTableColumnService.Update(obj);

            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }

            return Ok(result);
        }
        /// <summary>
        /// Handler=List
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="orderField">XXX DESC,YYY ASC</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-search")]
        public async Task<IActionResult> OnGetListAsync(string keyword, string orderField, int pageIndex, int pageSize, int BuilderTableId = 0)
        {
            orderField = "Id Asc";
            var seh = new SenparcExpressionHelper<BuilderTableColumn>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Remark.Contains(keyword));
            seh.ValueCompare.AndAlso(BuilderTableId != 0, _ => _.TableId.Equals(BuilderTableId));
            var where = seh.BuildWhereExpression();
            var result = await _builderTableColumnService.GetObjectListAsync(pageIndex, pageSize, where, orderField);
            return Ok(new
            {
                result.TotalCount,
                result.PageIndex,
                List =
                result.Select(_ => new
                {
                    _.Id,
                    _.TableName,
                    _.ColumnName,
                    _.Comment,
                    _.ColumnType,
                    _.EntityType,
                    _.EntityName,
                    _.IsKey,
                    _.IsIncrement,
                    _.IsRequired,
                    _.IsInsert,
                    _.IsEdit,
                    _.IsList,
                    _.IsQuery,
                    _.QueryType,
                    _.HtmlType,
                    _.EditType,
                    _.Sort,
                    _.CreateTime,
                    _.CreateUserId,
                    _.UpdateTime,
                    _.UpdateUserId,
                    _.EditRow,
                    _.EditCol,
                    _.UpdateUserName,
                    _.CreateUserName,
                    _.MaxLength,
                    _.LastUpdateTime,
                    _.Remark,
                    _.AddTime,
                    _.AdminRemark
                })
            });
        }

        /// <summary>
        /// ÅúÁ¿É¾³ý
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            var result = new Response();
            try
            {
                _builderTableColumnService.Delete(ids);

            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.InnerException?.Message ?? ex.Message;
            }
            return Ok(result);
        }

    }
}
