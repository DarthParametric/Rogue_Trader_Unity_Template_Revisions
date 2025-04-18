 using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using OwlcatModification.Editor.Build;
using OwlcatModification.Editor.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OwlcatModification.Editor
{
	public static class ToolsMenu
	{
		[MenuItem("Assets/Modification Tools/Build", priority = 1)]
		private static void Build()
		{
			var modifications = AssetDatabase.FindAssets($"t:{nameof(Modification)}")
					.Select(AssetDatabase.GUIDToAssetPath)
					.Select(AssetDatabase.LoadAssetAtPath<Modification>)
					.ToArray();
			if (modifications.Length < 1)
			{
				EditorUtility.DisplayDialog("Error!", "No modifications found", "Close");
				return;
			}

			if (modifications.Length == 1)
			{
				Builder.BuildAndOpen(modifications[0]);
				return;
			}

			var window = EditorWindow.GetWindow<BuildModificationWindow>();
			window.Modifications = modifications;
			window.Show();
			window.Focus();
		}

		[MenuItem("Assets/Modification Tools/Blueprints' Types", priority = 2)]
		private static void ShowBlueprintTypesWindow()
		{
			BlueprintTypesWindow.ShowWindow();
		}

		[MenuItem("Assets/Modification Tools/Copy guid and file id", priority = 2)]
		private static void CopyAssetGuidAndFileID()
		{
			var obj = Selection.activeObject;
			if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long fileId))
			{
				GUIUtility.systemCopyBuffer = $"{{\"guid\": \"{guid}\", \"fileid\": {fileId}}}";
			}
			else
			{
				GUIUtility.systemCopyBuffer = $"Can't find guid and fileId for asset '{AssetDatabase.GetAssetPath(obj)}'";
			}
		}

		[MenuItem("Assets/Modification Tools/Copy file name", priority = 2)]
		private static void CopyFileNameWithoutExtension()
		{
			var obj = Selection.activeObject;
			string path = AssetDatabase.GetAssetPath(obj);
			string filename = Path.GetFileName(path).Split('.')[0];
			GUIUtility.systemCopyBuffer = filename;
			Debug.Log($"File name {filename} copied to clipboard");
		}

		[MenuItem("Assets/Modification Tools/Copy blueprint's guid", priority = 3)]
		private static void CopyBlueprintGuid()
		{
			var obj = Selection.activeObject;
			string path = AssetDatabase.GetAssetPath(obj);
			Regex regex;
			if (path.EndsWith(".jbp"))
			{
				regex = new Regex("\"AssetId\": \"([^\"]+)\"");
			}
			else if(path.EndsWith(".jbp_patch"))
			{
				regex = new Regex("\"TargetGuid\": \"([^\"]+)\"");
			}
			else
			{
				GUIUtility.systemCopyBuffer = "not blueprint id found";
				return;
			}
			
			using (var s = new StreamReader(path))
			{
				while (!s.EndOfStream)
				{
					string line = s.ReadLine();
					if (string.IsNullOrEmpty(line))
					{
						continue;
					}

					var m = regex.Match(line);
					if (m.Success)
					{
						GUIUtility.systemCopyBuffer = m.Groups[1].ToString();
						Debug.Log($"Found and copied guid from file: {m.Groups[1].ToString()}");
						return;
					}
				}
			}
			
			Debug.Log("No blueprint id found");
			GUIUtility.systemCopyBuffer = "no blueprint id found";
		}
		
		[MenuItem("Assets/Modification Tools/Copy blueprint guid", true)]
		private static bool IsCopyBlueprintGuidAllowed()
		{
			var obj = Selection.activeObject;
			string path = AssetDatabase.GetAssetPath(obj);
			return path != null && path.EndsWith(".jbp");
		}
		
		[MenuItem("Assets/Modification Tools/Create Blueprint", priority = 1)]
		private static void CreateBlueprint()
		{
			CreateBlueprintWindow.ShowWindow();
		}
	}
}