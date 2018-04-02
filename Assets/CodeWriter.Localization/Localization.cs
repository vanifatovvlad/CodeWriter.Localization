using System;
using UnityEngine;

namespace CodeWriter.Localization
{
    public static class Localization
    {
        private static readonly object[] m_args0 = new object[0];
        private static readonly object[] m_args1 = new object[2];
        private static readonly object[] m_args2 = new object[4];
        private static readonly object[] m_args3 = new object[6];

        private static LocalizationSettings m_settings;
        private static LocalizationLocaleSettings m_currentLocaleSettings;

        public static string currentLocale
        {
            get { return m_currentLocaleSettings.name; }
        }

        public static void Initialize(LocalizationSettings settings)
        {
            m_settings = settings;
            m_currentLocaleSettings = m_settings.locales[0];
        }

        public static bool SetLocale(string locale)
        {
            foreach (var localeSettings in m_settings.locales)
            {
                if (localeSettings.name.Equals(locale, StringComparison.OrdinalIgnoreCase))
                {
                    m_currentLocaleSettings = localeSettings;
                    return true;
                }
            }

            Debug.LogWarningFormat("Localization.SetLocale(): locale '{0}' not exists", locale);
            return false;
        }

        public static string Localize(string key)
        {
            return Localize(key, m_args0);
        }

        public static string Localize(string key, string arg1, object value1)
        {
            try
            {
                m_args1[0] = arg1; m_args1[1] = value1;

                return Localize(key, m_args1);
            }
            finally
            {
                m_args1[0] = m_args1[1] = null;
            }
        }

        public static string Localize(string key, string arg1, object value1, string arg2, object value2)
        {
            try
            {
                m_args2[0] = arg1; m_args2[1] = value1;
                m_args2[2] = arg2; m_args2[3] = value2;

                return Localize(key, m_args2);
            }
            finally
            {
                m_args2[0] = m_args2[1] = m_args2[2] = m_args2[3] = null;
            }
        }

        public static string Localize(string key, string arg1, object value1, string arg2, object value2, string arg3, object value3)
        {
            try
            {
                m_args3[0] = arg1; m_args3[1] = value1;
                m_args3[2] = arg2; m_args3[3] = value2;
                m_args3[4] = arg3; m_args3[5] = value3;

                return Localize(key, m_args3);
            }
            finally
            {
                m_args3[0] = m_args3[1] = m_args3[2] = m_args3[3] = m_args3[4] = m_args3[5] = null;
            }
        }

        public static string Localize(string key, params object[] args)
        {
            int keyIndex = Array.BinarySearch(m_settings.keys, key, StringComparer.Ordinal);
            if (keyIndex < 0)
            {
                Debug.LogWarningFormat("Localization.Localize(): key '{0}' not exists", key);
                return key;
            }

            return Localize(keyIndex, args);
        }

        public static string Localize(int keyIndex, object[] args)
        {
            var text = m_currentLocaleSettings.words[keyIndex];
            return StringUtils.Replace(text, args);
        }
    }
}