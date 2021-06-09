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
    /// 课程用户记录
    ///</summary>
    public class CourseUserRecord_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CourseUserRecordService _courseUserRecordService;
        public CourseUserRecord_IndexModel(IServiceProvider serviceProvider, CourseUserRecordService courseUserRecordService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _courseUserRecordService = courseUserRecordService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseuserrecord-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryCourseUserRecordListReq req)
        {
            var seh = new SenparcExpressionHelper<CourseUserRecord>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _courseUserRecordService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "Id", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "CourseId", Description = "课程ID", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "AccountId", Description = "用户ID", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "AccountId", Description = "用户", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "AccountId", Description = "手机号", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "progress", Description = "观看进度", Browsable = true, Type = "byte" });
            properties.Add(new KeyDescription { Key = "AddTime", Description = "开始时间", Browsable = true, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "watched_at", Description = "看完时间", Browsable = true, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "watched_at", Description = "订阅", Browsable = true, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "is_watched", Description = "看完,0否,1是", Browsable = false, Type = "byte" });
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
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseuserrecord-add", "courseuserrecord-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateCourseUserRecordReq req)
        {
            await _courseUserRecordService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseuserrecord-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _courseUserRecordService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
