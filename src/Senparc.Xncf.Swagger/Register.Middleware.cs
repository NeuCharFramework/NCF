using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Swagger.Builder;
using System;
using System.IO;

namespace Senparc.Xncf.Swagger
{
    public partial class Register : IXncfMiddleware
    {
        public IApplicationBuilder UseMiddleware(IApplicationBuilder app)
        {
            app.UseSwaggerCustom();
            return app;
        }
    }
}
