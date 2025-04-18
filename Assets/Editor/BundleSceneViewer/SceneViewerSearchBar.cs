using UnityEditor.IMGUI.Controls;

namespace Editor.Scene
{
    public class SceneViewerSearchBar : SearchField
    {
        public delegate void OnUpdateSearchDelegate(string pattern);
        public delegate void OnClearSearchDelegate();

        public event OnUpdateSearchDelegate OnUpdateSearch;
        public event OnClearSearchDelegate OnClearSearch;

        public void OnGUI()
        {
            string pattern = base.OnGUI(_searchPattern);

            if (pattern != _searchPattern)
            {
                if (string.IsNullOrEmpty(pattern))
                {
                    OnClearSearch?.Invoke();
                }
                else
                {
                    OnUpdateSearch?.Invoke(pattern);
                }

                _searchPattern = pattern;
            }
        }

        private string _searchPattern = string.Empty;
    }
}
