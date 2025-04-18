
#if UNITY_EDITOR && EDITOR_FIELDS
using Kingmaker.AreaLogic.SceneControllables;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


[CustomPropertyDrawer(typeof(ControllableReference))]
public class ControllableComponentPropertyEditor : PropertyDrawer
{
	public override VisualElement CreatePropertyGUI(SerializedProperty property)
		=> new ControllableReferenceProperty(property,  typeof(ControllableComponent));
	
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		DrawInspectorHelper.DrawControllableReference(fieldInfo, property);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return 0;
	}
}

[CustomPropertyDrawer(typeof(ControllableAnimatorSetStateReference))]
public class ControllableGameObjectComponentPropertyEditor : PropertyDrawer
{
	public override VisualElement CreatePropertyGUI(SerializedProperty property)
		=> new ControllableReferenceProperty(property, typeof(ControllableAnimator));
	
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		DrawInspectorHelper.DrawControllableReference(fieldInfo, property);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return 0;
	}
}

#endif
