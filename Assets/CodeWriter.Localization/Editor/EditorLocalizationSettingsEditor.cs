using CodeWriter.Localization.Helpers;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodeWriter.Localization
{
    [CustomEditor(typeof(EditorLocalizationSettings))]
    public class EditorLocalizationSettingsEditor : Editor
    {
        private static Type[] m_importerTypes;
        private static string[] m_importerNames;

        private SerializedProperty m_importerProperty;
        private SerializedProperty m_settingsProperty;
        private Editor m_importerEditor;

        void OnEnable()
        {
            if (m_importerTypes == null)
            {
                m_importerTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(asm => asm.GetTypes())
                    .Where(type => type.GetCustomAttributes(typeof(CustomLocalizationImporterAttribute), false).Length > 0)
                    .ToArray();

                m_importerNames = Array.ConvertAll(m_importerTypes, t =>
                {
                    var attr = (CustomLocalizationImporterAttribute)t.GetCustomAttributes(typeof(CustomLocalizationImporterAttribute), false)[0];
                    return attr.name;
                });
            }

            m_importerProperty = serializedObject.FindProperty("m_importer");
            m_settingsProperty = serializedObject.FindProperty("m_settings");

            TryCreateImporterEditor();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.ObjectField(m_settingsProperty.displayName, EditorLocalizationSettings.settings, typeof(LocalizationSettings), false);
            DoImporterDropDown();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                TryCreateImporterEditor();
            }

            GUILayout.Space(5);

            if (m_importerEditor)
            {
                GUILayout.Label(m_importerEditor.target.name, EditorStyles.boldLabel);
                m_importerEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("No importer selected", MessageType.Error);
            }

            GUILayout.Space(5);

            DoImportButton();

            serializedObject.ApplyModifiedProperties();
        }

        private void TryCreateImporterEditor()
        {
            var target = m_importerProperty.objectReferenceValue;
            if (target == null)
                return;

            if (m_importerEditor)
                DestroyImmediate(m_importerEditor);

            m_importerEditor = CreateEditor(target);
        }

        private void DoImporterDropDown()
        {
            var name = m_importerProperty.objectReferenceValue
                ? m_importerNames[Array.IndexOf(m_importerTypes, m_importerProperty.objectReferenceValue.GetType())]
                : string.Empty;
            var index = Array.IndexOf(m_importerNames, name);

            var newIndex = EditorGUILayout.Popup(m_importerProperty.displayName, index, m_importerNames);
            if (newIndex != index)
            {
                m_importerProperty.objectReferenceValue = GetImporter(m_importerNames[newIndex], m_importerTypes[newIndex]);
            }
        }

        private void DoImportButton()
        {
            GUI.enabled = EditorLocalizationSettings.importer.CanImport();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Import", GUILayout.Width(230), GUILayout.Height(23)))
            {
                EditorLocalizationSettings.importer.Import();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        private static LocalizationImporter GetImporter(string name, Type type)
        {
            var guids = AssetDatabaseHelper.FindAsset<LocalizationImporter>();
            if (guids.Length == 0)
            {
                var instance = (LocalizationImporter)ScriptableObject.CreateInstance(type);

                var fileName = string.Format("LocalizationPrefs.{0}.asset", name);
                AssetDatabaseHelper.CreateFolder(EditorLocalizationSettings.prefsDefaultPath);
                AssetDatabase.Refresh();
                AssetDatabaseHelper.CreateAsset(instance, fileName, EditorLocalizationSettings.prefsDefaultPath);

                return instance;
            }
            else
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<LocalizationImporter>(path);
            }
        }
    }
}