using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.MaQueKeTang;
using Senparc.Xncf.MaQueKeTang.Request;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    /// <summary>
    /// 课程用户
    ///</summary>
    public class CourseUser_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CourseUserService _courseUserService;
        public CourseUser_IndexModel(IServiceProvider serviceProvider, CourseUserService courseUserService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _courseUserService = courseUserService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseuser-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryCourseUserListReq req)
        {
            var seh = new SenparcExpressionHelper<CourseUser>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _courseUserService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "AccountId", Description = "用户Id", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "CourseId", Description = "课程Id", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "charge", Description = "收费", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "Flag", Description = "删除状态", Browsable = true, Type = "bool"});
properties.Add(new KeyDescription { Key = "AddTime", Description = "添加时间", Browsable = true, Type = "DateTime"});
properties.Add(new KeyDescription { Key = "LastUpdateTime", Description = "最后修改时间", Browsable = true, Type = "DateTime"});
properties.Add(new KeyDescription { Key = "AdminRemark", Description = "备注", Browsable = true, Type = "string"});
properties.Add(new KeyDescription { Key = "Remark", Description = "说明", Browsable = true, Type = "string"});
properties.Add(new KeyDescription { Key = "TenantId", Description = "租户Id", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "Id", Description = "Id", Browsable = true, Type = "int"});

            //properties.Add(new KeyDescription {  Key= "sort", Description="排序", Browsable=true, Type ="int" });
            var propertyStr = string.Join(',', properties.Select(u => u.Key));

            return Ok(new { ObjectList.TotalCount, ObjectList.PageIndex, List = ObjectList.AsEnumerable(), columnHeaders = properties });
        }
        /// <summary>
        /// 新增/编辑
		/// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseuser-add", "courseuser-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateCourseUserReq req)
        {
            await _courseUserService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseuser-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _courseUserService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
