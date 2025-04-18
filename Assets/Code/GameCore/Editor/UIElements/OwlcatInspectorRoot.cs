using System;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
	public class OwlcatInspectorRoot : OwlcatContentContainer
	{
		//Paths may change in nearest future. This is left to make search easier
		#if OWLCAT_MODS
		public const string CommonPath = "Assets/Code/GameCore/Editor/UIElements/Styles/CommonStyle.uss";
		public const string ProPath = "Assets/Code/GameCore/Editor/UIElements/Styles/ProStyle.uss";
		public const string PersonalPath = "Assets/GameCore/Code/Editor/UIElements/Styles/PersonalStyle.uss";
		#else
		public const string CommonPath = "Assets/Code/GameCore/Editor/UIElements/Styles/CommonStyle.uss";
		public const string ProPath = "Assets/Code/GameCore/Editor/UIElements/Styles/ProStyle.uss";
		public const string PersonalPath = "Assets/GameCore/Code/Editor/UIElements/Styles/PersonalStyle.uss";
		#endif

		public readonly SerializedObject SerializedObject;

		public OwlcatInspectorRoot(SerializedObject serializedObject, bool isHideScriptProp)
		{
			SerializedObject = serializedObject;
			name = serializedObject.targetObject.name;

			LoadStyles();
            // just debug
   //         var it = serializedObject.GetIterator();
   //         it.NextVisible(true);
   //         do
   //         {
   //             try
   //             {
   //                 var f = it.GetFieldInfo();
   //                 PFLog.Default.Log($"Prop: {it.propertyPath} is for field {f?.DeclaringType.Name}.{f?.Name}");
   //             }
   //             catch (Exception x)
   //             {
   //                 PFLog.Default.Log($"Prop: {it.propertyPath} throws");
   //                 PFLog.Default.Exception(x);
   //             }
			//} while (it.Next(true));
            
			SetupContent(this, serializedObject, isHideScriptProp);

			RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
		}

        public OwlcatInspectorRoot(SerializedProperty property)
        {
			SerializedObject = property.serializedObject;
            name = property.FindPropertyRelative("name")?.stringValue ?? property.serializedObject.targetObject.name;

            LoadStyles();
            if (property.hasVisibleChildren)
            {
                property = property.Copy(); // this is a root property, but for the SetupContent we need its first child
                if (property.NextVisible(true))
                {
                    SetupContent(this, property, true);
                }

                RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            }
        }

        private void LoadStyles()
		{
			var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin ? ProPath : PersonalPath);
			styleSheets.Add(styles);

			styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommonPath);
			styleSheets.Add(styles);
		}

		public static void SetupContent(OwlcatContentContainer root, SerializedObject serializedObject, bool isHideScriptProp)
		{
			var iterator = serializedObject.GetIterator();
			if (iterator.NextVisible(true))
			{
				SetupContent(root, iterator, isHideScriptProp);
			}
		}


		public static void SetupContent(OwlcatContentContainer root, SerializedProperty rootProperty, bool isHideScriptProp)
        {
            var startDepth = rootProperty.depth; 
            do
            {
                if (rootProperty.propertyPath.Equals("m_Script"))
                {
                    if (!isHideScriptProp)
                    {
                        var field = new ScriptProperty(rootProperty);
                        root.Add(field);
                    }

                    continue;
                }

                try
                {
                    var propField = UIElementsUtility.CreatePropertyElement(rootProperty, false);
                    if (propField != null)
                    {
                        root.Add(propField);
                    }
                }
                catch (Exception e)
                {
                    root.Add(new ErrorElement($"{rootProperty.displayName}: {e.Message}",
                        $"Mess: {e.Message}\nTrace: {e.StackTrace}"));
                }
            } while (rootProperty.NextVisible(false) && rootProperty.depth>=startDepth); // if we did not start at root, exit the loop when we get out of the property
        }

		private void OnKeyDown(KeyDownEvent evt)
		{
			bool handled = false;
			switch (evt.keyCode)
			{
				case KeyCode.UpArrow:
					handled = FocusPrev();
					break;
				case KeyCode.DownArrow:
					handled = FocusNext();
					break;
				case KeyCode.LeftArrow:
					handled = SetExpanded(false);
					break;
				case KeyCode.RightArrow:
					handled = SetExpanded(true);
					break;
				case KeyCode.Return:
					handled = EnterElement();
					break;
				case KeyCode.Escape:
					handled = ExitElement();
					break;

				default:
					var current = focusController?.GetFocusedProperty();
					current?.TryHandle(evt);
					break;
			}

			if (handled)
			{
				evt.PreventDefault();
				evt.StopImmediatePropagation();
			}
		}

		private bool FocusPrev()
		{
			if (focusController?.focusedElement is TextField textField && textField.multiline)
			{
				return false;
			}

			GetPrevProperty()?.Focus();
			return true;
		}

		private bool FocusNext()
		{
			if (focusController?.focusedElement is TextField textField && textField.multiline)
			{
				return false;
			}

			GetNextProperty()?.Focus();
			return true;
		}

		private bool EnterElement()
		{
			if (focusController?.focusedElement is OwlcatVisualElement)
			{
				var current = focusController?.GetFocusedProperty();
				current.GetAllChildren()
					.FirstOrDefault(c
						=> c.focusable && (c is PropertyField || c is IMGUIContainer ||
										   c.GetType().Assembly.FullName.StartsWith("UnityEditor")))?
					.Focus();
				return true;
			}

			return false;
		}

		private bool ExitElement()
		{
			var current = focusController?.GetFocusedProperty();
			current?.Focus();
			return true;
		}

		[CanBeNull]
		public OwlcatProperty GetPrevProperty()
		{
			var current = focusController?.GetFocusedProperty();
			if (current == null)
			{
				return this.GetAllProperties()?.FirstOrDefault();
			}

			OwlcatProperty prev = null;
			foreach (var e in this.GetAllProperties())
			{
				string prevPropertyPath = prev?.PropertyPath;
				if (e.PropertyPath == current.PropertyPath &&
					prevPropertyPath != current.PropertyPath)
				{
					return prev;
				}

				prev = e;
			}

			return null;
		}

		[CanBeNull]
		public OwlcatProperty GetNextProperty()
		{
			var current = focusController?.GetFocusedProperty();
			if (current == null)
			{
				return this.GetAllProperties()?.FirstOrDefault();
			}

			OwlcatProperty prev = null;
			foreach (var e in this.GetAllProperties())
			{
				string prevPropertyPath = prev?.PropertyPath;
				if (prevPropertyPath == current.PropertyPath &&
					e.PropertyPath != current.PropertyPath)
				{
					return e;
				}

				prev = e;
			}

			return null;
		}

		private bool SetExpanded(bool expanded)
		{
			var current = focusController?.GetFocusedProperty();
			if (current == null || !current.Expandable)
			{
				return false;
			}

			current.IsExpanded = expanded;
			return true;
		}
	}
}