using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.MaQueKeTang;
using Senparc.Xncf.MaQueKeTang.Request;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    /// <summary>
    /// 课程附件
    ///</summary>
    public class CourseAttach_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CourseAttachService _courseAttachService;

        public CourseAttach_IndexModel(IServiceProvider serviceProvider, CourseAttachService courseAttachService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _courseAttachService = courseAttachService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseattach-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryCourseAttachListReq req)
        {
            var seh = new SenparcExpressionHelper<CourseAttach>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _courseAttachService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "Id", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "name", Description = "附件名", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "path", Description = "路径", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "download_times", Description = "下载次数", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "CourseId", Description = "课程Id", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "disk", Description = "存储磁盘", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "size", Description = "单位：byte", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "extension", Description = "文件格式", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "only_buyer", Description = "只有购买者可以下载,1是,0否", Browsable = false, Type = "byte" });
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
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseattach-add", "courseattach-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateCourseAttachReq req)
        {
            await _courseAttachService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("courseattach-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _courseAttachService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
        /// <summary>
        /// 批量上传文件接口
        /// </summary>
        /// <param name="files">客户端文本框需设置name='files'</param>
        /// <returns>服务器存储的文件信息</returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("uploadfile-upload")]
        public IActionResult OnPostUpload([FromForm] IFormFileCollection files)
        {
            var result = new Response<IList<CourseAttach>>();
            try
            {
                result.Result = _courseAttachService.Upload(files);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return Ok(result);
        }
    }
}
