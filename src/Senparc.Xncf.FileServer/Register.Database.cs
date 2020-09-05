using Senparc.Xncf.FileServer.Models.DatabaseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase;
using System;
using Senparc.Xncf.FileServer.Utils;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.Features;
using Senparc.Xncf.FileServer.Services;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Hosting;
using Senparc.Xncf.FileServer.Models;
using Senparc.Ncf.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Senparc.Xncf.Swagger.Utils;

namespace Senparc.Xncf.FileServer
{
    public partial class Register :
        IXncfDatabase  //注册 XNCF 模块数据库（按需选用）
    {
        #region IXncfDatabase 接口

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public const string DATABASE_PREFIX = "FileServer_";

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;
        /// <summary>
        /// 设置 XncfSenparcEntities 类型
        /// </summary>
        public Type XncfDatabaseDbContextType => typeof(FileServerSenparcEntities);


        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            modelBuilder.ApplyConfiguration(new FileServer_FileRecordConfigurationMapping());
            modelBuilder.ApplyConfiguration(new FileServer_SysKeyConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //缓存注册
            services.AddMemoryCache();

            //应用服务注册
            services.AddScoped<SysKeyService>();
            //services.AddScoped<ServiceBase<SysKey>>();
            services.AddScoped<SysKey>();
            services.AddScoped<SysKeyDto>();
            services.AddScoped<FileRecord>();
            services.AddScoped<FileRecordDto>();
            //AutoMap映射
            base.AddAutoMapMapping(profile =>
            {
                //SysKey
                profile.CreateMap<SysKeyDto, SysKey>();
                profile.CreateMap<SysKey, SysKeyDto>();
                //FileRecord
                profile.CreateMap<FileRecordDto, FileRecord>();
                profile.CreateMap<FileRecord, FileRecordDto>();
            });

            //配置映射
            services.Configure<SafeSetting>(FileServerConfiguration.GetSection("FileServer:Safe"));
            services.Configure<StaticResourceSetting>(FileServerConfiguration.GetSection("FileServer:StaticResource"));
            services.AddControllersWithViews()/*.AddRazorRuntimeCompilation()*/;
            //开发时：安装该包可以动态修改视图 cshtml 页面，无需重新运行项目
            //发布时：建议删除该包，会生成一堆“垃圾”
            //Install-Package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation

            //services.AddControllers().AddNewtonsoftJson(options =>
            //{
            //    //Action原样输出JSON
            //    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
            //    //日期格式化
            //    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss.fff";
            //});

            //路由小写
            services.AddRouting(options => { options.LowercaseUrls = true; });
            //配置上传文件大小限制（详细信息：FormOptions）
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = FileServerConfiguration.GetValue<int>("FileServer:StaticResource:MaxSize") * 1024 * 1024;
            });
            //跨域
            var domains = FileServerConfiguration.GetSection("Cors:Domain").Get<string[]>();
            var allowedOrigins = new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase);
            services.AddCors(options => options.AddDefaultPolicy(builder =>
            {
                //builder.AllowCredentials();
                builder.SetIsOriginAllowed(origin => allowedOrigins.Contains(new Uri(origin).Host));
            }));
            ////版本控制
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddApiVersioning(x =>
            {
                x.DefaultApiVersion = new ApiVersion(1, 0);
                x.AssumeDefaultVersionWhenUnspecified = true;
                x.ReportApiVersions = true;
                x.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
            });
        }
        #endregion
    }
}
