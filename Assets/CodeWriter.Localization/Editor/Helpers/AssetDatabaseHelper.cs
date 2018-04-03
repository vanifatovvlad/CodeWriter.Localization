using UnityEditor;
using System;

namespace CodeWriter.Localization.Helpers
{
    public static class AssetDatabaseHelper
    {
        public static string[] FindAsset(Type type)
        {
            return AssetDatabase.FindAssets("t:" + type.Name);
        }

        public static string[] FindAsset<T>() where T : UnityEngine.Object
        {
            return FindAsset(typeof(T));
        }

        public static void CreateAsset(UnityEngine.Object asset, string name, params string[] folders)
        {
            AssetDatabase.CreateAsset(asset, string.Join("/", folders) + "/" + name);
        }

        public static void CreateFolder(string path)
        {
            CreateFolder(path.Split('/'));
        }

        public static void CreateFolder(params string[] folders)
        {
            if (folders == null)
                throw new ArgumentNullException("folders");

            if (folders.Length < 1 || folders[0] != "Assets")
                throw new ArgumentException("First folder must be 'Assets'");

            var path = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                var folder = folders[i];
                var newPath = path + "/" + folder;

                if (!AssetDatabase.IsValidFolder(newPath))
                    AssetDatabase.CreateFolder(path, folder);

                path = newPath;
            }
        }
    }
}