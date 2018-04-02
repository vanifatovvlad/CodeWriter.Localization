using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodeWriter.Localization
{
    [CustomEditor(typeof(EditorLocalizationSettings))]
    public class EditorLocalizationSettingsEditor : Editor
    {
        private LocalizationImporter[] m_importerAssets;
        private string[] m_importerNames;

        private SerializedProperty m_importerProperty;
        private SerializedProperty m_settingsProperty;
        private Editor m_importerEditor;

        void OnEnable()
        {
            m_importerAssets = AssetDatabase.FindAssets("t:" + typeof(LocalizationImporter).Name)
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<LocalizationImporter>(path))
                .ToArray();

            m_importerNames = m_importerAssets.Select(o => o.name).ToArray();

            m_importerProperty = serializedObject.FindProperty("m_importer");
            m_settingsProperty = serializedObject.FindProperty("m_settings");

            TryCreateImporterEditor();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_settingsProperty);

            DoImporterDropDown();
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                TryCreateImporterEditor();
            }

            GUILayout.Space(5);

            if (m_importerEditor)
            {
                GUILayout.Label(m_importerEditor.target.name + " Settings", EditorStyles.boldLabel);
                m_importerEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("No importer selected", MessageType.Error);
            }

            GUILayout.Space(5);

            DoImportButton();
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
            var name = m_importerProperty.objectReferenceValue ? m_importerProperty.objectReferenceValue.name : string.Empty;
            var index = Array.IndexOf(m_importerNames, name);
            var newIndex = EditorGUILayout.Popup(m_importerProperty.displayName, index, m_importerNames);
            if (newIndex != index)
            {
                m_importerProperty.objectReferenceValue = m_importerAssets[newIndex];
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
    }
}