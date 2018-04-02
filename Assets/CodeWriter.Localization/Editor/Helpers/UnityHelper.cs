using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CodeWriter.Localization.Helpers
{
    internal static class UnityHelper
    {
        private static Func<Rect, string, string> EditorGUI_SearchField;

        static UnityHelper()
        {
            var method = typeof(UnityEditor.Editor).Assembly.GetTypes()
               .First(o => o.Name.Equals("EditorGUI", StringComparison.Ordinal))
               .GetMethod("SearchField", BindingFlags.Static | BindingFlags.NonPublic);

            EditorGUI_SearchField = (Func<Rect, string, string>)Delegate.CreateDelegate(typeof(Func<Rect, string, string>), method, true);
        }

        public static string SearchField(Rect position, string text)
        {
            //UnityEditor.EditorGUI.SearchField(Rect position, string text)
            return EditorGUI_SearchField(position, text);
        }

        public static Rect GUIToScreenRect(Rect guiRect)
        {
            Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            guiRect.x = vector.x;
            guiRect.y = vector.y;
            return guiRect;
        }
    }
}