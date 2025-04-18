using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class OwlcatObjectWithDefaultPickerProperty : OwlcatProperty
    {
        public OwlcatObjectWithDefaultPickerProperty(SerializedProperty prop, [NotNull] FieldInfo info) : base(prop)
        {
            ObjectField = new OwlcatObjectWithDefaultPickerField(prop.displayName, info);
            ContentContainer.Add(ObjectField);
            ObjectField.BindProperty(prop); // should be OwlcatBind, but it does not work for object fields because magic
        }

        public OwlcatObjectWithDefaultPickerField ObjectField { get; }
    }
    
    public class OwlcatObjectWithDefaultPickerField : ObjectField
    {
        public OwlcatObjectWithDefaultPickerField(string label, [NotNull] FieldInfo fieldInfo) : base(label)
        {
            objectType = fieldInfo.FieldType;
            var info = typeof(OwlcatObjectField).GetMethod("get_visualInput", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            var control = (VisualElement)info.Invoke(this, null);
            var point = control.hierarchy[1];
            control.Remove(point);

            var newPoint = new VisualElement();
            foreach (string clazz in point.GetClasses())
            {
                newPoint.AddToClassList(clazz);
            }

            style.flexGrow = 1;
            newPoint.RegisterCallback<MouseDownEvent>(OpenPicker);
            control.Add(newPoint);
        }

        private void OpenPicker(MouseDownEvent evt)
        {
            WorkaroundUnityUIToolkitBrokenObjectSelector.ShowObjectPicker(objectType, _ => {}, x => value = x, value);
        }
    }
    public static class WorkaroundUnityUIToolkitBrokenObjectSelector
    {
        public static void ShowObjectPicker(
            Type requiredType,
            Action<Object> onSelectorClosed,
            Action<Object> onSelectionChanged,
            Object initialValueOrNull = null)
        {
            var objectSelectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ObjectSelector");
            PropertyInfo getProperty = objectSelectorType.GetProperty("get", BindingFlags.Public | BindingFlags.Static);
            if (getProperty == null)
            {
                EditorUtility.DisplayDialog("Unity Editor api changed",
                    "Default object picker is now broken, please file a bug report to programmers.\n", ":( Okay");
                return;
            }

            object selectorWindowInstance = getProperty.GetValue(null);

            MethodInfo miShow = objectSelectorType.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[]
                {
                    typeof(Object),
                    typeof(Type),
                    typeof(Object),
                    typeof(bool),
                    typeof(List<int>),
                    typeof(Action<Object>),
                    typeof(Action<Object>),
                    typeof(bool)
                }, System.Array.Empty<ParameterModifier>());
            
            if (miShow == null)
            {
                EditorUtility.DisplayDialog("Unity Editor api changed",
                    "Default object picker is now broken, please file a bug report to programmers.\n", ":( Okay");
                return;
            }

            miShow.Invoke(selectorWindowInstance, new object[]
                {
                    initialValueOrNull,
                    requiredType,
                    null,
                    false,
                    null,
                    onSelectorClosed,
                    onSelectionChanged,
                    true
                }
            );
        }
    }
}