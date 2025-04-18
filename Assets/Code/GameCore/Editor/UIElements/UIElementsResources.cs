using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Kingmaker.Editor.UIElements
{
	public static class UIElementsResources
	{
		static UIElementsResources()
		{
			FoldoutCollapsed = LoadIcon("IN foldout");
			FoldoutExpanded = LoadIcon("IN foldout on");
			Background = LoadIcon("IN BigTitle");
			NewWindowIcon = LoadIcon("ScaleTool");
			SettingsIcon = LoadIcon("SettingsIcon");
			OverrideIcon = EditorGUIUtility.Load("override.png") as Texture2D;
			RevertOverrideIcon = EditorGUIUtility.Load("revert.png") as Texture2D;
		}

		public static Texture2D FoldoutCollapsed { get; }
		public static Texture2D FoldoutExpanded { get; }
		public static Texture2D Background { get; }
		public static Texture2D NewWindowIcon { get; }
		public static Texture2D SettingsIcon { get; }
		public static Texture2D OverrideIcon { get; }
		public static Texture2D RevertOverrideIcon { get; }

		static Color m_ArrayZebraColor1 = new Color(0.79f, 0.78f, 0.78f);
		static Color m_ArrayZebraColor2 = new Color(0.72f, 0.72f, 0.72f);
		static Color m_ArrayZebraColorPro1 = new Color(0.2f, 0.2f, 0.2f);
		static Color m_ArrayZebraColorPro2 = new Color(0.14f, 0.14f, 0.14f);

		public static Color GetZebra(int index)
			=> index % 2 == 0 ?
			(EditorGUIUtility.isProSkin ? m_ArrayZebraColorPro1 : m_ArrayZebraColor1) :
			(EditorGUIUtility.isProSkin ? m_ArrayZebraColorPro2 : m_ArrayZebraColor2);

		public static Texture2D LoadIcon(string name)
		{
			if (UnityEditorInternal.InternalEditorUtility.HasPro())
			{
				var newName = "d_" + Path.GetFileName(name);
				var dirName = Path.GetDirectoryName(name);
				if (!String.IsNullOrEmpty(dirName))
					newName = String.Format("{0}/{1}", dirName, newName);

				Texture2D tex = LoadIconInternal(newName);
				if (tex != null)
				{
					return tex;
				}
				else
				{
					return LoadIconInternal(name);
				}
			}
			else
			{
				return LoadIconInternal(name);
			}
		}

		static Texture2D LoadIconInternal(string name)
		{
			var tex = EditorGUIUtility.Load(EditorResources.generatedIconsPath + name + ".asset") as Texture2D;

			if (!tex)
			{
				tex = EditorGUIUtility.Load(EditorResources.iconsPath + name + ".png") as Texture2D;
			}
			if (!tex)
			{
				tex = EditorGUIUtility.Load(name) as Texture2D; // Allow users to specify their own project path to an icon (e.g see EditorWindowTitleAttribute)
			}

			return tex;
		}
	}
}