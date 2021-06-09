using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Senparc.Xncf.MaQueKeTang.Migrations.Migrations.SqlServer
{
    public partial class AddSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_MaQueKeTang_Color",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                    AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                    LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                    AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                    Remark = table.Column<string>(nullable: true, comment: "说明"),
                    Red = table.Column<int>(nullable: false),
                    Green = table.Column<int>(nullable: false),
                    Blue = table.Column<int>(nullable: false),
                    AdditionNote = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_MaQueKeTang_Color", x => x.Id);
                });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_CourseAttach",
            columns: table => new
            {
                CourseId = table.Column<int>(nullable: false, comment: "课程Id"),
                name = table.Column<string>(nullable: false, comment: "附件名"),
                path = table.Column<string>(nullable: false, comment: "路径"),
                disk = table.Column<string>(nullable: false, comment: "存储磁盘"),
                size = table.Column<int>(nullable: false, comment: "单位：byte"),
                extension = table.Column<string>(nullable: false, comment: "文件格式"),
                only_buyer = table.Column<byte>(nullable: false, comment: "只有购买者可以下载,1是,0否"),
                download_times = table.Column<int>(nullable: false, comment: "下载次数"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseAttach", x => x.Id);
            });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_CourseComment",
            columns: table => new
            {
                AccountId = table.Column<int>(nullable: false, comment: "用户Id"),
                CourseId = table.Column<int>(nullable: false, comment: "课程Id"),
                original_content = table.Column<string>(nullable: false, comment: "原始内容"),
                render_content = table.Column<string>(nullable: false, comment: "渲染后的内容"),
                deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseComment", x => x.Id);
            });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_CourseUserRecord",
            columns: table => new
            {
                CourseId = table.Column<int>(nullable: false, comment: "课程Id"),
                AccountId = table.Column<int>(nullable: false, comment: "用户Id"),
                deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                is_watched = table.Column<byte>(nullable: false, comment: "看完,0否,1是"),
                watched_at = table.Column<DateTime>(nullable: true, comment: "观看时间"),
                progress = table.Column<byte>(nullable: false, comment: "进度"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseUserRecord", x => x.Id);
            });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_Course",
            columns: table => new
            {
                AccountId = table.Column<int>(nullable: false, comment: "用户Id"),
                title = table.Column<string>(nullable: false, comment: "名"),
                slug = table.Column<string>(nullable: false, comment: "slug"),
                thumb = table.Column<string>(nullable: false, comment: "封面"),
                charge = table.Column<int>(nullable: false, comment: "收费"),
                short_description = table.Column<string>(nullable: false, comment: "简短介绍"),
                original_desc = table.Column<string>(nullable: false, comment: "简介原始内容"),
                render_desc = table.Column<string>(nullable: false, comment: "简介渲染后的内容"),
                seo_keywords = table.Column<string>(nullable: false, comment: "SEO关键字"),
                seo_description = table.Column<string>(nullable: false, comment: "SEO描述"),
                published_at = table.Column<DateTime>(nullable: true, comment: "上线时间"),
                is_show = table.Column<byte>(nullable: false, comment: "1显示,-1隐藏"),
                category_id = table.Column<int>(nullable: false, comment: "分类id"),
                is_rec = table.Column<byte>(nullable: false, comment: "推荐,0否,1是"),
                user_count = table.Column<int>(nullable: false, comment: "学习人数"),
                is_free = table.Column<byte>(nullable: false, comment: "免费,0否,1是"),
                comment_status = table.Column<byte>(nullable: false, comment: "0关闭评论,1所有人,2仅购买"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_Course", x => x.Id);
            });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_CourseCategory",
            columns: table => new
            {
                name = table.Column<string>(nullable: false, comment: "分类名"),
                parent_id = table.Column<int>(nullable: false, comment: "父id"),
                parent_name = table.Column<string>(nullable: false, comment: "父级名称"),
                is_show = table.Column<byte>(nullable: false, comment: "是否显示,1显示,0不显示"),
                sort = table.Column<int>(nullable: false, comment: "升序"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseCategory", x => x.Id);
            });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_CourseUser",
            columns: table => new
            {
                AccountId = table.Column<int>(nullable: false, comment: "用户Id"),
                CourseId = table.Column<int>(nullable: false, comment: "课程Id"),
                charge = table.Column<int>(nullable: false, comment: "收费"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseUser", x => x.Id);
            });
            migrationBuilder.CreateTable(
            name: "Senparc_MaQueKeTang_CourseUserLike",
            columns: table => new
            {
                AccountId = table.Column<int>(nullable: false, comment: "用户Id"),
                CourseId = table.Column<int>(nullable: false, comment: "课程Id"),
                Flag = table.Column<bool>(nullable: false, comment: "删除状态"),
                AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后修改时间"),
                AdminRemark = table.Column<string>(nullable: true, comment: "备注"),
                Remark = table.Column<string>(nullable: true, comment: "说明"),
                TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseUserLike", x => x.Id);
            });
            migrationBuilder.CreateTable(
                name: "Senparc_MaQueKeTang_CourseChapter",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1"),
                    ParentId = table.Column<int>(nullable: false, comment: "父级章节Id"),
                    ParentName = table.Column<string>(nullable: false, comment: "父级章节名称"),
                    CourseId = table.Column<int>(nullable: false, comment: "课程Id"),
                    title = table.Column<string>(nullable: false, comment: "标题"),
                    sort = table.Column<int>(nullable: false, comment: "排序"),
                    deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                    AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                    AddUserId = table.Column<string>(nullable: false, comment: "添加者Id"),
                    AddUserName = table.Column<string>(nullable: false, comment: "添加者名称"),
                    LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后更新时间"),
                    UpdateUserId = table.Column<string>(nullable: false, comment: "更新者Id"),
                    UpdateUserName = table.Column<string>(nullable: false, comment: "更新者名称"),
                    AdminRemark = table.Column<string>(nullable: false, comment: "备注"),
                    Remark = table.Column<string>(nullable: false, comment: "说明"),
                    TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                    Flag = table.Column<bool>(nullable: false, comment: "删除状态")
                },
                constraints: table => {
                    table.PrimaryKey("PK_Senparc_MaQueKeTang_CourseChapter", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Senparc_MaQueKeTang_Video",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false, comment: "用户ID"),
                    CourseId = table.Column<int>(nullable: false, comment: "课程ID"),
                    Title = table.Column<string>(nullable: false, comment: "标题"),
                    Slug = table.Column<string>(nullable: false, comment: "slug"),
                    Url = table.Column<string>(nullable: false, comment: "播放地址"),
                    Charge = table.Column<int>(nullable: false, comment: "价格"),
                    ViewNum = table.Column<int>(nullable: false, comment: "观看次数"),
                    ShortDescription = table.Column<string>(nullable: false, comment: "简短介绍"),
                    OriginalDesc = table.Column<string>(nullable: false, comment: "简介原始内容"),
                    RenderDesc = table.Column<string>(nullable: false, comment: "简介渲染后的内容"),
                    SeoKeywords = table.Column<string>(nullable: false, comment: "SEO关键字"),
                    SeoDescription = table.Column<string>(nullable: false, comment: "SEO描述"),
                    PublishedAt = table.Column<DateTime>(nullable: true, comment: "上线时间"),
                    IsShow = table.Column<byte>(nullable: false, comment: "1显示,-1隐藏"),
                    AliyunVideoId = table.Column<string>(nullable: true, comment: "阿里云视频ID"),
                    ChapterId = table.Column<int>(nullable: false, comment: "章节ID"),
                    Duration = table.Column<int>(nullable: false, comment: "时长，单位：秒"),
                    TencentVideoId = table.Column<string>(nullable: true, comment: "腾讯云video_id"),
                    IsBanSell = table.Column<byte>(nullable: false, comment: "禁止售卖,0否,1是"),
                    CommentStatus = table.Column<byte>(nullable: false, comment: "0禁止评论,1所有人,2仅购买"),
                    FreeSeconds = table.Column<int>(nullable: false, comment: "试看秒数"),
                    PlayerPC = table.Column<string>(nullable: false, comment: "pc播放器"),
                    PlayerH5 = table.Column<string>(nullable: false, comment: "h5播放器"),
                    BanDrag = table.Column<byte>(nullable: false, comment: "禁止拖动,0否,1是"),
                    deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                    AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                    AddUserId = table.Column<string>(nullable: false, comment: "添加者Id"),
                    AddUserName = table.Column<string>(nullable: false, comment: "添加者名称"),
                    LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后更新时间"),
                    UpdateUserId = table.Column<string>(nullable: false, comment: "更新者Id"),
                    UpdateUserName = table.Column<string>(nullable: false, comment: "更新者名称"),
                    AdminRemark = table.Column<string>(nullable: false, comment: "备注"),
                    Remark = table.Column<string>(nullable: false, comment: "说明"),
                    TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                    Flag = table.Column<bool>(nullable: false, comment: "删除状态")
                },
                constraints: table => {
                    table.PrimaryKey("PK_Senparc_MaQueKeTang_Video", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Senparc_MaQueKeTang_VideoUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false, comment: "用户ID"),
                    VideoId = table.Column<int>(nullable: false, comment: "视频ID"),
                    Charge = table.Column<int>(nullable: false, comment: "收费"),
                    deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                    AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                    AddUserId = table.Column<string>(nullable: false, comment: "添加者Id"),
                    AddUserName = table.Column<string>(nullable: false, comment: "添加者名称"),
                    LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后更新时间"),
                    UpdateUserId = table.Column<string>(nullable: false, comment: "更新者Id"),
                    UpdateUserName = table.Column<string>(nullable: false, comment: "更新者名称"),
                    AdminRemark = table.Column<string>(nullable: false, comment: "备注"),
                    Remark = table.Column<string>(nullable: false, comment: "说明"),
                    TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                    Flag = table.Column<bool>(nullable: false, comment: "删除状态")
                },
                constraints: table => {
                    table.PrimaryKey("PK_Senparc_MaQueKeTang_VideoUser", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Senparc_MaQueKeTang_VideoUserComment",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false, comment: "用户ID"),
                    VideoId = table.Column<int>(nullable: false, comment: "视频ID"),
                    OriginalContent = table.Column<string>(nullable: false, comment: "原始内容"),
                    RenderContent = table.Column<string>(nullable: false, comment: "渲染后的内容"),
                    deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                    AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                    AddUserId = table.Column<string>(nullable: false, comment: "添加者Id"),
                    AddUserName = table.Column<string>(nullable: false, comment: "添加者名称"),
                    LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后更新时间"),
                    UpdateUserId = table.Column<string>(nullable: false, comment: "更新者Id"),
                    UpdateUserName = table.Column<string>(nullable: false, comment: "更新者名称"),
                    AdminRemark = table.Column<string>(nullable: false, comment: "备注"),
                    Remark = table.Column<string>(nullable: false, comment: "说明"),
                    TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                    Flag = table.Column<bool>(nullable: false, comment: "删除状态")
                },
                constraints: table => {
                    table.PrimaryKey("PK_Senparc_MaQueKeTang_VideoUserComment", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Senparc_MaQueKeTang_VideoUserWatchRecord",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false, comment: "Id").Annotation("SqlServer: Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false, comment: "用户ID"),
                    CourseId = table.Column<int>(nullable: false, comment: "课程ID"),
                    VideoId = table.Column<int>(nullable: false, comment: "视频ID"),
                    WatchSeconds = table.Column<int>(nullable: false, comment: "已观看秒数"),
                    WatchedAt = table.Column<DateTime>(nullable: true, comment: "看完时间"),
                    deleted_at = table.Column<DateTime>(nullable: true, comment: "删除时间"),
                    AddTime = table.Column<DateTime>(nullable: false, comment: "添加时间"),
                    AddUserId = table.Column<string>(nullable: false, comment: "添加者Id"),
                    AddUserName = table.Column<string>(nullable: false, comment: "添加者名称"),
                    LastUpdateTime = table.Column<DateTime>(nullable: false, comment: "最后更新时间"),
                    UpdateUserId = table.Column<string>(nullable: false, comment: "更新者Id"),
                    UpdateUserName = table.Column<string>(nullable: false, comment: "更新者名称"),
                    AdminRemark = table.Column<string>(nullable: false, comment: "备注"),
                    Remark = table.Column<string>(nullable: false, comment: "说明"),
                    TenantId = table.Column<int>(nullable: false, comment: "租户Id"),
                    Flag = table.Column<bool>(nullable: false, comment: "删除状态")
                },
                constraints: table => {
                    table.PrimaryKey("PK_Senparc_MaQueKeTang_VideoUserWatchRecord", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_MaQueKeTang_Color");
        }
    }
}

