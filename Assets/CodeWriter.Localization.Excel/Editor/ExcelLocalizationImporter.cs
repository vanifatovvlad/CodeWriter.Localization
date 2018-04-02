using System.IO;
using System.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace CodeWriter.Localization.Excel
{
    public sealed class ExcelLocalizationImporter : LocalizationImporter
    {
        [SerializeField]
        private Object[] m_excelFiles;

        [SerializeField]
        private bool m_autoReimport = true;

        public Object[] excelFiles
        {
            get { return m_excelFiles; }
        }

        public string[] excelFilesPathes
        {
            get { return m_excelFiles.Select(f => AssetDatabase.GetAssetPath(f)).ToArray(); }
        }

        public bool autoReimport
        {
            get { return m_autoReimport; }
        }

        public override bool CanImport()
        {
            return EditorLocalizationSettings.settings != null;
        }
        
        public override void Import()
        {
            try
            {
                var builder = new LocalizationSettingsBuilder(EditorLocalizationSettings.settings);

                foreach (var path in excelFilesPathes)
                {
                    if (!File.Exists(path))
                        throw new UnityException(string.Format("File '{0}' not exists", path));

                    using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var book = (Path.GetExtension(path) == ".xls")
                            ? (IWorkbook)new HSSFWorkbook(stream)
                            : (IWorkbook)new XSSFWorkbook(stream);

                        ImportWorkbook(builder, book);
                    }
                }

                builder.Apply();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ImportWorkbook(LocalizationSettingsBuilder builder, IWorkbook workbook)
        {
            for (int sheetNum = 0; sheetNum < workbook.NumberOfSheets; sheetNum++)
            {
                ImportSheet(builder, workbook.GetSheetAt(sheetNum));
            }
        }

        private void ImportSheet(LocalizationSettingsBuilder builder, ISheet sheet)
        {
            var header = sheet.GetRow(0);
            
            var locales = header.Skip(1).TakeWhile(c => c != null)
                .Select(cell => cell.StringCellValue)
                .ToArray();

            var progressBarHeader = string.Format("Import {0}", sheet.SheetName);

            for (int rowNum = 1; rowNum <= sheet.LastRowNum; rowNum++)
            {
                IRow row = sheet.GetRow(rowNum);

                if (row == null)
                    continue;

                var key = row.GetCell(0);
                if (key == null || string.IsNullOrEmpty(key.StringCellValue))
                    continue;

                var keyString = key.StringCellValue;

                EditorUtility.DisplayProgressBar(progressBarHeader, keyString, (float)rowNum / sheet.LastRowNum);

                for (int cellNum = 1; cellNum < row.LastCellNum; cellNum++)
                {
                    var cell = row.GetCell(cellNum);
                    if (cell != null)
                    {
                        var locale = locales[cellNum - 1];
                        builder.AddWord(locale, keyString, cell.StringCellValue);
                    }
                }
            }
        }        
    }
}