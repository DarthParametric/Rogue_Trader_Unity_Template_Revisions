using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories
{
	public interface IElementFactory<out TE> where TE : Element
	{
		TE Create([NotNull] SimpleBlueprint owner, [NotNull] object source);

		string GetCaption();
	}

	public sealed class ElementFactoryWithSource
	{
		public readonly object Source;
		public readonly IElementFactory<Element> Factory;

		public ElementFactoryWithSource(IElementFactory<Element> factory, object source)
		{
			Source = source;
			Factory = factory;
		}

		public override string ToString()
		{
			return Factory.GetCaption();
		}
	}

	//Marker interface for UnityEditor.TypeCache
	public interface IElementFactoryMarker
	{
	}

	public abstract class ElementFactory<TE, TS> : IElementFactoryMarker, IElementFactory<TE>
		where TE : Element, new()
    {
		public TE Create(SimpleBlueprint owner, object source)
		{
            var element = (TE)Element.CreateInstance(typeof(TE));
			Populate(owner, element, (TS)source);
			owner?.AddNewElement(element); // owner is actually always null for new blueprints
			return element;
		}

		protected abstract void Populate(SimpleBlueprint owner, TE element, TS source);

		public virtual string GetCaption()
		{
			return $"{typeof(TE).Name} based on {typeof(TS).Name}";
		}

		public override string ToString()
		{
			return GetCaption();
		}
	}
}