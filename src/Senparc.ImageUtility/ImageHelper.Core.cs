using System;
using System.Drawing;
using System.IO;
//using Jillzhang.GifUtility;

namespace Senparc.ImageUtility
{
    public partial class ImageHelper
    {
        const string ThumbnailPrefix = "thumbnail";

        /// <summary>
        /// You must create a delegate and pass a reference to the delegate as the callback parameter, but the delegate is not used.
        /// </summary>
        /// <remarks>https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.getthumbnailimage?view=netcore-2.2</remarks>
        /// <returns></returns>
        private static bool GetThumbnailImageCallBack()
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="isLocked"></param>
        /// <param name="outputPath"></param>
        /// <param name="isoldfilename"></param>
        /// <param name="isDel"></param>
        /// <returns></returns>
        public static string GetThumbnail(Stream sourceStream, string orignalFullFileName, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        {
            int outputWidth;
            int outputHeight;
            string thumbnailFileName;
            string thumbnailFileNameWithoutExtension;
            string thumbnailPath;
            using (Image image = Bitmap.FromStream(sourceStream))
            {
                if (isLocked)
                {
                    if (image.Width > width)
                    {
                        outputWidth = width;
                        outputHeight = (image.Height * width) / image.Width;
                    }
                    else
                    {
                        outputWidth = image.Width;
                        outputHeight = image.Height;
                    }
                }
                else
                {
                    outputWidth = width;
                    outputHeight = height;
                }
                using (Image thumbnail = image.GetThumbnailImage(outputWidth, outputHeight, GetThumbnailImageCallBack, IntPtr.Zero))
                {
                    if (isoldfilename)
                    {
                        thumbnailFileNameWithoutExtension = $"{System.IO.Path.GetFileNameWithoutExtension(orignalFullFileName)}_{ThumbnailPrefix}";
                    }
                    else
                    {
                        thumbnailFileNameWithoutExtension = $"{DateTime.Now.Ticks}{Guid.NewGuid().ToString("n").Substring(0, 8)}_{ThumbnailPrefix}";
                    }
                    thumbnailFileName = $"{thumbnailFileNameWithoutExtension}{System.IO.Path.GetExtension(orignalFullFileName)}";

                    thumbnailPath = System.IO.Path.Combine(outputPath, thumbnailFileName);
                    thumbnail.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            if (isDel && System.IO.File.Exists(orignalFullFileName))
            {
                System.IO.File.Delete(orignalFullFileName);
            }
            return thumbnailFileName;
        }

        public static string GetThumbnail(string sourceFilePath, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        {
            using (FileStream fs = new FileStream(sourceFilePath, FileMode.Open))
            {
                return GetThumbnail(fs, sourceFilePath, width, height, isLocked, outputPath, isoldfilename, isDel);
            }
        }

        //public static string GetThumbnail(IFormFile formFile, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        //{
        //    using (Stream stream = formFile.OpenReadStream())
        //    {
        //        return GetThumbnail(stream, formFile.FileName, width, height, isLocked, outputPath, isoldfilename, isDel);
        //    }
        //}
        //public static bool GetThumbnail(string sourceFilePath, int width, int height, bool isLocked, string outputPath, bool isoldfilename, bool isDel)
        //{
        //    string outputFilePath;
        //    if (!File.Exists(sourceFilePath))
        //    {
        //        throw new IOException(string.Format("文件{0}不存在!", sourceFilePath));
        //    }
        //    //如果是文件夹路径
        //    string Path = HttpContext.Current.Server.MapPath(outputPath);
        //    Path = System.IO.Path.GetDirectoryName(Path);
        //    string filename = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
        //    outputFilePath = System.IO.Path.Combine(Path, filename + ".jpg");


        //    int outputWidth, outputHeight;
        //    using (Image sourceImg = Image.FromFile(sourceFilePath))
        //    {
        //        if (isLocked)
        //        {
        //            //if (sourceImg.Width > sourceImg.Height)
        //            //{
        //            if (sourceImg.Width > width)
        //            {
        //                outputWidth = width;
        //                outputHeight = (sourceImg.Height * width) / sourceImg.Width;
        //            }
        //            else
        //            {
        //                outputWidth = sourceImg.Width;
        //                outputHeight = sourceImg.Height;
        //            }
        //        }
        //        else
        //        {
        //            outputWidth = width;
        //            outputHeight = height;
        //        }
        //        using (Bitmap bitmap = new Bitmap(outputWidth, outputHeight))
        //        {
        //            using (Graphics g = Graphics.FromImage(bitmap))
        //            {
        //                g.Clear(Color.FromName("white"));
        //                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        //                g.DrawImage(sourceImg, new Rectangle(0, 0, outputWidth, outputHeight), new Rectangle(0, 0, sourceImg.Width, sourceImg.Height), GraphicsUnit.Pixel);
        //                bitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        //            }
        //        }

        //    }
        //    if (isDel)
        //    {
        //        DeleteFile(sourceFilePath);
        //    }
        //    return true;

        //}

        ///// <summary>
        ///// 缩略图片 
        ///// </summary>
        ///// <param name="sourcePath">源文件物理路径+文件名</param>
        ///// <param name="width">缩放到的宽度</param>
        ///// <param name="height">缩放到的高度</param>
        ///// <param name="locked">是否保持原始宽高比例</param>
        ///// <param name="outputPath">存放文件夹+文件名</param>
        ///// <param name="isDel">是否删除源文件</param>
        ///// <returns>文件物理路径</returns>
        //public static string GetThumbnail(string sourceFilePath, int width, int height, bool isLocked, string outputPath, bool isDelOriginalFile)
        //{
        //    string outputFilePath;
        //    if (!File.Exists(sourceFilePath))
        //    {
        //        throw new IOException(string.Format("文件{0}不存在!", sourceFilePath));
        //    }
        //    //如果是文件夹路径
        //    //string Path = HttpContext.Current.Server.MapPath(outputPath);
        //    //Path = System.IO.Path.GetDirectoryName(Path);
        //    //string filename = System.IO.Path.GetFileName(sourceFilePath);
        //    //outputFilePath = System.IO.Path.Combine(Path, filename);

        //    outputFilePath = (outputPath.StartsWith("~/")) ? HttpContext.Current.Server.MapPath(outputPath) : outputPath;//如果是应用程序相对路径，则
        //    //转换为物理路径
        //    int outputWidth, outputHeight;
        //    using (Image sourceImg = Image.FromFile(sourceFilePath))
        //    {
        //        if (isLocked)
        //        {
        //            if (sourceImg.Width > sourceImg.Height)
        //            {
        //                if (sourceImg.Width > width)
        //                {
        //                    outputWidth = width;
        //                    outputHeight = (sourceImg.Height * width) / sourceImg.Width;
        //                }
        //                else
        //                {
        //                    outputWidth = sourceImg.Width;
        //                    outputHeight = sourceImg.Height;
        //                }
        //            }
        //            else
        //            {
        //                if (sourceImg.Height > height)
        //                {
        //                    double rate = (height / (double)sourceImg.Height);
        //                    outputWidth = (int)(sourceImg.Width * rate);
        //                    outputHeight = height;
        //                }
        //                else
        //                {
        //                    outputWidth = sourceImg.Width;
        //                    outputHeight = sourceImg.Height;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            outputWidth = width;
        //            outputHeight = height;
        //        }
        //        if (sourceImg.RawFormat.Guid == ImageFormat.Gif.Guid)
        //        {
        //            double rate = 1;
        //            if (width < sourceImg.Width)
        //            {
        //                rate = ((double)width) / ((double)sourceImg.Width);
        //            }
        //            sourceImg.Dispose();
        //            GifHelper gif = new GifHelper();
        //            gif.GetThumbnail(sourceFilePath, rate, outputFilePath);
        //        }
        //        else
        //        {
        //            using (Bitmap bitmap = new Bitmap(outputWidth, outputHeight))
        //            {
        //                using (Graphics g = Graphics.FromImage(bitmap))
        //                {
        //                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        //                    g.DrawImage(sourceImg, new Rectangle(0, 0, outputWidth, outputHeight), new Rectangle(0, 0, sourceImg.Width, sourceImg.Height), GraphicsUnit.Pixel);
        //                    bitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        //                }
        //            }
        //        }
        //    }
        //    if (isDelOriginalFile)
        //    {
        //        DeleteFile(sourceFilePath);
        //    }
        //    return System.IO.Path.GetFileName(outputFilePath);
        //}

        /// <summary>
        /// 取随机文件名
        /// </summary>
        /// <returns></returns>
        public static string GetRndFileName()
        {
            Random rnd = new Random();
            string fp = DateTime.Now.ToString("yyyyMMddhhmmss") + rnd.Next(1000, 9999).ToString();
            return fp;
        }

        //public static void UpdateFileName(string sourceFileName, string newFileName)
        //{
        //    File.Move(sourceFileName, HttpContext.Current.Server.MapPath(newFileName));
        //}

        #region 私有方法

        /// <summary>
        /// 是否是文件路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool isFilePath(string path)
        {
            if (System.IO.Path.HasExtension(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath"></param>
        private static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    throw new IOException(string.Format("删除文件出错！{0},错误代码:{1}", filePath, ex.Message));
                }
            }
        }


        #endregion
    }
}
