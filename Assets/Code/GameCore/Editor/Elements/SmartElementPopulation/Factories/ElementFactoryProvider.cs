using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.ElementsSystem;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories
{
	public class ElementFactoriesProvider
	{
		public static readonly ElementFactoriesProvider Instance = new ElementFactoriesProvider();

		private readonly Dictionary<ElementFactoryProviderKey, List<IElementFactory<Element>>> m_Mapping =
			new Dictionary<ElementFactoryProviderKey, List<IElementFactory<Element>>>();

		private ElementFactoriesProvider()
		{
			var existingFactories = TypeCache.GetTypesDerivedFrom<IElementFactoryMarker>()
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.Where(p => IsAssignableToGenericType(p, typeof(ElementFactory<,>)));

			var elementWithAncestors = new List<Type>();
			var sourceWithDescendants = new List<Type>();
			foreach (var factory in existingFactories)
			{
				elementWithAncestors.Clear();
				sourceWithDescendants.Clear();

				(Type element, Type source) = GetElementAndSourcePair(factory);
				var currentElement = element;
				while (currentElement != null && currentElement != typeof(ScriptableObject))
				{
					elementWithAncestors.Add(currentElement);
					currentElement = currentElement.BaseType;
				}

				sourceWithDescendants.Add(source);
				sourceWithDescendants.AddRange(TypeCache.GetTypesDerivedFrom(source));

				var factoryInstance = (IElementFactory<Element>)Activator.CreateInstance(factory);
				foreach (var elementType in elementWithAncestors)
				{
					foreach (var sourceType in sourceWithDescendants)
					{
						ElementFactoryProviderKey key = new ElementFactoryProviderKey(elementType, sourceType);
						if (!m_Mapping.TryGetValue(key, out var factories))
						{
							factories = new List<IElementFactory<Element>>();
							m_Mapping[key] = factories;
						}

						factories.Add(factoryInstance);
					}
				}
			}
		}

		[NotNull]
		public IEnumerable<ElementFactoryWithSource> FindFactories(Type elementType, IEnumerable<object> sources)
		{
			if (sources == null || sources.Empty())
				return Enumerable.Empty<ElementFactoryWithSource>();

			var result = new List<ElementFactoryWithSource>();
			foreach (var source in sources)
			{
				foreach (var factory in FindFactories(elementType, source.GetType()))
				{
					result.Add(new ElementFactoryWithSource(factory, source));
				}
			}

			return result;
		}

		[NotNull]
		public IEnumerable<IElementFactory<Element>> FindFactories(Type elementType, Type sourceType)
		{
			var key = new ElementFactoryProviderKey(elementType, sourceType);

			var result = Enumerable.Empty<IElementFactory<Element>>();
			if (m_Mapping.TryGetValue(key, out var factories))
			{
				result = factories;
			}

			return result;
		}

		private static (Type, Type) GetElementAndSourcePair(Type currentType)
		{
			Type resultType = currentType;

			while (!resultType.IsGenericType ||
			       resultType.GetGenericTypeDefinition() != typeof(ElementFactory<,>))
			{
				resultType = currentType.BaseType;
			}

			var arguments = resultType.GetGenericArguments();

			return (arguments[0], arguments[1]);
		}

		private static bool IsAssignableToGenericType(Type givenType, Type genericType)
		{
			if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
				return true;

			Type baseType = givenType.BaseType;

			return baseType != null && IsAssignableToGenericType(baseType, genericType);
		}

		private class ElementFactoryProviderKey
		{
			private readonly Type m_ElementType;
			private readonly Type m_SourceType;

			public ElementFactoryProviderKey(Type elementType, Type sourceType)
			{
				m_ElementType = elementType;
				m_SourceType = sourceType;
			}

			private bool Equals(ElementFactoryProviderKey other)
			{
				return m_ElementType == other.m_ElementType && m_SourceType == other.m_SourceType;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((ElementFactoryProviderKey)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((m_ElementType != null ? m_ElementType.GetHashCode() : 0) * 397) ^
					       (m_SourceType != null ? m_SourceType.GetHashCode() : 0);
				}
			}

			public override string ToString()
			{
				return $"{nameof(m_ElementType)}: {m_ElementType}, {nameof(m_SourceType)}: {m_SourceType}";
			}
		}
	}
}