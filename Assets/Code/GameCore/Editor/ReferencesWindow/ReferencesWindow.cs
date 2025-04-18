using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Utility;
using Owlcat.Editor.Core.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.ReferencesWindow
{
    public partial class ReferencesWindow : EditorWindow
    {
        private Object m_Target;
        private List<Object> m_References = new();
        private readonly List<(Object target, Object result)> m_MultiReferences = new();
        private Vector2 m_Scroll;
	    private bool m_IncludeScenes;
        private bool m_FilterByType;
        private bool m_FilterByFolder;
		private bool m_FindInResults;
        private Type m_FilterType;
        private string m_FilterFolder;
        private string m_TargetString;
        private Action<Object> m_SelectCallback;
	    private bool m_QgrepMode = true;
	    private bool m_IncludeSubDirs = false;
	    private bool m_SeparateSearch = false;

	    void OnEnable()
	    {
		    CheckGrepSettings();
	    }

        [MenuItem("Assets/Find References In Project...", false, 25)]
        public static void FindReferencesInProject()
        {
            if (!Selection.activeObject || EditorUtility.IsPersistent(Selection.activeObject))
            {
                FindReferencesInProject(Selection.activeObject);
            }
        }

        public static void FindReferencesInProject(Object target)
        {
            GetWindow<ReferencesWindow>(true, "References to " + target).m_Target = target;//.FindReferences(target);
        }

        public static void ShowResults(IEnumerable<Object> references)
        {
	        GetWindow<ReferencesWindow>(true, "Search results").m_References.AddRange(references);
        }

        public static void FindReferencesInProject(SimpleBlueprint simpleBlueprint)
        {
            if (simpleBlueprint == null)
            {
                PFLog.Default.Warning($"{nameof(ReferencesWindow)}.{nameof(FindReferencesInProject)}: provided simple blueprint is null.");
                return;
            }

            GetWindow<ReferencesWindow>(true, "References to " + simpleBlueprint).m_Target = BlueprintEditorWrapper.Wrap(simpleBlueprint);
        }

        private void FindReferences(Object asset)
        {
	        m_Target = asset;

            string guid = asset is BlueprintEditorWrapper 
	            ?  ((BlueprintEditorWrapper)m_Target).Blueprint.AssetGuid 
	            : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

            FindReferences(guid, asset.name);
        }

        private void FindReferences(string id, string assetname)
        {
            EditorUtility.DisplayProgressBar("Looking for references to " + assetname, "Building file list", 0);

            string rootAssets = m_FilterByFolder && m_FilterFolder.Contains("/Assets/")
                ? m_FilterFolder[(m_FilterFolder.IndexOf("/Assets/", StringComparison.Ordinal) + 1)..]
                : "Assets/";

            string[] allFiles = GetAllFiles(rootAssets)?.ToArray();

			m_References.Clear();
			for (int ii = 0; ii < allFiles.Length; ii++)
            {
	            string assetPath = allFiles[ii];
	            string fileName = Path.GetFileName(assetPath);
                if (fileName is "LightingData.asset" or "BlueprintLibrary.asset")
                    continue;

                if (ii % 100 == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Looking for references to " + assetname,
                        "Looking in " + fileName,
                        (float)ii / allFiles.Length))
                    break;
                
                string assetText = File.ReadAllText(assetPath);
                if (!assetText.Contains(id))
                {
	                continue;
                }

                // Blueprint
                if (assetPath.EndsWith(".jbp"))
                {
	                assetPath = BlueprintsDatabase.FullToRelativePath(assetPath);
	                SimpleBlueprint simpleBlueprint = BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(assetPath);
	                var reference = BlueprintEditorWrapper.Wrap(simpleBlueprint);
	                Debug.Log($"[{assetPath}] references this blueprint", reference);
	                m_References.Add(reference);
                }
                // Asset
                else
                {
	                var reference = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
	                Debug.Log($"[{assetPath}] references this blueprint", reference);
	                m_References.Add(reference);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        private IEnumerable<string> GetAllFiles(string rootAssets)
        {
	        if (m_FindInResults && !m_References.Empty())
	        {
		        return m_References.Select(r => r is BlueprintEditorWrapper bw
				        ? BlueprintsDatabase.GetAssetPath(bw.Blueprint)
				        : AssetDatabase.GetAssetOrScenePath(r));
	        }

	        if (m_FilterByType && m_FilterType != null)
	        {
		        if (m_FilterType.IsOrSubclassOf<Object>())
		        {
			        return AssetDatabase.FindAssets("t:" + m_FilterType.Name,
					        m_FilterByFolder ? new[] {rootAssets} : null)
				        .Select(AssetDatabase.GUIDToAssetPath)
				        .Concat(BlueprintsDatabase.SearchByType(m_FilterType).Select(found => found.Path));
		        }

		        return BlueprintsDatabase.SearchByType(m_FilterType)
			        .Select(found => BlueprintsDatabase.RelativeToFullPath(found.Path));
	        }

	        if (m_FilterFolder.Contains("/Assets/"))
	        {
		        return Directory.GetFiles(rootAssets, "*.asset", SearchOption.AllDirectories)
			        .Concat(Directory.GetFiles(rootAssets, "*.prefab", SearchOption.AllDirectories))
			        .Concat(m_IncludeScenes
				        ? Directory.GetFiles(rootAssets, "*.unity", SearchOption.AllDirectories)
				        : Enumerable.Empty<string>());
	        }

	        return Directory.GetFiles("Blueprints/", "*.jbp", SearchOption.AllDirectories);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    m_Target = EditorGUILayout.ObjectField(m_Target, typeof(Object), true);
                    if (m_Target)
                    {
                        m_TargetString = m_Target is BlueprintEditorWrapper bw
                            ? bw.Blueprint.AssetGuid
                            : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_Target));
                    }
                    else
                    {
                        m_TargetString = EditorGUILayout.TextField(m_TargetString);
                    }
                }
                
				using (new EditorGUI.DisabledScope(m_QgrepThread?.IsRunning ?? false))
	            {
					if (m_Target & AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(m_Target)))
					{
						m_IncludeSubDirs =
							EditorGUILayout.ToggleLeft("+Subfolders", m_IncludeSubDirs, GUILayout.MaxWidth(90f));
						m_SeparateSearch =
							EditorGUILayout.ToggleLeft("Separate", m_SeparateSearch, GUILayout.MaxWidth(70f));
					}
					
					if (GUILayout.Button("Find"))
		            {
			            FindTarget();
		            }
	            }
            }
            
	        using (new EditorGUILayout.HorizontalScope())
	        {
		        bool sm = GUILayout.Toggle(m_QgrepMode == false, "Slow mode", EditorStyles.miniButtonLeft);
		        bool qm = GUILayout.Toggle(m_QgrepMode == true, "QGrep mode", EditorStyles.miniButtonRight);

		        if (sm != (!m_QgrepMode) || (qm != m_QgrepMode))
		        {
			        m_QgrepMode = !m_QgrepMode;
		        }
	        }

	        if (m_QgrepMode)
	        {
		        DrawGrepGUI();
	        }
	        else
	        {
		        DrawSlowModeGUI();
	        }
	        
	        DisplayResults();

	        var totalRefsCount = m_SeparateSearch ? m_MultiReferences.Count : m_References.Count;
	        
	        if (totalRefsCount <= 0)
	        {
		        return;
	        }

	        using (new EditorGUILayout.HorizontalScope())
	        {
		        if (GUILayout.Button("Copy Results", EditorStyles.miniButtonLeft))
		        {
			        if (m_SeparateSearch)
			        {
				        //target
				        //	result1
				        //	result2
				        StringBuilder result = new StringBuilder();
				        var query = m_FoundMultiPaths
					        .GroupBy(o => o.targetGuid, o => o.foundPath);
				        foreach (var group in query)
				        {
					        result.AppendLine(AssetDatabase.GUIDToAssetPath(group.Key));
					        foreach (string found in group)
					        {
						        result.AppendLine($"\t{found}");
					        }
					        // result.AppendLine();
				        }

				        EditorGUIUtility.systemCopyBuffer = result.ToString();
			        }
			        else
			        {
				        EditorGUIUtility.systemCopyBuffer =
					        string.Join("\n", m_References.NotNull().Select(obj => obj.name));
			        }
		        }

		        if (m_SeparateSearch && GUILayout.Button("Copy Results v.2", EditorStyles.miniButtonRight))
		        {
			        //target	result1
			        //	result2
			        StringBuilder result = new StringBuilder();
			        var query = m_FoundMultiPaths
				        .GroupBy(o => o.targetGuid, o => o.foundPath);

			        foreach (var group in query)
			        {
				        result.Append(AssetDatabase.GUIDToAssetPath(group.Key));
				        foreach (string found in group)
				        {
					        result.AppendLine($"\t{found}");
				        }
			        }

			        EditorGUIUtility.systemCopyBuffer = result.ToString();
		        }
	        }

	        // if (GUILayout.Button("Copy As CSV", EditorStyles.miniButton))
			// {
			// 	if (m_SeparateSearch)
			// 	{
			// 		EditorGUIUtility.systemCopyBuffer =
			// 			string.Join("\n", m_MultiReferences.NotNull().Select(obj => $"{obj.target?.name ?? "null"},{obj.result?.name ?? "null"}"));
			// 	}
			// 	else
			// 	{
			// 		EditorGUIUtility.systemCopyBuffer =
			// 			string.Join("\n", m_References.NotNull().Select(obj => $"{m_Target.name},{obj.name}"));
			// 	}
			// }

			if (GUILayout.Button("Copy Paths As CSV", EditorStyles.miniButton))
			{
				if (m_SeparateSearch)
				{
					EditorGUIUtility.systemCopyBuffer = string.Join("\n",
						m_FoundMultiPaths
							.NotNull()
							.Select(obj => $"{AssetDatabase.GUIDToAssetPath(obj.targetGuid)},{obj.foundPath}"));
				}
				else
				{
					EditorGUIUtility.systemCopyBuffer =
						string.Join("\n", m_References.NotNull().Select(obj => $"{AssetDatabase.GetAssetPath(m_Target)},{AssetDatabase.GetAssetPath(obj)}"));
				}
			}

			if (!GUILayout.Button("Resave", EditorStyles.miniButton))
	        {
		        return;
	        }

	        try
	        {
		        for (int i = 0; i < m_References.Count; i++)
		        {
			        if (!EditorUtility.IsPersistent(m_References[i]))
			        {
				        continue;
			        }

			        if (EditorUtility.DisplayCancelableProgressBar(
				            "Resaving objects",
				            m_References[i].name,
				            i / (float)m_References.Count))
			        {
				        return;
			        }

			        Object obj = m_References[i];
			        var prototypeableObject = obj as PrototypeableObjectBase;
			        
			        if (prototypeableObject)
			        {
#if UNITY_EDITOR && EDITOR_FIELDS
				        prototypeableObject.CopyOverridesFromProto();
#endif
			        }
			        try
			        {
				        EditorUtility.SetDirty(obj);
			        }
			        catch (Exception e)
			        {
				        Debug.LogErrorFormat("Set dirty try at obj {0} failure, coz {1}", obj.name, e);
			        }
		        }
		        
		        if (EditorUtility.DisplayCancelableProgressBar("Resaving objects", "Saving assets", 1f))
		        {
			        return;
		        }
		        
		        AssetDatabase.SaveAssets();
		        ResaveUtilities.RevertLocalRotations();
	        }
	        finally
	        {
		        EditorUtility.ClearProgressBar();
	        }
        }

        private void DisplayResults()
        {
	        using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll))
	        {
		        m_Scroll = scope.scrollPosition;
		        if (!m_SeparateSearch)
		        {
					List<Object> nextReferences = new();
			        foreach (var reference in m_References.EmptyIfNull())
			        {
				        if (!reference)
				        {
					        GUILayout.Label("null?");
					        continue;
				        }

				        bool selected = Selection.Contains(reference);
				        var bg = GUI.backgroundColor;
				        GUI.backgroundColor = selected ? GUI.skin.settings.selectionColor : bg;
				        using (new EditorGUILayout.HorizontalScope())
				        {
					        DrawReferenceButton(reference, selected);

					        GUI.backgroundColor = bg;
					        var scene = reference as SceneAsset;
					        if (scene)
					        {
						        DrawAdditionalButtons(scene);
					        }

					        if (GUILayout.Button("Find", GUILayout.ExpandWidth(false)))
					        {
						        m_FilterByFolder = false;
						        m_FilterFolder = "";
						        //set reference as new search object
						        nextReferences.Add(reference);
					        }
				        }
			        }

			        foreach (var reference in nextReferences)
			        {
				        if (reference == null)
				        {
					        continue;
				        }

				        FindReferencesInProject(reference);
				        //mimic main "Find" button
				        FindTarget();
			        }
		        }
		        else
		        {
					//multi-target search requires multi-target list
					//TODO: get rid of copy-pasta
					List<Object> nextReferences = new();
					foreach ((Object target, Object found) referenceTuple in m_MultiReferences.EmptyIfNull())
					{
						if (referenceTuple.target == null)
						{
							GUILayout.Label("null target?");
							continue;
						}
						
						if(referenceTuple.found == null)
						{
							continue;
						}

						bool selected = Selection.Contains(referenceTuple.found);
						var bg = GUI.backgroundColor;
						GUI.backgroundColor = selected ? GUI.skin.settings.selectionColor : bg;
						using (new EditorGUILayout.HorizontalScope())
						{
							DrawMultiReferenceButton(referenceTuple, selected);

							GUI.backgroundColor = bg;
							var scene = referenceTuple.found as SceneAsset;
							if (scene)
							{
								DrawAdditionalButtons(scene);
							}

							if (GUILayout.Button("Find", GUILayout.ExpandWidth(false)))
							{
								m_FilterByFolder = false;
								m_FilterFolder = "";
								//set reference as new search object
								nextReferences.Add(referenceTuple.found);
							}
						}
					}

					foreach (var reference in nextReferences)
					{
						if (reference == null)
						{
							continue;
						}

						FindReferencesInProject(reference);
						//mimic main "Find" button
						FindTarget();
					}
				}
	        }
        }

        private void DrawAdditionalButtons(SceneAsset scene)
        {
	        string path = AssetDatabase.GetAssetPath(scene);
	        var s = SceneManager.GetSceneByPath(path);
	        if (!s.isLoaded
	            && GUILayout.Button("Open", GUILayout.ExpandWidth(false)))
	        {

		        EditorSceneManager.OpenScene(path,
			        Event.current.control ? OpenSceneMode.Additive : OpenSceneMode.Single);
		        return;
	        }

	        if (GUILayout.Button("Find in scene", GUILayout.ExpandWidth(false)))
	        {
		        FindReferencesInScene(m_Target);
	        }
        }

        private void DrawReferenceButton(Object reference, bool selected)
        {
	        if (!GUILayout.Button(reference.name, GUI.skin.box, GUILayout.ExpandWidth(true)))
	        {
		        return;
	        }

	        if (selected)
	        {
		        Selection.objects = Selection.objects.Where(o => o != reference).ToArray();
	        }
	        else
	        {
		        Selection.activeObject = reference;
	        }

	        m_SelectCallback?.Invoke(reference);
        }

		private void DrawMultiReferenceButton((Object target, Object found) reference, bool selected)
		{
			using (var h = new EditorGUILayout.HorizontalScope("Button"))
			{
				// using (new EditorGUI.DisabledScope())
				{
					EditorGUILayout.ObjectField(reference.target, typeof(Object), true, GUILayout.MaxWidth(200f));
				}

				if (!GUILayout.Button(reference.found.name, GUI.skin.box, GUILayout.ExpandWidth(true)))
				{
					return;
				}

				if (selected)
				{
					Selection.objects = Selection.objects.Where(o => o != reference.found).ToArray();
				}
				else
				{
					Selection.activeObject = reference.found;
				}

				m_SelectCallback?.Invoke(reference.found);
			}
		}
		
		private void DrawMultiObjectFoldout(Object target, Object[] references, Object selectedObj)
		{
			//TODO: Implement
			throw new NotImplementedException("IMPLEMENT BEAUTIFUL UI FOR MULTIREFERENCE!");
			//need a structure to hold dynamic list of foldout bools?
			// var showFound = EditorGUILayout.BeginFoldoutHeaderGroup()
		}

		private void DrawSlowModeGUI()
        {
	        using (GuiScopes.FixedWidth(1, 1))
	        {
		        using (new EditorGUILayout.HorizontalScope())
		        {
			        GUILayout.Label("By Type", GUILayout.Width(60));
			        m_FilterByType = EditorGUILayout.Toggle(m_FilterByType, GUILayout.Width(20));
			        if (m_FilterByType)
			        {
				        TypePicker.Button(
					        m_FilterType == null ? "..." : m_FilterType.Name,
					        () => TypeCache.GetTypesDerivedFrom(typeof(BlueprintScriptableObject)).Where(t => !t.IsAbstract),
					        t => m_FilterType = t);
			        }
		        }

		        using (new EditorGUILayout.HorizontalScope())
		        {
			        GUILayout.Label("By Folder", GUILayout.Width(60));
			        m_FilterByFolder = EditorGUILayout.Toggle(m_FilterByFolder, GUILayout.Width(20));
			        
			        if (m_FilterByFolder &&
			            GUILayout.Button(string.IsNullOrEmpty(m_FilterFolder) ? "..." : m_FilterFolder))
			        {
				        m_FilterFolder =
					        EditorUtility.OpenFolderPanel("Select folder", m_FilterFolder ?? "", "Mechanics");
			        }
		        }

		        using (new EditorGUILayout.HorizontalScope())
		        {
			        GUILayout.Label("Include Scenes", GUILayout.Width(60));
			        m_IncludeScenes = EditorGUILayout.Toggle(m_IncludeScenes, GUILayout.Width(20));
		        }

		        if (m_References.Empty())
		        {
			        return;
		        }

		        using (new EditorGUILayout.HorizontalScope())
		        {
			        GUILayout.Label("In Results", GUILayout.Width(60));
			        m_FindInResults = EditorGUILayout.Toggle(m_FindInResults, GUILayout.Width(20));
		        }
	        }
        }

        private void FindTarget()
        {
	        if (m_QgrepMode)
	        {
		        FindReferencesQgrep();
		        return;
	        }

	        if (m_Target)
	        {
		        FindReferences(m_Target);
		        return;
	        }
	        
	        FindReferences(m_TargetString, m_TargetString);
        }

        private static void FindReferencesInScene(Object target)
        {
            Selection.activeObject = target;
            EditorApplication.ExecuteMenuItem("Assets/Find References In Scene");
        }

        public void SetReferences(IEnumerable<Object> references, Action<Object> onSelectCallback = null)
        {
            m_References = references.ToList();
            m_SelectCallback = onSelectCallback;
        }
        
        public void SetReferences(IEnumerable<BlueprintScriptableObject> references, Action<Object> onSelectCallback = null)
        {
            m_References = references.Select(_blueprint => BlueprintEditorWrapper.Wrap(_blueprint)).Cast<Object>().ToList();
            m_SelectCallback = onSelectCallback;
        }
    }
}