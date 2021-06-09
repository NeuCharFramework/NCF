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
    /// 课程分类
    ///</summary>
    public class CourseCategory_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CourseCategoryService _courseCategoryService;
        public CourseCategory_IndexModel(IServiceProvider serviceProvider, CourseCategoryService courseCategoryService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _courseCategoryService = courseCategoryService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("coursecategory-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryCourseCategoryListReq req)
        {
            var seh = new SenparcExpressionHelper<CourseCategory>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _courseCategoryService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "ID", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "sort", Description = "排序", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "name", Description = "分类名", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "is_show", Description = "显示", Browsable = true, Type = "byte" });
            properties.Add(new KeyDescription { Key = "parent_id", Description = "父id", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "parent_name", Description = "父级名称", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "AddTime", Description = "添加时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "LastUpdateTime", Description = "最后修改时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "AdminRemark", Description = "备注", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "Remark", Description = "说明", Browsable = false, Type = "string" });
            var propertyStr = string.Join(',', properties.Select(u => u.Key));

            return Ok(new { ObjectList.TotalCount, ObjectList.PageIndex, List = ObjectList.AsEnumerable(), columnHeaders = properties });
        }
        /// <summary>
        /// 新增/编辑
		/// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("coursecategory-add", "coursecategory-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateCourseCategoryReq req)
        {
            await _courseCategoryService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("coursecategory-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _courseCategoryService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
