using Senparc.Ncf.Core.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Interface
{
    public class Config : IXncfInterfaceConfig
    {
        public static string XncfName => "Senparc.Xncf.Installer";
        //public string XncfAspireProjectName => XncfName.Replace(".", "_");
    }
}
