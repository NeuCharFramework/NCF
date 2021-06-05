
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkShop.Xncf.WebApiDemo01.Models.DatabaseModel.Config
{
    /// <summary>
    /// 允许扩展名类
    /// </summary>
    public class AllowFileExtension
    {
        /// <summary>
        /// 图片扩展名
        /// </summary>
        public static readonly HashSet<string> ImageExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp"
        };
        /// <summary>
        /// 音视频扩展名
        /// </summary>
        public static readonly HashSet<string> VideoExtensiong = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".flv", ".swf", ".mkv", ".avi", ".rm", ".rmvb", ".mpeg", ".mpg",
            ".ogg", ".ogv", ".mov", ".wmv", ".mp4", ".webm", ".mp3", ".wav", ".mid"
        };
        /// <summary>
        /// 文件扩展名
        /// </summary>
        public static readonly HashSet<string> FileExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp",
            ".flv", ".swf", ".mkv", ".avi", ".rm", ".rmvb", ".mpeg", ".mpg",
            ".ogg", ".ogv", ".mov", ".wmv", ".mp4", ".webm", ".mp3", ".wav", ".mid",
            ".rar", ".zip", ".tar", ".gz", ".7z", ".bz2", ".cab", ".iso",
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf", ".txt", ".md", ".xml"
        };
    }
}
