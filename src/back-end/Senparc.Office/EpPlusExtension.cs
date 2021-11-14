using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Office
{

    /// <summary>
    ///
    ///<example>https://github.com/JanKallman/EPPlus</example> 
    /// </summary>
    public static class EpPlusExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static ExcelWorksheet GetWorksheet(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The path is empty.");
            }
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                throw new NullReferenceException("file not found");
            }
            var excelPackage = new ExcelPackage(fileInfo);
            if (excelPackage.Workbook.Worksheets.Count <= 0)
            {
                throw new NullReferenceException("worksheet not found");
            }
            return excelPackage.Workbook.Worksheets.FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ExcelWorksheet GetWorksheet(MemoryStream stream)
        {
            var excelPackage = new ExcelPackage(stream);
            if (excelPackage.Workbook.Worksheets.Count <= 0)
            {
                throw new NullReferenceException("worksheet not found");
            }
            return excelPackage.Workbook.Worksheets.FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<ExcelWorksheet> GetWorksheetAsync(IFormFile file)
        {
            var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream).ConfigureAwait(false);
            return GetWorksheet(memoryStream);
        }

        public static string ReadToString(ExcelWorksheet worksheet)
        {
            var rowCount = worksheet.Dimension?.Rows;
            var colCount = worksheet.Dimension?.Columns;

            if (!rowCount.HasValue || !colCount.HasValue)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int row = 1; row <= rowCount.Value; row++)
            {
                for (int col = 1; col <= colCount.Value; col++)
                {
                    sb.AppendFormat("{0}\t", worksheet.Cells[row, col].Value);
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
