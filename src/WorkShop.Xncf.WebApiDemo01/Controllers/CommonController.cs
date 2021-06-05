
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Cache;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Cache.CsRedis;
using WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.VO;
using WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.Config;
using WorkShop.Xncf.WebApiDemo01.Utils;
using WorkShop.Xncf.WebApiDemo01.Services;

namespace WorkShop.Xncf.WebApiDemo01.Controllers
{
    /// <summary>
    /// 文件上传接口
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiController]
    [ApiVersion("2")]
    public class CommonController : BaseController
    {
        private readonly IOptionsMonitor<StaticResourceSetting> staticResourceSetting;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CommonController(IOptionsMonitor<StaticResourceSetting> staticResourceSetting, IWebHostEnvironment webHostEnvironment)
        {
            this.staticResourceSetting = staticResourceSetting;
            this._webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Demo()
        {
            return Ok("Hello UEditor");
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="file">文件信息</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Upload([FromForm] IFormFile file)
        {
            string prefixPath = string.Empty;
            try
            {
                var file_data = this.Request.Form.Files[0];
                if (file_data == null)
                {
                    return Fail("文件参数无效，请提供name值为file_data的文件");
                }
                //验证文件扩展名
                var extension = Path.GetExtension(file_data.FileName);
                if (!AllowFileExtension.FileExtension.Contains(extension))
                {
                    return Fail("不支持此扩展名文件的上传！");
                }
                //基础存储路径
                var basePath = "default"; // sysKeyModel.Name;
                //验证文件前缀路径有效性
                if (!string.IsNullOrWhiteSpace(prefixPath))
                {
                    if (prefixPath.IndexOfAny(Path.GetInvalidPathChars()) > -1)//验证路径有效性
                    {
                        return Fail("无效路径！");
                    }
                    //进一步规范路径
                    var invalidPattern = new Regex(@"[\\\/\:\*\?\042\<\>\|]");
                    prefixPath = invalidPattern.Replace(prefixPath, "");

                    prefixPath = prefixPath.Replace("_", "\\");//使用下划线“_”代替斜杠“\”
                    basePath = Path.Combine(basePath, prefixPath);
                }
                //物理文件路径
                var pathMp = Path.Combine(_webHostEnvironment.ContentRootPath, staticResourceSetting.CurrentValue.RootDir, basePath);
                if (!Directory.Exists(pathMp)) Directory.CreateDirectory(pathMp);

                string strFileName = file_data.FileName.Split('\\').LastOrDefault();

                var filename = $"{DateTime.Now:yyyyMMddHHmmss}-{UniqueHelper.LongId()}{extension}";
                string strFileHash = string.Empty;
                string strFilePath = string.Empty;
                using (var fs = new FileStream(Path.Combine(pathMp, filename), FileMode.CreateNew))
                {
                    file_data.CopyTo(fs);
                    strFileHash = HashHelper.SHA1File(fs);//赋值文件Hash值
                    fs.Flush();
                }
                ////物理文件路径(斜杠)转换为URL路径(反斜杠)
                strFilePath = $"{Request.Scheme}://{Request.Host.Value}/static/default/{filename}";
                return Success(strFilePath);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="file">文件信息</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult EditorUpload([FromForm] IFormFile file)
        {
            string prefixPath = string.Empty;
            try
            {
                var file_data = this.Request.Form.Files[0];
                if (file_data == null)
                {
                    return Fail("文件参数无效，请提供name值为file_data的文件");
                }
                //验证文件扩展名
                var extension = Path.GetExtension(file_data.FileName);
                if (!AllowFileExtension.FileExtension.Contains(extension))
                {
                    return Fail("不支持此扩展名文件的上传！");
                }
                //基础存储路径
                var basePath = "default"; // sysKeyModel.Name;
                //验证文件前缀路径有效性
                if (!string.IsNullOrWhiteSpace(prefixPath))
                {
                    if (prefixPath.IndexOfAny(Path.GetInvalidPathChars()) > -1)//验证路径有效性
                    {
                        return Fail("无效路径！");
                    }
                    //进一步规范路径
                    var invalidPattern = new Regex(@"[\\\/\:\*\?\042\<\>\|]");
                    prefixPath = invalidPattern.Replace(prefixPath, "");

                    prefixPath = prefixPath.Replace("_", "\\");//使用下划线“_”代替斜杠“\”
                    basePath = Path.Combine(basePath, prefixPath);
                }
                //物理文件路径
                var pathMp = Path.Combine(_webHostEnvironment.ContentRootPath, staticResourceSetting.CurrentValue.RootDir, basePath);
                if (!Directory.Exists(pathMp)) Directory.CreateDirectory(pathMp);

                string strFileName = file_data.FileName.Split('\\').LastOrDefault();

                var filename = $"{DateTime.Now:yyyyMMddHHmmss}-{UniqueHelper.LongId()}{extension}";
                string strFileHash = string.Empty;
                string strFilePath = string.Empty;
                using (var fs = new FileStream(Path.Combine(pathMp, filename), FileMode.CreateNew))
                {
                    file_data.CopyTo(fs);
                    strFileHash = HashHelper.SHA1File(fs);//赋值文件Hash值
                    fs.Flush();
                }
                ////物理文件路径(斜杠)转换为URL路径(反斜杠)
                strFilePath = $"{Request.Scheme}://{Request.Host.Value}/static/default/{filename}";
                var response = new
                {
                    uploaded = 1,
                    url = strFilePath
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new
                {
                    uploaded = 0,
                    url = ex.Message
                };
                return Ok(response);
            }
        }
    }
}
