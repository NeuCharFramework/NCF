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
    /// 课程
    ///</summary>
    public class Course_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CourseService _courseService;
        public Course_IndexModel(IServiceProvider serviceProvider, CourseService courseService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _courseService = courseService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("course-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryCourseListReq req)
        {
            var seh = new SenparcExpressionHelper<Course>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _courseService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "课程Id", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "title", Description = "课程", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "charge", Description = "价格", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "user_count", Description = "学习人数", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "AccountId", Description = "用户Id", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "slug", Description = "slug", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "thumb", Description = "封面", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "short_description", Description = "简短介绍", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "original_desc", Description = "简介原始内容", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "render_desc", Description = "简介渲染后的内容", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "seo_keywords", Description = "SEO关键字", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "seo_description", Description = "SEO描述", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "published_at", Description = "上线时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "is_show", Description = "1显示,-1隐藏", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "category_id", Description = "分类id", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "is_rec", Description = "推荐,0否,1是", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "is_free", Description = "免费,0否,1是", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "comment_status", Description = "0关闭评论,1所有人,2仅购买", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "AddTime", Description = "添加时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "LastUpdateTime", Description = "最后修改时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "AdminRemark", Description = "备注", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "Remark", Description = "说明", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "TenantId", Description = "租户Id", Browsable = false, Type = "int" });

            //properties.Add(new KeyDescription {  Key= "sort", Description="排序", Browsable=true, Type ="int" });
            var propertyStr = string.Join(',', properties.Select(u => u.Key));

            return Ok(new { ObjectList.TotalCount, ObjectList.PageIndex, List = ObjectList.AsEnumerable(), columnHeaders = properties });
        }
        /// <summary>
        /// 新增/编辑
		/// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("course-add", "course-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateCourseReq req)
        {
            await _courseService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("course-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _courseService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
