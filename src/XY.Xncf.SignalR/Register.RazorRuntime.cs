using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.IO;

namespace XY.Xncf.SignalR
{
    public partial class Register : IXncfRazorRuntimeCompilation
    {
        public string LibraryPath => Path.Combine(SiteConfig.WebRootPath, "..", "..", "..", "XY.Xncf.SignalR", "src", "XY.Xncf.SignalR");
    }
}
