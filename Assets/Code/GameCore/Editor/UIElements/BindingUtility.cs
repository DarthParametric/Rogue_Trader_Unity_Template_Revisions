using Kingmaker.Editor.UIElements.Custom;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
	public static class BindingUtility
	{
		private static readonly Dictionary<VisualElement, SerializedObject> m_Bindable =
			new Dictionary<VisualElement, SerializedObject>();
			
		public static void OwlcatBind<T>(this T bindable, SerializedProperty prop) where T : VisualElement, IBindable
		{
			if (UIElementsUtility.InitializationProcessFlag.Flag)
				return;
			
			bindable.bindingPath = prop.propertyPath;
			OwlcatBind(bindable, prop.serializedObject);
		}
		
		/// <summary>
		/// it's higher-performance then base Unity Engine binding
		/// </summary>
		public static void OwlcatBind<T>(this T bindable, SerializedObject so)
			where T : VisualElement
		{
			if (so == null)
			{
				UnityEngine.Debug.LogError("so is null");
			}

			if (UIElementsUtility.InitializationProcessFlag.Flag)
				return;

			m_Bindable[bindable] = so;
			if (m_Bindable.Count == 1)
			{
				EditorApplication.delayCall += Bind;
			}
		}
		

		private static void Bind()
		{
			Profiler.BeginSample("BindingUtility.Bind");
			var bindDict = new Dictionary<VisualElement, SerializedObject>();
			foreach (var element in m_Bindable)
			{
				var root = GetRoot(element.Key);
				if (root != null && !bindDict.ContainsKey(root))
				{
					bindDict.Add(root, element.Value);
				}
			}

			foreach (var pair in bindDict)
			{
				pair.Key.Bind(pair.Value);
			}

			m_Bindable.Clear();
			Profiler.EndSample();
		}

		private static VisualElement GetRoot(VisualElement element)
		{
			while (element.parent != null)
			{
				if (element is OwlcatInspectorRoot iRoot)
				{
					return iRoot;
				}

				if (element is ComponentElement cRoot)
				{
					return cRoot;
				}

				element = element.parent;
			}

			return default;
		}
	}
}