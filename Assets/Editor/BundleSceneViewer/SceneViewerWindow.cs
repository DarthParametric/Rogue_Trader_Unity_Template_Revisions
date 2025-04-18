using Editor.BundleSceneViewerStarter;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Editor.Scene
{
    public class SceneViewGui : EditorWindow
    {
        [MenuItem("Modification Tools/SceneViewer Window")]
        public static void ShowWindow()
        {
            SceneViewGui window = GetWindow<SceneViewGui>(typeof(EditorGUI).Assembly.GetType("UnityEditor.ProjectBrowser"));
            window.Show();
        }

        private void OnDoubleClickSceneHandler(string sceneName, bool additive)
        {
            if (!_builder.LoadScene(sceneName, additive))
            {
                Debug.LogWarning($"Failed to begin scene {sceneName}");
            }
        }

        public void OnEnable()
        {
            BundleSceneServerStarter.EnsureBundleSceneServerStarted();

            _treeViewState = new();

            _treeView = new(_treeViewState);
            _treeView.OnDoubleClickScene += OnDoubleClickSceneHandler;

            _searchBar = new();
            _searchBar.OnUpdateSearch += pattern => _treeView.searchString = pattern;
            _searchBar.OnClearSearch += () => _treeView.searchString = string.Empty;

            _database = new();
            _database.OnReceivedSceneList += () => EditorApplication.delayCall += () => _treeView.SetSceneList(_database.Scenes);
            _database.Start();

            _builder = new(_database);
        }

        public void OnDisable()
        {
            _builder.Dispose();
            _database.Dispose();
        }

        public void OnGUI()
        {
            _searchBar.OnGUI();
            _treeView.OnGUI(GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)));
        }

        public void Update()
        {
            _database.Update();
            _builder.Update();
        }

        private TreeViewState _treeViewState;
        private SceneViewerTreeView _treeView;
        private SceneViewerSearchBar _searchBar;
        private AssetDatabaseConnection _database;
        private SceneBuilder _builder;
    }
}
