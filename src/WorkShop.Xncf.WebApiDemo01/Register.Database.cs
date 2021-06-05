#define RELEASE

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using WorkShop.Xncf.WebApiDemo01.Utils;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace WorkShop.Xncf.WebApiDemo01
{
    public partial class Register : IXncfDatabase  //注册 XNCF 模块数据库（按需选用）
    {
        #region IXncfDatabase 接口

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public const string DATABASE_PREFIX = "WorkShop_WebApiDemo01_";

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
            //ex. services.AddScoped(typeof(Color));

#if RELEASE
            //生成之前先注释一下代码

            services.Configure<StaticResourceSetting>(FileServerConfiguration.GetSection("FileServer:StaticResource"));

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
#endif
        }

        #endregion
    }
}
