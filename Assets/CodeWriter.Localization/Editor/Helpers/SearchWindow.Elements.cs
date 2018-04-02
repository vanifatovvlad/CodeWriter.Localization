using UnityEditor;
using UnityEngine;

namespace CodeWriter.Localization.Helpers
{
    internal partial class SearchWindow : EditorWindow
    {
        internal class GroupElement : Element
        {
            public Vector2 scroll;
            public int selectedIndex;

            public GroupElement(string name)
            {
                this.content = new GUIContent(name);
            }

            protected GroupElement()
            { }
        }

        internal class SearchRootElement : GroupElement
        {
            public SearchRootElement(string name) : base(name)
            { }
        }
    }
}