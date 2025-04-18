using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.DragDrop;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation
{
	public static class ElementDragAndDropController
	{
		public static readonly GUIStyle PreDropStyle = EditorStyles.foldoutPreDrop;

		private static EventType? s_CurrentEventType;

		private static readonly Dictionary<Type, List<ElementFactoryWithSource>> ElementFactories =
			new Dictionary<Type, List<ElementFactoryWithSource>>();

		public static void OnBeforeElementGUI(Type elementType)
		{
			s_CurrentEventType = Event.current.type;
			if (s_CurrentEventType == EventType.Repaint || s_CurrentEventType == EventType.Layout) return;
			if (s_CurrentEventType != EventType.DragUpdated && s_CurrentEventType != EventType.DragPerform)
			{
				ElementFactories.Clear();
				return;
			}

			InitFactories(elementType);
		}

		public static void InitFactories(Type elementType)
		{
			if (!ElementFactories.TryGetValue(elementType, out var elementFactories))
			{
				elementFactories = new List<ElementFactoryWithSource>();
				ElementFactories[elementType] = elementFactories;
			}

			elementFactories.Clear();
			var draggedObjects = DragManager.Instance.DragInProgress
				? DragManager.Instance.UnityObjects
				: DragAndDrop.objectReferences;

			if (draggedObjects.Length != 1)
				return;

			foreach (var source in GetPotentialSources(draggedObjects[0]))
			{
				var sourceType = source.GetType();
				var factories = ElementFactoriesProvider.Instance.FindFactories(elementType, sourceType);
				if (factories.Empty()) 
					continue;

				foreach (var factory in factories)
				{
					elementFactories.Add(new ElementFactoryWithSource(factory, source));
				}
			}
		}

		public static void OnAfterElementGUI()
		{
			if (s_CurrentEventType == EventType.DragPerform || s_CurrentEventType == EventType.DragExited)
				ElementFactories.Clear();
		}

		public static bool HasFactories(Type elementType)
		{
			int factoriesAmount = ElementFactories.Get(elementType)?.Count ?? 0;

			return factoriesAmount > 0;
		}

		public static bool HasProperElementDropped(Type elementType, Rect rect)
		{
			if (!HasFactories(elementType)) return false;
			if (!rect.Contains(Event.current.mousePosition)) return false;
			if (s_CurrentEventType != EventType.DragUpdated && s_CurrentEventType != EventType.DragPerform)
				return false;

			bool result;
			DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			result = s_CurrentEventType == EventType.DragPerform;

			return result;
		}

		public static List<ElementFactoryWithSource> GetFactories(Type elementType)
		{
			return new List<ElementFactoryWithSource>(ElementFactories[elementType]);
		}

		private static IEnumerable<object> GetPotentialSources(Object unityObject)
		{
			if (unityObject is GameObject gameObject)
			{
				return gameObject.GetComponents(typeof(Component));
			}
			if(unityObject is BlueprintEditorWrapper bew)
            {
				return Enumerable.Repeat(bew.Blueprint, 1);
			}
            
			return Enumerable.Repeat(unityObject, 1);
		}
	}
}