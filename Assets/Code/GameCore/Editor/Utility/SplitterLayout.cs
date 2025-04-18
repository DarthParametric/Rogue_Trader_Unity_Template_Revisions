using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor.Utility
{
	public class SplitterLayout
	{
		private static readonly MethodInfo s_BeginMethod;
		private static readonly MethodInfo s_EndMethod;
		private static readonly Type s_SplitterType;
		private static readonly Type s_StateType;

		static SplitterLayout()
		{
			s_SplitterType = typeof(EditorGUI).Assembly.GetType("UnityEditor.SplitterGUILayout");

			s_BeginMethod =
				s_SplitterType.GetMethods(BindingFlags.Static | BindingFlags.Public)
					.Single(m => (m.Name == "BeginHorizontalSplit") && (m.GetParameters().Length == 2));
			s_EndMethod = s_SplitterType.GetMethod("EndVerticalSplit", BindingFlags.Static | BindingFlags.Public);

			s_StateType = typeof(EditorGUI).Assembly.GetType("UnityEditor.SplitterState");
		}

		public static object CreateSplitterState(params float[] relSizes)
		{
			return Activator.CreateInstance(s_StateType, relSizes);
		}

		public static void BeginVertical(object state, params GUILayoutOption[] opts)
		{
			s_BeginMethod.Invoke(null, new[] {state, opts});
		}

		public static void EndVertical()
		{
			s_EndMethod.Invoke(null, null);
		}
	}
}