using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.CodeBuilder;
using Senparc.Xncf.CodeBuilder.Request;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    /// <summary>
    /// 生成记录
    ///</summary>
    public class UserBuilderLog_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserBuilderLogService _userBuilderLogService;
        public UserBuilderLog_IndexModel(IServiceProvider serviceProvider, UserBuilderLogService userBuilderLogService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _userBuilderLogService = userBuilderLogService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("userbuilderlog-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryUserBuilderLogListReq req)
        {
            var seh = new SenparcExpressionHelper<UserBuilderLog>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _userBuilderLogService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "Flag", Description = "是否删除", Browsable = true, Type = "bool"});
            properties.Add(new KeyDescription { Key = "AddTime", Description = "", Browsable = true, Type = "DateTime"});
            properties.Add(new KeyDescription { Key = "LastUpdateTime", Description = "最后更新时间", Browsable = true, Type = "DateTime"});
            properties.Add(new KeyDescription { Key = "AdminRemark", Description = "", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "Remark", Description = "备注", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "AdditionNote", Description = "", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "TenantId", Description = "租户Id", Browsable = true, Type = "int"});
            properties.Add(new KeyDescription { Key = "UserId", Description = "用户Id", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "TableName", Description = "表名", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "ModuleName", Description = "模块名称", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "Path", Description = "存放路径", Browsable = true, Type = "string"});
            properties.Add(new KeyDescription { Key = "Count", Description = "发送次数", Browsable = true, Type = "int"});

            //properties.Add(new KeyDescription {  Key= "sort", Description="排序", Browsable=true, Type ="int" });
            var propertyStr = string.Join(',', properties.Select(u => u.Key));

            return Ok(new { ObjectList.TotalCount, ObjectList.PageIndex, List = ObjectList.AsEnumerable(), columnHeaders = properties });
        }
        /// <summary>
        /// 新增/编辑
		/// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("userbuilderlog-add", "userbuilderlog-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateUserBuilderLogReq req)
        {
            await _userBuilderLogService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("userbuilderlog-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _userBuilderLogService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
