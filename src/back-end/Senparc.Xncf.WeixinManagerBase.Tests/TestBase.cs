using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.XncfBase;
using System.Text;

namespace Senparc.Xncf.WeixinManagerBase.Tests
{
    [TestClass]
    public class TestBase
    {
        public static IServiceCollection ServiceCollection { get; private set; }
        public static IConfiguration Configuration { get; set; }

        public static IHostEnvironment Env { get; set; }

        protected static IRegisterService registerService;
        protected static SenparcSetting _senparcSetting;


        public TestBase()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            RegisterServiceCollection();


            RegisterServiceStart();


        }

        /// <summary>
        /// ×¢²á IServiceCollection ºÍ MemoryCache
        /// </summary>
        public static void RegisterServiceCollection()
        {
            ServiceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            ServiceCollection.AddSenparcGlobalServices(config);
            ServiceCollection.AddMemoryCache();//Ê¹ÓÃÄÚ´æ»º´æ
        }

        /// <summary>
        /// ×¢²á RegisterService.Start()
        /// </summary>
        public static void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //×¢²á
            var mockEnv = new Mock<IHostEnvironment/*IHostingEnvironment*/>();
            mockEnv.Setup(z => z.ContentRootPath).Returns(() => UnitTestHelper.RootPath);

            Env = mockEnv.Object;

            registerService = Senparc.CO2NET.AspNet.RegisterServices.RegisterService.Start(Env, _senparcSetting)
                .UseSenparcGlobal(autoScanExtensionCacheStrategies);

            StartNcfEngine();

        }

        private static void StartNcfEngine()
        {
            var result = ServiceCollection.StartEngine(TestBase.Configuration, TestBase.Env);
            Console.WriteLine(result);
            //Assert.IsTrue(Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList.Count > 0);
        }
    }
}