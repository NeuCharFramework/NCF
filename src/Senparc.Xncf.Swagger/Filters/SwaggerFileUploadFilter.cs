using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.Swagger.Filters
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        private static readonly string[] FormFilePropertyNames = typeof(IFormFile).GetTypeInfo().DeclaredProperties.Select(x => x.Name).ToArray();
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
               !context.ApiDescription.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase) &&
               !context.ApiDescription.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileParameters = context.ApiDescription.ActionDescriptor.Parameters.Where(n => n.ParameterType == typeof(IFormFile)).ToList();
            if (fileParameters.Count < 0)
            {
                return;
            }

            foreach (var fileParameter in fileParameters)
            {
                var kvList = operation.RequestBody.Content
                    .Where(x => x.Key == "multipart/form-data");
                foreach (var kv in kvList)
                {
                    var formFileParameters = kv.Value.Schema.Properties
                           .Where(x => FormFilePropertyNames.Contains(x.Key))
                           .ToArray();
                    foreach (var formFileParameter in formFileParameters)
                    {
                        kv.Value.Schema.Properties.Remove(formFileParameter);
                    }

                    kv.Value.Schema.Properties.Add("file_data", new OpenApiSchema()
                    {
                        Title = "上传文件",
                        Type = "file",
                        Description = "要上传的文件"
                    });
                    kv.Value.Schema.Required.Add("file_data");
                }
            }
        }
    }
}
