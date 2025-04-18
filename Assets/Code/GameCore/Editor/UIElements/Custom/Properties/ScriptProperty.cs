using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class ScriptProperty : OwlcatProperty
	{
		public ScriptProperty(SerializedProperty prop) : base(prop)
		{
			_objectField = new ObjectField() { value = prop.objectReferenceValue, label = prop.displayName };
			_objectField.SetEnabled(false);
			_objectField.AddToClassList("owlcat-inner-field");
			ContentContainer.Add(_objectField);
			RegisterCallback<MouseDownEvent>(Click);
		}

		ObjectField _objectField;

		private void Click(MouseDownEvent evt)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Select in Project"), false, () => Selection.activeObject = _objectField.value);
			menu.AddItem(new GUIContent("Open in Editor"), false, () => AssetDatabase.OpenAsset(_objectField.value));
			menu.ShowAsContext();
		}
	}
}