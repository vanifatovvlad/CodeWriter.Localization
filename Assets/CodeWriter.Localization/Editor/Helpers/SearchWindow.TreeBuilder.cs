using System;
using System.Collections.Generic;
using UnityEditor;

namespace CodeWriter.Localization.Helpers
{
    internal partial class SearchWindow : EditorWindow
    {
        public sealed class TreeBuilder
        {
            private int m_level = 0;
            private List<string> m_levels;
            private List<Element> m_elements;
            private readonly char? m_splitter;
            private readonly string m_root;

            public TreeBuilder(char? splitter, string root)
            {
                m_splitter = splitter;
                m_root = root;

                Clear();
            }

            public void Clear()
            {
                m_level = 0;
                m_levels = new List<string>();
                m_elements = new List<Element>();
                m_elements.Add(new GroupElement(m_root));
            }

            public void BeginGroup(string path)
            {
                m_level++;
                var groupElement = new GroupElement(path);
                groupElement.level = m_level;
                m_elements.Add(groupElement);
            }

            public IDisposable BeginGroupScope(string path)
            {
                return new GroupScope(this, path);
            }

            public void EndGroup()
            {
                m_level--;
            }

            public void Item(string path, Element element)
            {
                if (m_splitter.HasValue)
                {
                    var parts = path.Split(m_splitter.Value);

                    while (m_levels.Count > parts.Length)
                        m_levels.RemoveAt(m_levels.Count - 1);

                    while (m_levels.Count > 0 && parts[m_levels.Count - 1] != m_levels[m_levels.Count - 1])
                        m_levels.RemoveAt(m_levels.Count - 1);

                    while (m_levels.Count < parts.Length - 1)
                    {
                        var groupElement = new GroupElement(parts[m_levels.Count]);
                        groupElement.level = m_level + m_levels.Count + 1;
                        m_elements.Add(groupElement);
                        m_levels.Add(parts[m_levels.Count]);
                    }
                }

                element.level = m_level + m_levels.Count + 1;
                m_elements.Add(element);
            }

            public Element[] Build()
            {
                if (m_level != 0)
                    throw new InvalidOperationException("Each group opened by BeginGroup() should be closed by EndGroup()");

                return m_elements.ToArray();
            }

            private class GroupScope : IDisposable
            {
                private readonly TreeBuilder m_builder;

                public GroupScope(TreeBuilder builder, string path)
                {
                    m_builder = builder;
                    m_builder.BeginGroup(path);
                }

                public void Dispose()
                {
                    m_builder.EndGroup();
                }
            }
        }
    }
}