using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Designers.EventConditionActionSystem.NamedParameters;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.Spawners;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class EditorCutsceneParams : ScriptableSingleton<EditorCutsceneParams>
	{
#region Parameters

		[Serializable]
		private class Entry
		{
			[SerializeField]
			public string? Parameter;

			[SerializeField]
			public ParametrizedContextSetter.ParameterType ParameterType;

			private bool IsUnit
				=> ParameterType == ParametrizedContextSetter.ParameterType.Unit;
			[SerializeField]
			[ShowIf(nameof(IsUnit))]
			public UnitSpawnerBase? UnitSpawner;

			private bool IsLocator
				=> ParameterType == ParametrizedContextSetter.ParameterType.Locator;
			[SerializeField]
			[ShowIf(nameof(IsLocator))]
			public LocatorView? LocatorView;

			private bool IsMapObject
				=> ParameterType == ParametrizedContextSetter.ParameterType.MapObject;
			[SerializeField]
			[ShowIf(nameof(IsMapObject))]
			public MapObjectView? MapObjectView;

			private bool IsPosition
				=> ParameterType == ParametrizedContextSetter.ParameterType.Position;
			[SerializeField]
			[ShowIf(nameof(IsPosition))]
			public Vector3 Position;

			private bool IsBlueprint
				=> ParameterType == ParametrizedContextSetter.ParameterType.Blueprint;
			[SerializeField]
			[ShowIf(nameof(IsBlueprint))]
			public BlueprintEditorWrapper? Blueprint;

			private bool IsFloat
				=> ParameterType == ParametrizedContextSetter.ParameterType.Float;
			[SerializeField]
			[ShowIf(nameof(IsFloat))]
			public float Float;
		}

		[SerializeField]
		private Entry[]? Params;

#endregion

#region Parameters access

		public static UnitSpawnerBase? GetUnit(string paramName)
		{
			return instance.Params?.FindOrDefault(p => p.Parameter == paramName)?.UnitSpawner;
		}

		public static LocatorView? GetLocator(string paramName)
		{
			return instance.Params?.FindOrDefault(p => p.Parameter == paramName)?.LocatorView;
		}

		public static MapObjectView? GetMapObject(string paramName)
		{
			return instance.Params?.FindOrDefault(p => p.Parameter == paramName)?.MapObjectView;
		}

		public static Vector3? GetPosition(string paramName)
		{
			return instance.Params?.FindOrDefault(p => p.Parameter == paramName)?.Position;
		}

		public static BlueprintScriptableObject? GetBlueprint(string paramName)
		{
			var wrapper = instance.Params?.FindOrDefault(p => p.Parameter == paramName)?.Blueprint;
			return wrapper == null ? null : wrapper.Blueprint as BlueprintScriptableObject;
		}

		public static float? GetFloat(string paramName)
		{
			return instance.Params?.FindOrDefault(p => p.Parameter == paramName)?.Float;
		}
#endregion
	}
}