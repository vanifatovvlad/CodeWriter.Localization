using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeWriter.Localization.Helpers;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace CodeWriter.Localization.Excel
{
    [CustomEditor(typeof(ExcelLocalizationImporter))]
    public sealed class ExcelLocalizationImporterEditor : Editor
    {
        private ReorderableList m_excelFiles;
        private SerializedProperty m_autoReimportProperty;

        public new ExcelLocalizationImporter target
        {
            get { return (ExcelLocalizationImporter)base.target; }
        }

        void OnEnable()
        {
            m_autoReimportProperty = serializedObject.FindProperty("m_autoReimport");

            m_excelFiles = new ReorderableList(serializedObject, serializedObject.FindProperty("m_excelFiles"), true, true, true, true);
            m_excelFiles.drawHeaderCallback = rect =>
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    var eventType = Event.current.type;
                    if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences.All(r => ExcelHelper.IsExcelFileAsset(r)))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                            if (eventType == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();

                                foreach (var asset in DragAndDrop.objectReferences)
                                {
                                    AddExcelFile(asset);
                                }
                            }

                            Event.current.Use();
                        }
                    }
                }
                GUI.Label(rect, m_excelFiles.serializedProperty.displayName);
            };
            m_excelFiles.elementHeight = EditorGUIUtility.singleLineHeight;
            m_excelFiles.drawElementCallback = (rect, index, active, focus) =>
            {
                var fileProperty = m_excelFiles.serializedProperty.GetArrayElementAtIndex(index);

                Assert.IsNotNull(fileProperty.objectReferenceValue);

                var name = fileProperty.objectReferenceValue.name;
                if (focus)
                {
                    if (GUI.Button(rect, name, GUI.skin.label))
                    {
                        EditorGUIUtility.PingObject(fileProperty.objectReferenceValue);
                    }
                }
                else
                {
                    GUI.Label(rect, name);
                }
            };
            m_excelFiles.onAddDropdownCallback = (buttonRect, list) =>
            {
                var searchRect = new Rect(buttonRect) { x = buttonRect.xMax - 300, width = 300 };
                var builder = new ExcelAssetsTreeBuilder(excluded: target.excelFilesPathes);
                SearchWindow.Show(searchRect, builder, element =>
                {
                    var assetElement = (ExcelAssetsTreeBuilder.AssetElement)element;
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(assetElement.path);
                    AddExcelFile(asset);
                });
            };
            m_excelFiles.onRemoveCallback = list =>
            {
                var index = list.index;
                m_excelFiles.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
                m_excelFiles.serializedProperty.DeleteArrayElementAtIndex(index);
                m_excelFiles.serializedProperty.serializedObject.ApplyModifiedProperties();
            };
        }

        private void AddExcelFile(Object asset)
        {
            if (asset == null)
                return;

            if (!ExcelHelper.IsExcelFileAsset(asset))
                return;

            if (target.excelFiles.Contains(asset))
                return;

            int index = m_excelFiles.serializedProperty.arraySize++;
            m_excelFiles.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            m_excelFiles.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private class ExcelAssetsTreeBuilder : SearchWindow.ITreeBuilder
        {
            private SearchWindow.TreeBuilder m_builder;
            private IEnumerable<string> m_excluded;

            public ExcelAssetsTreeBuilder(IEnumerable<string> excluded)
            {
                m_excluded = excluded;
                m_builder = new SearchWindow.TreeBuilder(null, "Project");
            }

            public SearchWindow.Element[] Build()
            {
                m_builder.Clear();

                var pathes = AssetDatabase.FindAssets("t:" + typeof(Object).Name)
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Except(m_excluded)
                    .Where(path => ExcelHelper.IsExcelFilePath(path));

                foreach (var path in pathes)
                {
                    m_builder.Item(path, new AssetElement(path));
                }

                return m_builder.Build();
            }

            public class AssetElement : SearchWindow.Element, SearchWindow.ISearchableElement
            {
                public string path { get; private set; }
                public string searchText { get { return content.text; } }

                public AssetElement(string path)
                {
                    this.path = path;
                    content = new GUIContent(Path.GetFileNameWithoutExtension(path), path);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_autoReimportProperty);
            GUILayout.Space(5);
            m_excelFiles.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}