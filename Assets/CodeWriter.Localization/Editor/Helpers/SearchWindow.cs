using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace CodeWriter.Localization.Helpers
{
    internal partial class SearchWindow : EditorWindow
    {
        public abstract class Element
        {
            public static Comparison<Element> Comparer = (x, y) => x.name.CompareTo(y.name);

            internal int level;
            public GUIContent content = GUIContent.none;

            public string name
            {
                get { return content.text; }
            }
        }

        public interface ITreeBuilder
        {
            Element[] Build();
        }

        internal interface ICustomSearch
        {
            string search { get; set; }
            string delayedSearch { get; set; }
            void RebuildSearch();
        }

        internal interface ICustomGUI
        {
            void OnGUI(SearchWindow host);
        }

        internal interface ICustomKeyboard
        {
            void HandleKeyboard(SearchWindow host);
        }

        internal interface ISearchableElement
        {
            string searchText { get; }
        }

        private class SearchWindowWrap : ICustomKeyboard, ICustomSearch
        {
            private SearchWindow m_Host;
            public SearchWindowWrap(SearchWindow host)
            {
                m_Host = host;
            }

            public string search
            {
                get { return m_Host.m_Search; }
                set { m_Host.m_Search = value; }
            }

            public string delayedSearch
            {
                get { return m_Host.m_DelayedSearch; }
                set { m_Host.m_DelayedSearch = value; }
            }

            public void HandleKeyboard(SearchWindow host)
            {
                m_Host.HandleKeyboard();
            }

            public void RebuildSearch()
            {
                m_Host.RebuildSearch();
            }
        }

        private class PathComparer : IComparer<string>
        {
            private char m_splitter;

            public PathComparer(char pathSplitter)
            {
                m_splitter = pathSplitter;
            }

            public int Compare(string x, string y)
            {
                int xBegin = 0, xEnd = 0, yBegin = 0, yEnd = 0;
                string sNamespace = x, yNamespace = y;

                do
                {
                    xEnd = x.IndexOf(m_splitter, xBegin);
                    yEnd = y.IndexOf(m_splitter, yBegin);

                    if (xEnd == -1 || yEnd == -1)
                        break;

                    sNamespace = x.Substring(xBegin, xEnd - xBegin);
                    yNamespace = y.Substring(yBegin, yEnd - yBegin);

                    int cmp = sNamespace.CompareTo(yNamespace);
                    if (cmp != 0)
                        return cmp;

                    xBegin = xEnd + 1;
                    yBegin = yEnd + 1;
                }
                while (xEnd != -1 && yEnd != -1);

                if (xEnd != yEnd)
                    return xEnd == -1 ? 1 : -1;

                var xName = x.Substring(xBegin);
                var yName = y.Substring(yBegin);
                return xName.CompareTo(yName);
            }
        }

        private class Styles
        {
            public GUIStyle header = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("In BigTitle"));
            public GUIStyle componentButton = new GUIStyle("PR Label");
            public GUIStyle methodButton = new GUIStyle("PR Label");
            public GUIStyle groupButton;
            public GUIStyle background = "grey_border";
            public GUIStyle previewBackground = "PopupCurveSwatchBackground";
            public GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);
            public GUIStyle rightArrow = "AC RightArrow";
            public GUIStyle leftArrow = "AC LeftArrow";
            public GUIStyle miniLabel = new GUIStyle(EditorStyles.miniLabel);

            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;

                componentButton.alignment = TextAnchor.MiddleLeft;
                componentButton.padding.left -= 15;
                componentButton.fixedHeight = 20f;

                methodButton.alignment = TextAnchor.MiddleLeft;
                methodButton.padding.left += 3;
                methodButton.fixedHeight = 15f;

                groupButton = new GUIStyle(componentButton);
                groupButton.padding.left += 17;

                previewText.padding.left += 3;
                previewText.padding.right += 3;

                previewHeader.padding.left++;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }

        private static Styles s_Styles;
        private static SearchWindow s_SearchWindow;
        private static long s_LastClosedTime;

        private Element[] m_Tree;
        private Element[] m_SearchResultTree;
        private List<GroupElement> m_Stack;
        private SearchWindowWrap m_SelfWrap;

        private float m_Anim = 1f;
        private int m_AnimTarget = 1;
        private long m_LastTime;
        private bool m_ScrollToSelected;
        private string m_DelayedSearch;
        private string m_Search = string.Empty;

        private readonly List<GroupElement> m_NormalStack = new List<GroupElement>();
        private readonly List<GroupElement> m_SearchStack = new List<GroupElement>();
        private GroupElement m_SearchRoot;
        private int m_SearchRootIndex = -1;

        private Action<Element> m_callback;

        private bool hasSearch
        {
            get { return !string.IsNullOrEmpty(m_Search); }
        }

        private GroupElement activeParent
        {
            get { return m_Stack[m_Stack.Count - 2 + m_AnimTarget]; }
        }

        private Element[] activeTree
        {
            get { return hasSearch ? m_SearchResultTree : m_Tree; }
        }

        private Element activeElement
        {
            get
            {
                if (activeTree == null)
                    return null;

                List<Element> children = GetChildren(activeTree, activeParent);
                if (children.Count == 0)
                    return null;

                return children[activeParent.selectedIndex];
            }
        }

        private bool isAnimating
        {
            get { return m_Anim != m_AnimTarget; }
        }

        private SearchWindow()
        {
            m_Stack = m_NormalStack;
            m_SelfWrap = new SearchWindowWrap(this);
        }

        void OnEnable()
        {
            s_SearchWindow = this;
            m_Search = string.Empty;
        }

        void OnDisable()
        {
            s_LastClosedTime = DateTime.Now.Ticks / 10000L;
            s_SearchWindow = null;
        }

        public static SearchWindow Show(Rect rect, ITreeBuilder treeBuilder, Action<Element> callback)
        {
            var array = Resources.FindObjectsOfTypeAll<SearchWindow>();
            if (array.Length > 0)
            {
                array[0].Close();
                return null;
            }

            long time = DateTime.Now.Ticks / 10000L;
            if (time >= s_LastClosedTime + 50L)
            {
                Event.current.Use();

                if (s_SearchWindow == null)
                    s_SearchWindow = ScriptableObject.CreateInstance<SearchWindow>();

                s_SearchWindow.Init(rect, treeBuilder, callback);
                return s_SearchWindow;
            }

            return null;
        }

        private void Init(Rect buttonRect, ITreeBuilder treeBuilder, Action<Element> callback)
        {
            m_callback = callback;
            buttonRect = UnityHelper.GUIToScreenRect(buttonRect);
            var tree = treeBuilder.Build();
            InsertTree(tree);
            ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, 320));
            Focus();
            wantsMouseMove = true;
        }

        private void InsertTree(Element[] tree)
        {
            m_Tree = tree;

            if (m_Stack.Count == 0)
            {
                m_Stack.Add(m_Tree[0] as GroupElement);
            }
            else
            {
                GroupElement groupElement = m_Tree[0] as GroupElement;
                int level = 0;
                while (true)
                {
                    GroupElement groupElement2 = m_Stack[level];
                    m_Stack[level] = groupElement;
                    m_Stack[level].selectedIndex = groupElement2.selectedIndex;
                    m_Stack[level].scroll = groupElement2.scroll;
                    level++;
                    if (level == m_Stack.Count)
                    {
                        break;
                    }
                    List<Element> children = GetChildren(activeTree, groupElement);
                    Element element = children.FirstOrDefault(c => c.name == m_Stack[level].name);
                    if (element != null && element is GroupElement)
                    {
                        groupElement = (element as GroupElement);
                    }
                    else
                    {
                        while (m_Stack.Count > level)
                        {
                            m_Stack.RemoveAt(level);
                        }
                    }
                }
            }
            RebuildSearch();
        }

        void OnGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.Label(new Rect(0f, 0f, position.width, position.height), GUIContent.none, s_Styles.background);

            ((activeParent as ICustomKeyboard) ?? m_SelfWrap).HandleKeyboard(this);

            GUILayout.Space(7f);
            EditorGUI.FocusTextInControl("ComponentSearch");

            Rect rect = GUILayoutUtility.GetRect(10f, 20f);
            rect.x += 8f;
            rect.width -= 16f;
            GUI.SetNextControlName("ComponentSearch");

            var customSearch = (activeParent as ICustomSearch) ?? m_SelfWrap;
            string text = UnityHelper.SearchField(rect, customSearch.delayedSearch ?? customSearch.search);

            if (text != customSearch.search || customSearch.delayedSearch != null)
            {
                if (!isAnimating)
                {
                    customSearch.search = (customSearch.delayedSearch ?? text);
                    customSearch.RebuildSearch();
                    customSearch.delayedSearch = null;
                }
                else
                {
                    customSearch.delayedSearch = text;
                }
            }

            ListGUI(activeTree, m_Anim, GetElementRelative(0), GetElementRelative(-1));
            if (m_Anim < 1f)
            {
                ListGUI(activeTree, m_Anim + 1f, GetElementRelative(-1), GetElementRelative(-2));
            }

            if (isAnimating && Event.current.type == EventType.Repaint)
            {
                long ticks = DateTime.Now.Ticks;
                float delta = (ticks - m_LastTime) / 1E+07f;
                m_LastTime = ticks;
                m_Anim = Mathf.MoveTowards(m_Anim, m_AnimTarget, delta * 4f);
                if (m_AnimTarget == 0 && m_Anim == 0f)
                {
                    m_Anim = 1f;
                    m_AnimTarget = 1;
                    m_Stack.RemoveAt(m_Stack.Count - 1);
                }
                Repaint();
            }
        }

        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown)
                return;

            if (current.keyCode == KeyCode.DownArrow)
            {
                activeParent.selectedIndex++;
                activeParent.selectedIndex = Mathf.Min(activeParent.selectedIndex, GetChildren(activeTree, activeParent).Count - 1);
                m_ScrollToSelected = true;
                current.Use();
            }
            if (current.keyCode == KeyCode.UpArrow)
            {
                activeParent.selectedIndex--;
                activeParent.selectedIndex = Mathf.Max(activeParent.selectedIndex, 0);
                m_ScrollToSelected = true;
                current.Use();
            }

            if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
            {
                GoToChild(activeElement, true);
                current.Use();
            }

            if (!hasSearch)
            {
                if (current.keyCode == KeyCode.LeftArrow || current.keyCode == KeyCode.Backspace)
                {
                    GoToParent();
                    current.Use();
                }

                if (current.keyCode == KeyCode.RightArrow)
                {
                    GoToChild(activeElement, false);
                    current.Use();
                }

                if (current.keyCode == KeyCode.Escape)
                {
                    Close();
                    current.Use();
                }
            }
        }

        private void RebuildSearch()
        {
            if (!hasSearch)
            {
                m_SearchResultTree = null;
                if (m_Stack[m_Stack.Count - 1] is SearchRootElement)
                {
                    m_Stack = m_NormalStack;
                }
                m_AnimTarget = 1;
                m_LastTime = DateTime.Now.Ticks;
                m_SearchRoot = null;
                m_SearchRootIndex = -1;
                return;
            }

            if (m_SearchRootIndex == -1)
            {
                Assert.AreEqual(m_Stack, m_NormalStack);
                m_SearchRoot = activeParent;
                m_SearchRootIndex = Array.IndexOf(m_Tree, activeParent);
            }

            string[] searchKeys = m_Search.ToLower().Split(' ');
            List<Element> startsWithList = new List<Element>();
            List<Element> containsList = new List<Element>();
            Element[] tree = m_Tree;

            for (int i = m_SearchRootIndex; i < tree.Length; i++)
            {
                Element element = tree[i];

                if (element.level < m_SearchRoot.level)
                    break;

                var searchable = element as ISearchableElement;
                if (searchable != null)
                {
                    string elementName = searchable.searchText.ToLower();
                    bool containsKey = true;
                    bool startWithKey = false;
                    for (int j = 0; j < searchKeys.Length; j++)
                    {
                        string searchKey = searchKeys[j];
                        if (!elementName.Contains(searchKey))
                        {
                            containsKey = false;
                            break;
                        }
                        if (j == 0 && elementName.StartsWith(searchKey))
                        {
                            startWithKey = true;
                        }
                    }
                    if (containsKey)
                    {
                        if (startWithKey)
                        {
                            startsWithList.Add(element);
                        }
                        else
                        {
                            containsList.Add(element);
                        }
                    }
                }
            }
            startsWithList.Sort(Element.Comparer);
            containsList.Sort(Element.Comparer);

            List<Element> allList = new List<Element>();
            allList.Add(new SearchRootElement("Search in " + m_SearchRoot.name));
            allList.AddRange(startsWithList);
            allList.AddRange(containsList);

            m_SearchResultTree = allList.ToArray();
            m_Stack = m_SearchStack;
            m_Stack.Clear();
            m_Stack.Add(m_SearchResultTree[0] as GroupElement);
            if (GetChildren(activeTree, activeParent).Count >= 1)
            {
                activeParent.selectedIndex = 0;
            }
            else
            {
                activeParent.selectedIndex = -1;
            }
        }

        private GroupElement GetElementRelative(int rel)
        {
            int num = m_Stack.Count + rel - 1;
            return (num < 0) ? null : m_Stack[num];
        }

        private void GoToParent()
        {
            if (m_Stack.Count > 1)
            {
                m_AnimTarget = 0;
                m_LastTime = DateTime.Now.Ticks;
            }
        }

        private void GoToChild(Element e, bool addIfComponent)
        {
            if (e is GroupElement)
            {
                m_LastTime = DateTime.Now.Ticks;
                if (m_AnimTarget == 0)
                {
                    m_AnimTarget = 1;
                }
                else if (m_Anim == 1f)
                {
                    m_Anim = 0f;
                    m_Stack.Add(e as GroupElement);
                }
            }
            else
            {
                if (addIfComponent)
                {
                    if (m_callback != null)
                        m_callback(e);

                    Close();
                }
            }
        }

        private void ListGUI(Element[] tree, float anim, GroupElement parent, GroupElement grandParent)
        {
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0f, 1f, Mathf.Repeat(anim, 1f));

            Rect position = base.position;
            position.x = base.position.width * (1f - anim) + 1f;
            position.y = 30f;
            position.height -= 30f;
            position.width -= 2f;
            GUILayout.BeginArea(position);

            Rect rect = GUILayoutUtility.GetRect(10f, 25f);
            GUI.Label(rect, parent.name, s_Styles.header);

            if (grandParent != null)
            {
                Rect leftArrowPosition = new Rect(rect.x + 4f, rect.y + 7f, 13f, 13f);
                if (Event.current.type == EventType.Repaint)
                {
                    s_Styles.leftArrow.Draw(leftArrowPosition, false, false, false, false);
                }
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    GoToParent();
                    Event.current.Use();
                }
            }

            if (parent is ICustomGUI)
            {
                var customGUIElement = parent as ICustomGUI;
                customGUIElement.OnGUI(this);
            }
            else
            {
                ListGUI(tree, parent);
            }
            GUILayout.EndArea();
        }

        private void ListGUI(Element[] tree, GroupElement parent)
        {
            parent.scroll = GUILayout.BeginScrollView(parent.scroll, new GUILayoutOption[0]);

            EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));

            var children = GetChildren(tree, parent);
            Rect rect = default(Rect);
            for (int i = 0; i < children.Count; i++)
            {
                Element element = children[i];
                Rect elementRect = GUILayoutUtility.GetRect(16f, 20f, GUILayout.ExpandWidth(true));
                if ((Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown) && parent.selectedIndex != i && elementRect.Contains(Event.current.mousePosition))
                {
                    parent.selectedIndex = i;
                    Repaint();
                }

                bool selected = false;
                if (i == parent.selectedIndex)
                {
                    selected = true;
                    rect = elementRect;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    s_Styles.groupButton.Draw(elementRect, element.content, false, false, selected, selected);
                    if (element is GroupElement)
                    {
                        Rect rightArrowStyle = new Rect(elementRect.x + elementRect.width - 13f, elementRect.y + 4f, 13f, 13f);
                        s_Styles.rightArrow.Draw(rightArrowStyle, false, false, false, false);
                    }
                }

                if (Event.current.type == EventType.MouseDown && elementRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    parent.selectedIndex = i;
                    GoToChild(element, true);
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);
            GUILayout.EndScrollView();
            if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (rect.yMax - lastRect.height > parent.scroll.y)
                {
                    parent.scroll.y = rect.yMax - lastRect.height;
                    Repaint();
                }
                if (rect.y < parent.scroll.y)
                {
                    parent.scroll.y = rect.y;
                    Repaint();
                }
            }

            if (activeElement != null)
                GUILayout.Label(activeElement.content.tooltip, s_Styles.miniLabel);
        }

        private List<Element> GetChildren(Element[] tree, Element parent)
        {
            List<Element> childens = new List<Element>();
            int childrensLevel = -1;
            int i;
            for (i = 0; i < tree.Length; i++)
            {
                if (tree[i] == parent)
                {
                    childrensLevel = parent.level + 1;
                    i++;
                    break;
                }
            }
            if (childrensLevel == -1)
                return childens;

            while (i < tree.Length)
            {
                Element element = tree[i];
                if (element.level < childrensLevel)
                    break;

                if (element.level == childrensLevel || hasSearch)
                {
                    childens.Add(element);
                }
                i++;
            }
            return childens;
        }
    }
}