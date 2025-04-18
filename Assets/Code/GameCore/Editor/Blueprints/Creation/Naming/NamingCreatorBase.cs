using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.Creation.Naming;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
	public abstract class NamingCreatorBase : AssetCreatorBase
	{
		protected const string NameToken = "{name}";
		protected abstract string NameTokenNotEmpty { get; }

		private const string UnityBlueprintsFolder = "Assets/Mechanics/";

		protected abstract string DefaultFolder { get; }

		// Just a default value in case actual template is not defined
		protected abstract string DefaultTemplate { get; }

		// Template from naming config
		protected abstract string Template { get; }

		private string? m_Template;

		private readonly Regex m_Token = new("[{]([^{}]+)[}]");
		private MatchCollection? m_TemplateMatches;

		private bool m_IsFirstTime = true;

		private readonly Dictionary<string, string[]> m_Namings = new();
		private readonly Dictionary<string, string> m_Result = new();
		private readonly Dictionary<string, Type> m_Types = new();

		protected bool IsFolderOverridden;
		private bool m_IsNameEmpty;

		public override string LocationTemplate // No more needed in new naming system
			=> string.Empty;

		private void CollectNamingTypesFromTemplate()
		{
			var namingType = typeof(NamingBase);
			var namingTypes = namingType.Assembly.GetTypes()
				.Where(t => t.IsSubclassOf(namingType) && !t.IsAbstract);

			foreach (var type in namingTypes)
			{
				string typeName = type.Name;
				if (m_TemplateMatches.Contains(match => match.Groups[1].Value == typeName))
				{
					m_Types.TryAdd(typeName, type);
				}
			}
		}

		private IEnumerable<string>? GetNamings(Type namingType)
		{
			IEnumerable<string>? namings = null;
			if (namingType == typeof(Location))
			{
				if (m_Result.TryGetValue(nameof(Chapter), out string chapter))
				{
					namings = LocationsByChapter.instance.GetLocationNames(chapter);
				}
			}
			else
			{
				namings = new List<string>(AssetDatabase
					.FindAssets($"t:{namingType.FullName}")
					.Select(AssetDatabase.GUIDToAssetPath)
					.Select(AssetDatabase.LoadAssetAtPath<NamingBase>)
					.Select(asset => asset.name));
			}
			return namings;
		}

		private void InitNamings()
		{
			m_Namings.Clear();
			foreach (var (typeName, namingType) in m_Types)
			{
				var namings = GetNamings(namingType);
				if (namings != null)
				{
					m_Namings.Add(typeName, namings.ToArray());
				}
			}
		}

		private void GenerateControls()
		{
			string[] orderedTypeNames = NamingControlsOrder.instance.NamingTypeNames;
			foreach (var typeName in orderedTypeNames)
			{
				if (!m_Namings.TryGetValue(typeName, out string[] namings) || namings == null)
				{
					continue;
				}

				StringPicker.Button(
					typeName,
					() => namings,
					pickedName =>
					{
						m_Result[typeName] = pickedName;
						if (typeName == nameof(Chapter))
						{
							// Force user to re-pick location
							m_Result.Remove(nameof(Location));
							UpdateLocations(pickedName);
						}
					});
			}
		}

		protected virtual string TemplateOverride()
		{
			return m_Template ?? DefaultTemplate;
		}

		public void Reset()
		{
			m_IsFirstTime = true;
		}

		private void ReloadTemplate()
		{
			m_Template = Template;
			m_Template = TemplateOverride();
		}

		private void InitFirstTime()
		{
			ReloadTemplate();
			if (m_Template != null)
			{
				m_TemplateMatches = m_Token.Matches(m_Template);
			}
			CollectNamingTypesFromTemplate();
			InitNamings();
			m_Result.Clear();
		}

		private string TryGetCurrentActiveObjectFolder()
		{
			if (CreatesBlueprints && Selection.activeObject is BlueprintEditorWrapper bw)
			{
				// We are creating blueprint and active object is blueprint also
				string blueprintPath = BlueprintsDatabase.GetAssetPath(bw.Blueprint);
				return Path.GetDirectoryName(blueprintPath)?.Replace("\\", "/") + "/";
			}

			string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (!CreatesBlueprints && !string.IsNullOrEmpty(assetPath))
			{
				// We are creating generic asset and active object is generic asset also
				if (AssetDatabase.IsValidFolder(assetPath))
				{
					return assetPath + "/";
				}
				return Path.GetDirectoryName(assetPath)?.Replace("\\", "/") + "/";
			}

			return DefaultFolder;
		}

		private void SetTemplateToFolder(string folder)
		{
			m_Template = $"{folder}{{name}}.asset";
		}

		public override void OnGUI()
		{
			if (m_IsFirstTime)
			{
				InitFirstTime();
				m_IsFirstTime = false;
			}

			bool wasEmpty = m_IsNameEmpty;
			m_IsNameEmpty = GUILayout.Toggle(
				m_IsNameEmpty, "Do not use name", GUILayout.ExpandWidth(false));

			bool wasOverriden = IsFolderOverridden;
			IsFolderOverridden = GUILayout.Toggle(
				IsFolderOverridden, "Override template", GUILayout.ExpandWidth(false));
			if (IsFolderOverridden)
			{
				if (!wasOverriden)
				{
					SetTemplateToFolder(TryGetCurrentActiveObjectFolder());
				}
				if (GUILayout.Button("Set Custom Folder", GUILayout.ExpandWidth(true)))
				{

					string folder = EditorUtility.OpenFolderPanel(
						"Shared String Folder",
						TryGetCurrentActiveObjectFolder(),
						"");
					if (!string.IsNullOrEmpty(folder))
					{
						folder = folder[(Application.dataPath.Length - "Assets".Length)..] + "/";
						SetTemplateToFolder(folder);
					}
				}
			}
			else
			{
				ReloadTemplate();
			}

			if (!IsFolderOverridden)
			{
				using (GuiScopes.Horizontal())
				{
					GenerateControls();
				}
			}
		}

		private void UpdateLocations(string chapterName)
		{
			var namings = LocationsByChapter.instance.GetLocationNames(chapterName);
			if (namings == null)
			{
				m_Namings.Remove(nameof(Location));
			}
			else
			{
				m_Namings[nameof(Location)] = namings.ToArray();
			}
		}

		public override string ProcessTemplate(string? assetName = null)
		{
			string path = UpdateTemplateResult(m_Template, DefaultTemplate, assetName);
			path = ReplaceTemplates(path, new SerializedObject(this));
			if (CreatesBlueprints)
			{
				if (path.StartsWith(UnityBlueprintsFolder))
				{
					path = path[UnityBlueprintsFolder.Length..];
				}
				path = path.Replace(".asset", ".jbp");
			}
			return path;
		}

		protected string UpdateTemplateResult(string? template, string defaultTemplate, string? assetName = null)
		{
			string templateResult = template ?? defaultTemplate;

			templateResult = m_IsNameEmpty
				? templateResult.Replace(NameToken, "")
				: templateResult.Replace(NameToken, NameTokenNotEmpty);

			foreach (var (typeName, naming) in m_Result)
			{
				if (typeName == nameof(Chapter))
				{
					UpdateLocations(naming);
				}
				var match = m_TemplateMatches.FindOrDefault(match => match.Groups[1].Value == typeName);
				if (match != null)
				{
					templateResult = templateResult.Replace(match.Value, naming);
				}
			}

			assetName = string.IsNullOrEmpty(assetName) ? NewAssetWindow.AssetName : assetName;
			if (!string.IsNullOrEmpty(assetName))
			{
				var nameMatch = m_TemplateMatches.FindOrDefault(match => match.Groups[1].Value == NewAssetWindow.NamePart);
				if (nameMatch != null)
				{
					templateResult = templateResult.Replace(nameMatch.Value, assetName);
				}
			}

			return templateResult;
		}
	}
}