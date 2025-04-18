using System;
using JetBrains.Annotations;

namespace Kingmaker.Editor.Blueprints
{
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Class)]
	public class BlueprintCustomEditorAttribute : Attribute
	{
		public Type InspectedType { get; set; }
		public bool EditorForChildClasses { get; set; }

		public BlueprintCustomEditorAttribute()
		{
		}

		public BlueprintCustomEditorAttribute(Type inspectedType)
		{
			InspectedType = inspectedType;
		}

		public BlueprintCustomEditorAttribute(Type inspectedType, bool editorForChildClasses)
		{
			InspectedType = inspectedType;
			EditorForChildClasses = editorForChildClasses;
		}
	}
}