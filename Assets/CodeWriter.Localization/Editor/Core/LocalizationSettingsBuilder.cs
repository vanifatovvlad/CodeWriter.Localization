using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodeWriter.Localization
{
    public class LocalizationSettingsBuilder
    {
        private readonly Dictionary<string, SerializedProperty> m_localeToWords = new Dictionary<string, SerializedProperty>();
        private readonly List<string> m_keys = new List<string>();

        private readonly LocalizationSettings m_asset;
        private readonly SerializedObject m_serializedObject;

        private readonly SerializedProperty m_localesProperty;
        private readonly SerializedProperty m_keysProperty;

        public LocalizationSettingsBuilder(LocalizationSettings asset)
        {
            m_asset = asset;
            m_serializedObject = new SerializedObject(m_asset);

            m_keysProperty = m_serializedObject.FindProperty("m_keys");
            m_localesProperty = m_serializedObject.FindProperty("m_locales");

            Clear();
        }

        public void Apply()
        {
            if (m_localeToWords.Count == 0)
            {
                GetOrCreateLocaleWords("en");
            }

            foreach (var word in m_localeToWords.Values)
            {
                word.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            m_serializedObject.ApplyModifiedPropertiesWithoutUndo();

            m_localeToWords.Clear();
            m_keys.Clear();
        }

        public void AddWord(string locale, string key, string value)
        {
            var wordsProperty = GetOrCreateLocaleWords(locale);

            int index = m_keys.BinarySearch(key, StringComparer.Ordinal);
            if (index < 0)
            {
                index = ~index;

                m_keys.Insert(index, key);

                m_keysProperty.InsertArrayElementAtIndex(index);
                m_keysProperty.GetArrayElementAtIndex(index).stringValue = key;

                foreach (var word in m_localeToWords)
                {
                    word.Value.InsertArrayElementAtIndex(index);
                }
            }

            wordsProperty.GetArrayElementAtIndex(index).stringValue = value;
        }

        private void Clear()
        {
            m_localesProperty.arraySize = 0;
            m_keysProperty.arraySize = 0;

            var assetPath = AssetDatabase.GetAssetPath(m_asset);
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(AssetDatabase.IsSubAsset);

            foreach (var subAsset in subAssets)
            {
                ScriptableObject.DestroyImmediate(subAsset, true);
            }

            AssetDatabase.Refresh();
        }

        private SerializedProperty GetOrCreateLocaleWords(string locale)
        {
            SerializedProperty wordsProperty;
            if (m_localeToWords.TryGetValue(locale, out wordsProperty))
            {
                return wordsProperty;
            }

            var localeAsset = ScriptableObject.CreateInstance<LocalizationLocaleSettings>();
            localeAsset.name = locale;
            localeAsset.hideFlags |= HideFlags.HideInHierarchy;
            localeAsset.hideFlags |= HideFlags.NotEditable;
            AssetDatabase.AddObjectToAsset(localeAsset, m_asset);

            wordsProperty = new SerializedObject(localeAsset).FindProperty("m_words");
            wordsProperty.arraySize = m_keys.Count;

            int localeIndex = m_localesProperty.arraySize++;
            m_localesProperty.GetArrayElementAtIndex(localeIndex).objectReferenceValue = localeAsset;
            m_localeToWords.Add(locale, wordsProperty);

            return wordsProperty;
        }
    }
}