using System;
using UnityEngine;

namespace CodeWriter.Localization
{
    public abstract class LocalizationImporter : ScriptableObject, ILocalizationImporter
    {
        public abstract bool CanImport();
        public abstract void Import();
    }

    public interface ILocalizationImporter
    {
        bool CanImport();
        void Import();
    }

    internal class NullImporter : ILocalizationImporter
    {
        public static readonly NullImporter Instance = new NullImporter();

        public bool CanImport()
        {
            return false;
        }

        public void Import()
        {
            throw new InvalidOperationException();
        }
    }
}