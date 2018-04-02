using UnityEngine;

namespace CodeWriter.Localization
{
    [CreateAssetMenu(fileName = "Localization", menuName = "CodeWriter/Localization Asset")]
    public sealed class LocalizationSettings : ScriptableObject
    {
        [SerializeField] LocalizationLocaleSettings[] m_locales;
        [SerializeField] string[] m_keys;

        public LocalizationLocaleSettings[] locales
        {
            get { return m_locales; }
        }

        public string[] keys
        {
            get { return m_keys; }
        }
    }
}