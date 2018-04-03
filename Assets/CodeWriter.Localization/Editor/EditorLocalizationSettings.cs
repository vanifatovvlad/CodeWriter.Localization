using CodeWriter.Localization.Helpers;
using UnityEditor;
using UnityEngine;

namespace CodeWriter.Localization
{
    public sealed class EditorLocalizationSettings : ScriptableObject
    {
        internal static readonly string[] prefsDefaultPath = new[] { "Assets", "Editor", "Localization" };
        internal static readonly string[] assetDefaultPath = new[] { "Assets", "Resources" };

        [SerializeField]
        private LocalizationSettings m_settings;

        [SerializeField]
        private LocalizationImporter m_importer;

        private static EditorLocalizationSettings m_instance;

        public static EditorLocalizationSettings instance
        {
            get
            {
                if (m_instance == null)
                {
                    var guids = AssetDatabaseHelper.FindAsset<EditorLocalizationSettings>();
                    if (guids.Length == 0)
                    {
                        var instance = ScriptableObject.CreateInstance<EditorLocalizationSettings>();

                        AssetDatabaseHelper.CreateFolder(prefsDefaultPath);
                        AssetDatabase.Refresh();
                        AssetDatabaseHelper.CreateAsset(instance, "LocalizationPrefs.asset", prefsDefaultPath);

                        m_instance = instance;
                    }
                    else
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        m_instance = AssetDatabase.LoadAssetAtPath<EditorLocalizationSettings>(path);
                    }
                }
                return m_instance;
            }
        }

        public static LocalizationSettings settings
        {
            get
            {
                if (instance.m_settings == null)
                {
                    var guids = AssetDatabaseHelper.FindAsset<LocalizationSettings>();
                    if (guids.Length == 0)
                    {
                        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();

                        AssetDatabaseHelper.CreateFolder(assetDefaultPath);
                        AssetDatabase.Refresh();
                        AssetDatabaseHelper.CreateAsset(settings, "Localization.asset", assetDefaultPath);

                        instance.m_settings = settings;
                    }
                    else
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        instance.m_settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(path);
                    }
                }

                return instance.m_settings;
            }
        }

        public static ILocalizationImporter importer
        {
            get { return (ILocalizationImporter)instance.m_importer ?? NullImporter.Instance; }
        }
    }
}