using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Swagger.Builder;
using Senparc.Xncf.Swagger.Models;
using Senparc.Xncf.Swagger.Utils;

namespace Senparc.Xncf.Swagger
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IRegister 接口

        public override string Name => "Senparc.Xncf.Swagger";

        public override string Uid => "712d56f6-989e-4b5f-b769-86a870543e8d";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "1.0";//必须填写版本号

        public override string MenuName => "接口说明文档";

        public override string Icon => "fa fa-file-code-o";

        public override string Description => "接口说明文档";

        public override IList<Type> Functions => new Type[] {  /*typeof(BuildXncf)*/ };

        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            var serviceProvider = services.BuildServiceProvider();
            ConfigurationHelper.Configuration = configuration;
            ConfigurationHelper.HostEnvironment = serviceProvider.GetService<IWebHostEnvironment>();
            ConfigurationHelper.WebHostEnvironment = serviceProvider.GetService<IWebHostEnvironment>();
            ConfigurationHelper.SwaggerConfiguration = new ConfigurationBuilder().AddJsonFile("swaggerconfig.json", optional: true, reloadOnChange: true).Build();

            services.Configure<CustsomSwaggerOptions>(ConfigurationHelper.SwaggerConfiguration.GetSection("Swagger"));

            ConfigurationHelper.CustsomSwaggerOptions = ConfigurationHelper.SwaggerConfiguration.GetSection("Swagger").Get<CustsomSwaggerOptions>();
            ConfigurationHelper.CustsomSwaggerOptions.AddSwaggerGenAction = c =>
            {
                var xmlList = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.AllDirectories);
                foreach (var xml in xmlList)
                {
                    c.IncludeXmlComments(xml, true);
                }
            };
            ConfigurationHelper.CustsomSwaggerOptions.UseSwaggerAction = c => { };
            ConfigurationHelper.CustsomSwaggerOptions.UseSwaggerUIAction = c => { };

            //接口文档
            #region swagger
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddApiVersioning(x =>
                {
                    x.DefaultApiVersion = new ApiVersion(1, 0);
                    x.AssumeDefaultVersionWhenUnspecified = true;
                    x.ReportApiVersions = true;
                    x.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
                });
            services.AddSwaggerCustom();
            #endregion

            return base.AddXncfModule(services, configuration);
        }
    }
}
