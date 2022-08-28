using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;

namespace Senparc.Xncf.WeixinManagerBase.Tests
{
    [TestClass]
    public class TestBase
    {
        public IServiceCollection ServiceCollection { get; }
        public static IConfiguration Configuration { get; set; }

        public static IHostEnvironment Env { get; set; }

        protected static IRegisterService registerService;
        protected static SenparcSetting _senparcSetting;

        public TestBase()
        {

        }
    }
}