using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace CodeWriter.Localization
{
    public sealed class EditorLocalizationSettings : ScriptableObject
    {
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
                    var guids = AssetDatabase.FindAssets("t:" + typeof(EditorLocalizationSettings).Name);                
                    Assert.AreEqual(guids.Length, 1, "Expected only one EditorLocalizationSettings instance.");

                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    m_instance = AssetDatabase.LoadAssetAtPath<EditorLocalizationSettings>(path);
                    Assert.IsNotNull(m_instance, "Failed to load EditorLocalizationSettings instance.");
                }
                return m_instance;
            }
        }
        
        public static LocalizationSettings settings
        {
            get { return instance.m_settings; }
        }

        public static ILocalizationImporter importer
        {
            get { return (ILocalizationImporter)instance.m_importer ?? NullImporter.Instance; }
        }        
    }
}