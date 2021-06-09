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
    /// 视频
    ///</summary>
    public class Video_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly VideoService _videoService;
        public Video_IndexModel(IServiceProvider serviceProvider, VideoService videoService, Lazy<XncfModuleService>
         xncfModuleService) : base(xncfModuleService)
        {
            _serviceProvider = serviceProvider;
            _videoService = videoService;
        }
        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("video-search")]
        //public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        public async Task<IActionResult> OnGetListAsync([FromQuery] QueryVideoListReq req)
        {
            var seh = new SenparcExpressionHelper<Video>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(req.key), _ => _.Remark.Contains(req.key));
            var where = seh.BuildWhereExpression();
            var ObjectList = await _videoService.GetObjectListAsync(req.pageIndex, req.pageSize, where, req.orderField);

            List<KeyDescription> properties = new List<KeyDescription>();
            properties.Add(new KeyDescription { Key = "Id", Description = "视频ID", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "Title", Description = "标题", Browsable = true, Type = "string" });
            properties.Add(new KeyDescription { Key = "Charge", Description = "价格", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "Duration", Description = "时长", Browsable = true, Type = "int" });
            properties.Add(new KeyDescription { Key = "UserId", Description = "用户ID", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "CourseId", Description = "课程ID", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "Slug", Description = "slug", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "Url", Description = "播放地址", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "ViewNum", Description = "观看次数", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "ShortDescription", Description = "简短介绍", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "OriginalDesc", Description = "简介原始内容", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "RenderDesc", Description = "简介渲染后的内容", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "SeoKeywords", Description = "SEO关键字", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "SeoDescription", Description = "SEO描述", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "PublishedAt", Description = "上线时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "IsShow", Description = "1显示,-1隐藏", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "AliyunVideoId", Description = "阿里云视频ID", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "ChapterId", Description = "章节ID", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "TencentVideoId", Description = "腾讯云video_id", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "IsBanSell", Description = "禁止售卖,0否,1是", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "CommentStatus", Description = "0禁止评论,1所有人,2仅购买", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "FreeSeconds", Description = "试看秒数", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "PlayerPC", Description = "pc播放器", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "PlayerH5", Description = "h5播放器", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "BanDrag", Description = "禁止拖动,0否,1是", Browsable = false, Type = "byte" });
            properties.Add(new KeyDescription { Key = "AddTime", Description = "添加时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "AddUserId", Description = "添加者ID", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "AddUserName", Description = "添加者姓名", Browsable = false, Type = "string" });
            properties.Add(new KeyDescription { Key = "LastUpdateTime", Description = "最后修改时间", Browsable = false, Type = "DateTime" });
            properties.Add(new KeyDescription { Key = "UpdateUserId", Description = "更新者ID", Browsable = false, Type = "int" });
            properties.Add(new KeyDescription { Key = "UpdateUserName", Description = "更新者姓名", Browsable = false, Type = "string" });
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
        [Ncf.AreaBase.Admin.Filters.CustomerResource("video-add", "video-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] AddOrUpdateVideoReq req)
        {
            await _videoService.CreateOrUpdateAsync(req);
            return Ok(true);
        }
        /// <summary>
        /// 删除
		/// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("video-delete")]
        public IActionResult OnPostDelete([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                _videoService.DeleteObject(id);
            }
            return Ok(ids.Length);
        }
    }
}
