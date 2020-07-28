using System.IO;

namespace Senparc.Core.Utility
{
    public static class FileSaveUtility
    {
        public static string GetAvailableFileName(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            string fileNameWithoutExtension = filePath.Substring(0, filePath.Length - fileExtension.Length);
            string newFilePath = filePath;
            int i = 1;
            while (File.Exists(newFilePath))
            {
                newFilePath = $"{fileNameWithoutExtension}({i.ToString()}){fileExtension}";
                i++;
            }
            return newFilePath;
        }
    }
}