using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.File
{
    public static class FileExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="outPath">绝对路径</param>
        /// <returns></returns>
        public static async Task<bool> Upload(IFormFile formFile, string outPath)
        {
            if (formFile == null || formFile.Length <= 0)
            {
                throw new NullReferenceException("IFormFile is null");
            }
            // full path to file in temp location
            var filePath = Path.GetDirectoryName(outPath);
            if (!System.IO.File.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            using (var stream = new FileStream(outPath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
                stream.Flush();
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="fileName"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static async Task<bool> Upload(IFormFile formFile, string fileName, string paths)
        {
            if (formFile == null || formFile.Length <= 0)
            {
                throw new NullReferenceException("IFormFile is null");
            }
            // full path to file in temp location
            if (!System.IO.File.Exists(paths))
            {
                Directory.CreateDirectory(paths);
            }
            var filePath = Path.Combine(paths, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
                stream.Flush();
            }
            return true;
        }
    }
}
