#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Editor;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.Creation;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Kingmaker.Utility.UnityExtensions;

namespace Kingmaker.Editor.Blueprints
{
	public class NewAssetWindow : EditorWindow
	{
		[SerializeField]
		private AssetCreatorBase m_SelectedCreator;

		[SerializeField]
		private SerializedObject m_SerializedCreator;


		private static readonly Vector2 s_Size = new Vector2(600, 200);

		[NotNull]
		private static string s_Folder = "";

		[NotNull]
		private static string s_Name = "";

		private static bool s_CreateAnother = false;

		private static Action<object> s_CreationCallback;

		private bool m_FirstFrame = false;

		public NewAssetWindow()
		{
			titleContent = new GUIContent("New Blueprint");
			minSize = s_Size;
			maxSize = s_Size;
		}

		public static string AssetName
		{
			get { return s_Name; }
			set { s_Name = value; }
		}


		[MenuItem("Design/Create blueprint #=", false, 10000)]
		public static void ShowAssetWindowNew()
		{
			ShowWindow(null);
		}

		[MenuItem("Design/Create Shared String %#=", false, 10001)]
		public static void ShowSharedStringWindow()
		{
			ShowWindow(CreateInstance<BarkStringCreator>());
		}

        public static object CreateWithCreator(AssetCreatorBase creator, string name, string folder = null)
        {
	        s_Folder = folder ?? "";
	        string path = TemplateSubstitution(creator.LocationTemplate, creator, name, folder);

            if(!path.StartsWith("Assets") && !path.StartsWith("Blueprints"))
            {
                PFLog.Default.Error($"{creator.GetType().Name}: cannot create in {path}");
				return null;
            }

            return SaveAsset(path, creator);
		}

		public static void ShowWindow(AssetCreatorBase creator)
		{
			s_CreateAnother = false;
			s_Name = "";
			
			var window = GetWindow<NewAssetWindow>();

			window.m_FirstFrame = !creator;
			if (creator)
			{
				window.m_SelectedCreator = creator;
			}
			window.titleContent = new GUIContent(creator ? creator.CreatorName : "NICOLAY");
			window.Show();
		}

		public static void ShowWindow(AssetCreatorBase creator, string defaultName, Action<object> onCreated)
		{
			s_CreateAnother = false;

			var window = GetWindow<NewAssetWindow>();
			
			window.m_FirstFrame = false;
			if (creator)
			{
				window.m_SelectedCreator = creator;
			}
			s_Name = defaultName;
			s_CreationCallback = onCreated;
			window.titleContent = new GUIContent(creator.CreatorName);
			window.Show();
		}

		private List<string> GetFolders()
		{
			var template = GetTemplate();
			var i = template.IndexOf("{folder}", StringComparison.Ordinal);
			if (i < 0)
			{
				return new List<string>();
			}

			string directory = template.Substring(0, i);

			if (directory == "")
			{
				return new List<string>();
			}

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

			var result = Directory.GetDirectories(directory)
				.Select(Path.GetFileName)
				.ToList();

			result.Insert(0, "");

			return result;
		}

		public void OnGUI()
		{
			TypeSelector();

			if (m_SelectedCreator)
			{
				// show creator props
				FolderField();
				NameField();
				CreatorGUI();

				var path = GetAssetPath();
				GUILayout.Label(path, EditorStyles.wordWrappedLabel);
				CreateButton(path);
			}

			var e = Event.current;
			if (e.type == EventType.KeyDown)
			{
				if (e.keyCode == KeyCode.Escape)
				{
					Clear(true);
					e.Use();
				}
			}
		}

		private void CreatorGUI()
		{
			m_SerializedCreator = (m_SerializedCreator?.targetObject == m_SelectedCreator)
				? m_SerializedCreator
				: new SerializedObject(m_SelectedCreator);

			m_SerializedCreator.Update();
			SerializedProperty iterator = m_SerializedCreator.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (iterator.name!="m_Script" && !m_SelectedCreator.ShouldSkipProperty(iterator.name))
					EditorGUILayout.PropertyField(iterator, true);
			}
			m_SerializedCreator.ApplyModifiedProperties();
			m_SelectedCreator.OnGUI();
		}

		private void TypeSelector()
		{
			using (GuiScopes.Horizontal())
			{
				bool showPicker = m_FirstFrame && Event.current.type == EventType.Repaint;
				CreatorPicker.Button(
					"Type",
					t =>
					{
						m_SelectedCreator = t;
						Repaint();
					},
					showPicker,
					GUILayout.ExpandWidth(false)
				);
				if (showPicker)
					m_FirstFrame = false;

				if (m_SelectedCreator != null)
				{
					GUILayout.Label(m_SelectedCreator.CreatorName);
				}
			}
		}

		private string GetTemplate()
        {
            var creatorTemplate = m_SelectedCreator?.LocationTemplate;
			// hack: a bunch of creators default to assets path, rather than fixing all of them, we can simply fix the returned path here
			if (m_SelectedCreator.CreatesBlueprints)
            {
                if (creatorTemplate.StartsWith("Assets/Mechanics/"))
                {
                    creatorTemplate = creatorTemplate.Substring("Assets/Mechanics/".Length);
                }
                creatorTemplate = creatorTemplate.Replace(".asset", ".jbp");
            }
			
			return creatorTemplate;

        }

		private bool TemplateContains(string param)
		{
			return GetTemplate().Contains("{" + param + "}");
		}

		private void FolderField()
		{
			if (!TemplateContains("folder"))
			{
				return;
			}

			var folders = GetFolders();

			var rect = EditorGUILayout.GetControlRect();

			var labelRect = rect;
			labelRect.width = EditorGUIUtility.labelWidth;
			GUI.Label(labelRect, "Folder");

			var buttonRect = rect;
			buttonRect.xMin += EditorGUIUtility.labelWidth;

			StringPicker.Button(
				buttonRect, s_Folder,
				() => folders,
				f => s_Folder = f,
				style: EditorStyles.popup
			);
		}

		private void NameField()
		{
			GUI.SetNextControlName("NameField");
			s_Name = EditorGUILayout.TextField("Name", s_Name);
		}

		static string TemplateSubstitution(string template, AssetCreatorBase creator, string name, string folder)
		{
			if (!string.IsNullOrEmpty(name))
			{
				template = template.Replace("{name}", name);
			}
			if(!string.IsNullOrEmpty(folder))
			{
				template = template.Replace("{folder}", folder);
			}

			/*string cacheArea = creator.CurrentOpenArea;  //tmp HOTFIX: WH-25091
			
			if (creator.NeedsAreaReference && cacheArea != "")
			{
				template = template.Replace("{Area}", cacheArea);
				if (creator)
				{
					var so = new SerializedObject(creator);
					var field = so.FindProperty("Area");
					var guid = field?.FindPropertyRelative("guid");
					var bp = guid != null ? BlueprintsDatabase.LoadById<SimpleBlueprint>(guid.stringValue) : null;
					if (bp != null && bp.name != "")
					{
						template = template.Replace(cacheArea, "{Area}");
					}

					return creator.ReplaceTemplates(template, so);
				}
			}*/

			if (creator)
			{
				template = creator.ReplaceTemplates(template, new SerializedObject(creator));
			}

			return template;
		}

		private string GetAssetPath(string template = "")
		{
			var path = template == "" ? GetTemplate() : template;

			var folder = s_Folder;
			if (folder.IsNullOrEmpty())
			{
				if (path.Contains("{folder}"))
				{
					// TODO: units can select folder from prototype
					//var backupFolderPath = "";
					//if (s_PrototypeHolder.Blueprint != null)
					//{
					//	backupFolderPath = AssetDatabase.GetAssetPath(s_PrototypeHolder.Blueprint);
					//}

					//if (backupFolderPath != "")
					//{
					//	folder = Path.GetDirectoryName(backupFolderPath);
					//	folder = Path.GetFileName(folder);
					//	path = path.Replace("{folder}", folder);
					//}
				}
			}

			path = TemplateSubstitution(path, m_SelectedCreator, s_Name, folder);

			path = path.Replace("//", "/");
			return path;
		}

		private void CreateButton(string path)
		{
			using (GuiScopes.Color(Color.red))
			{
				if (path.Contains("{") || path.Contains("}"))
				{
					GUILayout.Label("not all data filled", EditorStyles.whiteLabel);
					return;
				}
				
				if (AssetDatabase.LoadMainAssetAtPath(path) != null)
				{
					GUILayout.Label("asset already exists", EditorStyles.whiteLabel);
					return;
				}
			}

			var e = Event.current;
			bool hotkey = e.type == EventType.KeyDown
						&& (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
						&& e.control;

			using (GuiScopes.Horizontal())
			{
				if (GUILayout.Button("Create") || hotkey)
				{
					CreateAndSaveAsset(path);
					Clear(!s_CreateAnother);
				}
				s_CreateAnother = GUILayout.Toggle(s_CreateAnother, "Create Another", GUILayout.ExpandWidth(false));
			}
		}
		private void CreateAndSaveAsset(string path)
		{
			if (!m_SelectedCreator)
				return;
			
			if (m_SelectedCreator.NeedFolderSelection)
				m_SelectedCreator.Folder = s_Folder; // todo: backup folder?
			
			string dir = Path.GetDirectoryName(path);
			if (dir != null)
				Directory.CreateDirectory(dir);

			object created = m_SelectedCreator.CreateAsset();

			if (created is SimpleBlueprint bp)
			{
				bp.name = s_Name;
				Save(bp, path);
				(bp as BlueprintScriptableObject)?.TryToInitAssetGuid();

				m_SelectedCreator.PostProcess(bp);
				Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
				BlueprintProjectView.Ping(bp);

				s_CreationCallback?.Invoke(bp);
				return;
			}

			if (created is Object obj)
			{
				obj.name = s_Name;

				if (m_SelectedCreator is SceneCreator sceneCreator)
				{
					Directory.Move( sceneCreator.DefaultPath, path);
				}
				else
				{
					Save(obj, path);
				}

				m_SelectedCreator.PostProcess(obj);
				Selection.activeObject = obj;

				s_CreationCallback?.Invoke(obj);
			}
		}

		private static object SaveAsset(string path, AssetCreatorBase creator)
		{
			string dir = Path.GetDirectoryName(path);
			if (dir != null)
				Directory.CreateDirectory(dir);
			
			object created = creator.CreateAsset();

			if (created is SimpleBlueprint bp)
			{
				bp.name = s_Name;
				Save(bp, path);
				(bp as BlueprintScriptableObject)?.TryToInitAssetGuid();

				creator.PostProcess(bp);
				Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
				BlueprintProjectView.Ping(bp);

				created = bp;
				{
					return created;
				}
			}

			if (created is Object obj)
			{
				obj.name = s_Name;

				if (creator is SceneCreator sceneCreator)
				{
					Directory.Move( sceneCreator.DefaultPath, path);
				}
				else
				{
					Save(obj, path);
				}

				creator.PostProcess(obj);
				Selection.activeObject = obj;

				
				created = obj;
			}

			return created;
		}

		private void Clear(bool close)
		{
			s_Name = "";
			s_CreationCallback = null;
			if (close)
			{
				m_SelectedCreator = null;
				Close();
			}
			else
			{
				m_SelectedCreator = (AssetCreatorBase) CreateInstance(m_SelectedCreator.GetType()); // this resets creator fields
				GUI.FocusControl("NameField");
			}
		}

		public static void SetCreationCallback(Action<object> callback)
		{
			s_CreationCallback = callback;
		}
        
        private static void Save(object obj, string path)
        {
            if (obj is SimpleBlueprint bp)
            {
                BlueprintsDatabase.CreateAsset(bp, path);
            }
            if (obj is Object asset)
            {
	            AssetDatabase.CreateAsset(asset, path);
            }
        }
	}
}
#endif