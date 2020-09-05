using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Linq;

namespace Senparc.Xncf.Swagger.Convention
{
    /// <summary>
    /// 按约定分配对文档的操作
    /// 可以按照以下约定以根据控制器名称空间将操作分配给文档
    /// 
    /// 需在Startup.ConfigureServices中加入以下配置
    /// services.AddMvc(c =>
    ///     c.Conventions.Add(new ApiExplorerGroupPerVersionConvention())
    /// );
    /// </summary>
    public class ApiExplorerGroupPerVersionConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var controllerNamespace = controller.ControllerType.Namespace; // e.g. "Controllers.V1"
            var apiVersion = controllerNamespace.Split('.').Last().ToLower();

            controller.ApiExplorer.GroupName = apiVersion;
        }
    }
}
