using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

namespace Kingmaker.Editor.Utility
{
    public class ProgressWrapper<T>:IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly ProgressWrapper<T> m_Collection;
            private readonly IEnumerator<T> m_EnumeratorImplementation;
            private readonly Stopwatch m_Stopwatch;
            
            private int m_Cur;

            public Enumerator(ProgressWrapper<T> collection)
            {
                m_Collection = collection;
                m_EnumeratorImplementation = collection.m_Collection.GetEnumerator();
                m_Stopwatch = Stopwatch.StartNew();

                m_Cur = 0;
                collection.Cancelled = false;
            }

            public void Dispose()
            {
                EditorUtility.ClearProgressBar();
            }

            public bool MoveNext()
            {
                if (m_Collection.Cancelled || !m_EnumeratorImplementation.MoveNext())
                    return false;
                    
                m_Cur++;
                if (m_Stopwatch.ElapsedMilliseconds > 100)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(m_Collection.m_Title, m_Collection.Info ?? m_EnumeratorImplementation.Current?.ToString() ?? "",
                        (float)m_Cur / m_Collection.m_Count))
                    {
                        m_Collection.Cancelled = true;
                        return false;
                    }
                    m_Stopwatch.Restart();
                }

                return true;
            }

            public void Reset()
            {
                m_EnumeratorImplementation.Reset();
            }

            public T Current
                => m_EnumeratorImplementation.Current;

            object IEnumerator.Current
                => ((IEnumerator)m_EnumeratorImplementation).Current;
        }
            
        private readonly IEnumerable<T> m_Collection;
        private readonly int m_Count;

        private readonly string m_Title;
        public string Info { get; set; }
        
        public bool Cancelled { get; private set; }

        public ProgressWrapper(ICollection<T> collection, string title = "", string info = null)
        {
            m_Collection = collection;
            m_Count = collection.Count;
            m_Title = title;
            Info = info;
        }

        public ProgressWrapper(IEnumerable<T> collection, int count, string title = "", string info = null)
        {
            m_Collection = collection;
            m_Count = count;
            m_Title = title;
            Info = info;
        }

        public Enumerator GetEnumerator()
            => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}