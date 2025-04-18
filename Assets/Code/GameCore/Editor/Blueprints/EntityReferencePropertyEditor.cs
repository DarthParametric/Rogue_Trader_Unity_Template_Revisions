#if UNITY_EDITOR && EDITOR_FIELDS
using Kingmaker.Blueprints;
using Kingmaker.Editor.UIElements.Custom;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(EntityReference))]
	class EntityReferencePropertyEditor : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new EntityReferenceProperty(property);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawInspectorHelper.DrawEntityReference(fieldInfo, property);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
}
#endif