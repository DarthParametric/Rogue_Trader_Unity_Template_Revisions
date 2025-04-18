using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Designers.Attributes;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories;
using Kingmaker.Editor.Workspace;
using Owlcat.Runtime.Core.Utility;
using UnityEngine;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation
{
	public static class ElementWorkspaceContextualPopulationController
	{
		private const BindingFlags ApplicableFieldsBindingFlags =
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly List<FieldInfo> CandidatesForFirstTarget = new List<FieldInfo>();
		private static readonly List<FieldInfo> CandidatesForSecondTarget = new List<FieldInfo>();

		public static void PrefillWithTargets(object obj, SimpleBlueprint owner)
		{
			var firstWorkspaceItem = WorkspaceCanvasWindow.FirstTarget;
			var secondWorkspaceItem = WorkspaceCanvasWindow.SecondTarget;

			if (firstWorkspaceItem == null && secondWorkspaceItem == null)
				return;

			CandidatesForFirstTarget.Clear();
			CandidatesForSecondTarget.Clear();

			var fieldInfos = obj.GetType().GetFields(ApplicableFieldsBindingFlags);
			foreach (var fieldInfo in fieldInfos)
			{
				if (fieldInfo.HasAttribute<WorkspaceIgnoreTarget>()) continue;

				if (fieldInfo.HasAttribute<WorkspaceSecondTarget>())
					CandidatesForSecondTarget.Add(fieldInfo);
				else
					CandidatesForFirstTarget.Add(fieldInfo);
			}

            // todo: [bp] owner is never actually used, so we should just remove it
			Populate(obj, null, firstWorkspaceItem, CandidatesForFirstTarget);
			Populate(obj, null, secondWorkspaceItem.Or(firstWorkspaceItem), CandidatesForSecondTarget);
		}

		private static void Populate(
			object obj, SimpleBlueprint owner, Object workspaceTargetItem, List<FieldInfo> relevantFieldsInfos)
		{
			if (workspaceTargetItem == null || relevantFieldsInfos.Count == 0)
				return;

			var sources = GetPotentialSources(workspaceTargetItem);
			foreach (var fieldInfo in relevantFieldsInfos)
			{
				var factoriesWithSources =
					ElementFactoriesProvider.Instance.FindFactories(fieldInfo.FieldType, sources);
				if (factoriesWithSources.Empty()) continue;

				var factoryWithSource = factoriesWithSources.First();
				var source = factoryWithSource.Source;
				var elementFactory = factoryWithSource.Factory;

				var element = elementFactory.Create(owner, source);
				fieldInfo.SetValue(obj, element);
			}
		}

		private static IEnumerable<object> GetPotentialSources(Object unityObject)
		{
			if (unityObject is GameObject gameObject)
			{
				return gameObject.GetComponents(typeof(Component));
			}

			return Enumerable.Repeat(unityObject, 1);
		}
	}
}