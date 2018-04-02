using UnityEngine;

namespace CodeWriter.Localization
{
    public sealed class LocalizationLocaleSettings : ScriptableObject
    {
        [SerializeField] string[] m_words;

        public string[] words
        {
            get { return m_words; }
        }
    }
}