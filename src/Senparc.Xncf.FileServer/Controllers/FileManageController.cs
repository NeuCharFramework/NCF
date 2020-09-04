using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Senparc.Xncf.FileServer.Models;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileServer.Models.VD;
using Senparc.Xncf.FileServer.Services;
using Senparc.Xncf.FileServer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileServer.Controllers
{
    /// <summary>
    /// 文件上传接口
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiController]
    [ApiVersion("1")]
    public class FileManageController : ControllerBase
    {
        private readonly IOptionsMonitor<StaticResourceSetting> staticResourceSetting;
        private readonly SysKeyService _sysKeyService;
        private readonly FileRecordService _fileRecordService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileManageController(IOptionsMonitor<StaticResourceSetting> staticResourceSetting, SysKeyService sysKeyService, FileRecordService fileRecordService, IWebHostEnvironment webHostEnvironment)
        {
            this.staticResourceSetting = staticResourceSetting;
            this._sysKeyService = sysKeyService;
            this._fileRecordService = fileRecordService;
            this._webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="file">文件信息</param>
        /// <param name="token">认证token</param>
        /// <param name="prefixPath">前置存储路径，将影响文件的实际存放路径</param>
        /// <returns></returns>
        [HttpPost]
        [HttpOptions]
        public async Task<ActionResultVD> Upload([FromForm, BindRequired] IFormFile file, [FromForm, BindRequired] string token, [FromForm] string prefixPath = null)
        {
            var vd = new ActionResultVD();
            try
            {
                var file_data = file;
                if (file_data == null)
                {
                    vd.Set(ARTag.invalid);
                    vd.Msg = "文件参数无效，请提供name值为file_data的文件";
                    return vd;
                }
                if (string.IsNullOrEmpty(token))
                {
                    vd.Set(ARTag.invalid);
                    vd.Msg = "请提供token。";
                    return vd;
                }
                //验证文件扩展名
                var extension = Path.GetExtension(file_data.FileName);
                if (!AllowFileExtension.FileExtension.Contains(extension))
                {
                    vd.Set(ARTag.invalid);
                    vd.Msg = "不支持此扩展名文件的上传！";
                    return vd;
                }
                var sysKeyModel = await _sysKeyService.ValidToken(token);
                //基础存储路径
                var basePath = sysKeyModel.Name;
                //验证文件前缀路径有效性
                if (!string.IsNullOrWhiteSpace(prefixPath))
                {
                    if (prefixPath.IndexOfAny(Path.GetInvalidPathChars()) > -1)//验证路径有效性
                    {
                        vd.Code = ARTag.invalid.GetHashCode();
                        vd.Msg = "无效路径！";
                        return vd;
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

                var fileRecordModel = new FileRecord()
                {
                    FileContentType = file_data.ContentType,
                    FileHash = file_data.GetHashCode().ToString(),
                    FileSize = (int)file_data.Length,
                    PrefixPath = prefixPath,
                    SysKeyId = sysKeyModel.Id
                };
                fileRecordModel.FileName = file_data.FileName.Split('\\').LastOrDefault();

                var filename = $"{DateTime.Now:yyyyMMddHHmmss}-{UniqueHelper.LongId()}{extension}";
                using (var fs = new FileStream(Path.Combine(pathMp, filename), FileMode.CreateNew))
                {
                    file_data.CopyTo(fs);
                    fileRecordModel.FileHash = HashHelper.SHA1File(fs);//赋值文件Hash值
                    fs.Flush();
                }

                fileRecordModel.FilePath = Path.Combine(basePath, filename);

                await _fileRecordService.AddAsync(fileRecordModel);
                vd.Set(true);
                //更改返回使用的返回路径
                fileRecordModel.FilePath = Path.Combine(staticResourceSetting.CurrentValue.RequestPath, basePath, filename).Replace("\\", "/");//物理文件路径(斜杠)转换为URL路径(反斜杠)
                vd.Data = _fileRecordService.Mapper.Map<FileRecordDto>(fileRecordModel);
            }
            catch (Exception ex)
            {
                vd.Set(ex);
            }
            return vd;
        }
    }
}
