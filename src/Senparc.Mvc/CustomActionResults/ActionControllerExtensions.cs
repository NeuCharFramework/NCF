using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Senparc.Mvc.CustomActionResults;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Mvc
{
    public static class ActionControllerExtensions
    {
        public static ZipResult Zip(this Controller controller, string zipFilename, IEnumerable<string> filenames)
        {
            return Zip(controller, zipFilename, filenames, null);
        }

        public static ZipResult Zip(this Controller controller, string zipFilename, IEnumerable<string> filenames, Dictionary<string, string> newFilesFromString)
        {
            return new ZipResult(null,zipFilename, filenames, newFilesFromString,null);
        }

        public static ActionResult CheckCode(this Controller controller, CheckCodeKind checoCodeKind)
        {
            return new CheckCodeResult(checoCodeKind);
        }
    }
}
