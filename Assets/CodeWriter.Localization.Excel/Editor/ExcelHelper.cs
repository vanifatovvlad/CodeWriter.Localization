using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CodeWriter.Localization.Excel
{
    public static class ExcelHelper
    {
        private static string[] m_excelFileExtensions = new[] { ".xls", ".xlsx" };

        public static bool IsExcelFileAsset(Object asset)
        {
            if (!AssetDatabase.IsMainAsset(asset))
                return false;

            var path = AssetDatabase.GetAssetPath(asset);
            return IsExcelFilePath(path);
        }

        public static bool IsExcelFilePath(string path)
        {
            var extension = Path.GetExtension(path);
            return m_excelFileExtensions.Contains(extension);
        }
    }
}