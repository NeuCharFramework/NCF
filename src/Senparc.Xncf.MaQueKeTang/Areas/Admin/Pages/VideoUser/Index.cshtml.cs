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
    /// 视频用户
    ///</summary>
    public class VideoUser_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly VideoUserService _videoUserService;
        public VideoUser_IndexModel(IServiceProvider serviceProvider, VideoUserService videoUserService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _videoUserService = videoUserService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("videouser-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryVideoUserListReq req)
        {
            var seh = new SenparcExpressionHelper<VideoUser>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _videoUserService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "ID", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "UserId", Description = "用户ID", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "VideoId", Description = "视频ID", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "Charge", Description = "收费", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "Flag", Description = "删除状态", Browsable = false, Type = "bool"});
properties.Add(new KeyDescription { Key = "AddTime", Description = "添加时间", Browsable = true, Type = "DateTime"});
properties.Add(new KeyDescription { Key = "AddUserId", Description = "添加者ID", Browsable = false, Type = "int"});
properties.Add(new KeyDescription { Key = "AddUserName", Description = "添加者姓名", Browsable = true, Type = "string"});
properties.Add(new KeyDescription { Key = "LastUpdateTime", Description = "最后修改时间", Browsable = true, Type = "DateTime"});
properties.Add(new KeyDescription { Key = "UpdateUserId", Description = "更新者ID", Browsable = false, Type = "int"});
properties.Add(new KeyDescription { Key = "UpdateUserName", Description = "更新者姓名", Browsable = true, Type = "string"});
properties.Add(new KeyDescription { Key = "AdminRemark", Description = "备注", Browsable = true, Type = "string"});
properties.Add(new KeyDescription { Key = "Remark", Description = "说明", Browsable = true, Type = "string"});
properties.Add(new KeyDescription { Key = "TenantId", Description = "租户Id", Browsable = true, Type = "int"});
properties.Add(new KeyDescription { Key = "deleted_at", Description = "删除时间", Browsable = true, Type = "DateTime"});

            //properties.Add(new KeyDescription {  Key= "sort", Description="排序", Browsable=true, Type ="int" });
            var propertyStr = string.Join(',', properties.Select(u => u.Key));

            return Ok(new { ObjectList.TotalCount, ObjectList.PageIndex, List = ObjectList.AsEnumerable(), columnHeaders = properties });
        }
        /// <summary>
        /// 新增/编辑
		/// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("videouser-add", "videouser-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateVideoUserReq req)
        {
            await _videoUserService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("videouser-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _videoUserService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
