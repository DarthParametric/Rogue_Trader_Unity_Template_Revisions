using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories;
using Kingmaker.Editor.NodeEditor.Window;
using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.ElementsSystem;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class FactoryDragAndDropComponent : DragAndDropComponent
	{
		private readonly Type m_ElementType;

		private readonly Action m_CreateContentPart;

		private readonly Action m_RemoveContentPart;

		public FactoryDragAndDropComponent(Type elementType, Action createContentPart, Action removeContentPart) 
			: base(null, null)
		{
			m_ElementType = elementType;
			IsValidateFunc = IsDropValid;
			DropFunc = DropProcess;
			m_RemoveContentPart = removeContentPart;
			m_CreateContentPart = createContentPart;
		}

		private bool IsDropValid()
		{
			ElementDragAndDropController.InitFactories(m_ElementType);
			if (!ElementDragAndDropController.HasFactories(m_ElementType))
				return false;

			return true;
		}

		private void DropProcess()
		{
			var factories = ElementDragAndDropController.GetFactories(m_ElementType);
			if (factories.Count == 0)
				return;

			if (factories.Count == 1)
			{
				OverrideProperty(factories[0]);
			}
			else
			{
				ElementFactoryWithSourcePicker.Show(
					Property,
					"Pick result",
					() => factories,
					selectedFactory => OverrideProperty(selectedFactory)
				);
			}

			if (NodeEditorBase.Drawing)
				BlueprintNodeEditor.CheckForNewNodes();
		}

		private void OverrideProperty(ElementFactoryWithSource factory)
		{
			SimpleBlueprint wrappedInstance = null;
			if (Property.Property.serializedObject.targetObject is BlueprintEditorWrapper bew)
				wrappedInstance = bew.WrappedInstance;
			
            var oldElement = ((Element)Property.Property.boxedValue);
            if (oldElement != null)
            {
	            oldElement.Delete();
	            m_RemoveContentPart();
            }

            var element = factory.Factory.Create(wrappedInstance, factory.Source);

            Property.Property.boxedValue = element;
            Property.Property.serializedObject.ApplyModifiedProperties();
            m_CreateContentPart();
		}
	}
}