using System.Linq;
using UnityEditor;

namespace CodeWriter.Localization.Excel
{
    public class ExcelLocalizationProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var excelImporter = EditorLocalizationSettings.importer as ExcelLocalizationImporter;
            if (excelImporter == null)
                return;

            if (!excelImporter.autoReimport)
                return;

            var excelPathes = excelImporter.excelFilesPathes;

            if (importedAssets.Any(asset => excelPathes.Contains(asset)))
            {
                excelImporter.Import();
            }
        }
    }
}