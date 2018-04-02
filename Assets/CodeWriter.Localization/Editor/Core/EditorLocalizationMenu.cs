using UnityEditor;

namespace CodeWriter.Localization
{
    public static class EditorLocalizationMenu
    {
        [MenuItem("Tools/Localization/Import")]
        static void Import()
        {
            EditorLocalizationSettings.importer.Import();
        }

        [MenuItem("Tools/Localization/Import", validate = true)]
        static bool CanImport()
        {
            return EditorLocalizationSettings.importer.CanImport();
        }

        [MenuItem("Tools/Localization/Settings")]
        static void Edit()
        {
            EditorGUIUtility.PingObject(EditorLocalizationSettings.instance);
        }
    }
}