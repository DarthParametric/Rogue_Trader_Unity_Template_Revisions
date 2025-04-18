using Kingmaker.Editor.Blueprints.Creation.Naming;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public abstract class TypedSharedStringCreator : SharedStringCreator
	{
		protected abstract string StringTypeName { get; }

		public override string CreatorName => $"Shared String ({StringTypeName})";

		protected override string TemplateOverride()
		{
			string template = base.TemplateOverride();
			return template.Replace($"{{{nameof(StringType)}}}", StringTypeName);
		}
	}
	public class ActionStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.Action.name;
	}
	public class BarkStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.Bark.name;
	}
	public class BuffStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.Buff.name;
	}
	public class EntryPointStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.EntryPoint.name;
	}
	public class ItemStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.Item.name;
	}
	public class LocationNameStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.LocationName.name;
	}
	public class NameStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.Name.name;
	}
	public class OtherStringCreator : TypedSharedStringCreator
	{
		protected override string StringTypeName
			=> StringTypesRoster.instance.Other.name;
	}
}