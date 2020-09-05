using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Senparc.Xncf.Swagger.Filters
{
    /// <summary>
    /// 添加swagger接口版本默认值
    /// </summary>
    public class SwaggerDefaultValueFilter : IOperationFilter
    {
        /// <summary>
        /// 将指定的筛选器应用于context
        /// </summary>
        /// <param name="operation">要应用筛选器</param>
        /// <param name="context">当前操作筛选上下文</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/412
            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/pull/413
            foreach (var parameter in operation.Parameters/*.OfType<NonBodyParameter>()*/)
            {
                var description = context.ApiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == parameter.Name);
                if (description == null)
                    return;
                if (parameter.Description == null)
                {
                    parameter.Description = description.ModelMetadata.Description;
                }

                if (description.RouteInfo != null)
                {
                    parameter.Required |= !description.RouteInfo.IsOptional;
                    //if (parameter.Default == null)
                    //    parameter.Default = description.RouteInfo.DefaultValue;
                }
                //else
                //{
                //    parameter.Required = description.ModelMetadata.IsRequired;
                //}
            }
        }
    }
}
