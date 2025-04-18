using System;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Utility
{
	/// <summary>
	/// SerializedProperty wrapper that stores object links(s) and path to ensure property survives original object being disposed
	/// </summary>
	public class RobustSerializedProperty
	{
		private SerializedProperty m_Property;
		private readonly Object[] m_Targets;
		private readonly Object m_Context;
		private readonly string m_Path;

		public RobustSerializedProperty([NotNull]SerializedProperty property)
		{
			m_Property = property.Copy();
			m_Targets = m_Property.serializedObject.targetObjects;
			m_Context = m_Property.serializedObject.context;
			m_Path = m_Property.propertyPath;
		}

		public SerializedProperty Property
		{
			get
			{
				try
				{
					// check if property is disposed. There's no IsDisposed there, so the only thing we can do is try to read anything and catch the resultiung exception
					var dummy = m_Property.depth;
					var dummy2 = m_Property.serializedObject.maxArraySizeForMultiEditing;
					if (!m_Property.serializedObject.targetObject)
						throw new Exception();
				}
				catch (Exception)
				{
					try
					{
						var so = new SerializedObject(m_Targets, m_Context);
						m_Property = so.FindProperty(m_Path);
					}
					catch
					{
						return null;
					}
				}
				return m_Property;
			}
		}

		// ReSharper disable once InconsistentNaming
		public SerializedObject serializedObject
			=> Property.serializedObject;

		// ReSharper disable once InconsistentNaming
		public Object targetObject
			=> Property.serializedObject.targetObject;

		public string Path => m_Path;


		public static implicit operator RobustSerializedProperty(SerializedProperty property)
		{
			return property == null ? null : new RobustSerializedProperty(property);
		}
		public static implicit operator SerializedProperty(RobustSerializedProperty property)
		{
			return property?.Property;
		}
	}
}