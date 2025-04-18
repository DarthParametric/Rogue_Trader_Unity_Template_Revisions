#if UNITY_EDITOR
using System.Linq;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem;
using UnityEditor;

namespace Kingmaker.Editor.Elements
{
	[CustomEditor(typeof(Element), true)]
	public class ElementInspector : UnityEditor.Editor
	{
		private static readonly string[] Excluded = { "m_Script", "m_ObjectHideFlags", "Not" };

		private static readonly string[] ExcludedOnNode = { "m_Script", "m_ObjectHideFlags" };
		//private string m_Stack;

		//public ElementInspector()
		//{
		//	m_Stack = StackTraceUtility.ExtractStackTrace();

		//}

		//void OnEnable()
		//{
		//	UberDebug.Log("ElementInspector .ctor stack: "+m_Stack);	
		//}

		public override void OnInspectorGUI()
		{
			bool includeChildrenPrev = PrototypedObjectEditorUtility.IncludeChildren;
			PrototypedObjectEditorUtility.IncludeChildren = true;

			serializedObject.Update();
			var excluded = NodeEditorBase.Drawing ? ExcludedOnNode : Excluded;
			DrawPropertiesExcluding(serializedObject, excluded);
			serializedObject.ApplyModifiedProperties();

			PrototypedObjectEditorUtility.IncludeChildren = includeChildrenPrev;
		}

		private new static void DrawPropertiesExcluding(SerializedObject obj, params string[] propertyToExclude)
		{
			SerializedProperty iterator = obj.GetIterator();
			
			bool more = iterator.Next(true); // (You need to call Next (true) on the first element to get to the first element)
			while (more)
			{
				more = propertyToExclude.Contains(iterator.name)
					? iterator.NextVisible(false)
					: PrototypedObjectEditorUtility.ShowPropertyRecursive(iterator);
			}
		}
	}
}
#endif