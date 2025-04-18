using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Editor.KnowledgeDatabase.Data;
using JetBrains.Annotations;
using Kingmaker.Utility.DotNetExtensions;

namespace Code.Editor.KnowledgeDatabase.Serialization
{
	public static class KnowledgeDatabaseColumns
	{
	#region Columns 

		[UsedImplicitly] public static readonly Column Name = new TypeColumn
		{
			Getter = i => i.Name,
			Setter = (v, i) => i.Name = v,
			EveryRow = true
		};

		[UsedImplicitly] public static readonly Column Guid = new TypeColumn
		{
			Getter = i => i.Guid,
			Setter = (v, i) => i.Guid = v,
			EveryRow = true
		};

		[UsedImplicitly] public static readonly Column Type = new TypeColumn
		{
			Getter = i => i.Type,
			Setter = (v, i) => i.Type = v
		};

		[UsedImplicitly] public static readonly Column AllowedOn = new TypeColumn
		{
			Getter = i => i.AllowedOn,
			Setter = (v, i) => i.AllowedOn = v,
			ObsoleteNames = new[] {"Allowed On"}
		};

		[UsedImplicitly] public static readonly Column HasRuntime = new TypeColumn
		{
			Getter = i => i.HasRuntime.ToString(),
			Setter = (v, i) => i.HasRuntime = StringToBool(v)
		};

		[UsedImplicitly] public static readonly Column FieldPath = new FieldColumn
		{
			Getter = i => i.FieldPath,
			Setter = (v, i) => i.FieldPath = v,
			ObsoleteNames = new[] {"Field Name"}
		};
		
		[UsedImplicitly] public static readonly Column FieldName = new FieldColumn
		{
			Getter = i => i.FieldName,
			Setter = (v, i) => i.FieldName = v
		};

		[UsedImplicitly] public static readonly Column FieldType = new FieldColumn
		{
			Getter = i => i.Type,
			Setter = (v, i) => i.Type = v,
			ObsoleteNames = new[] {"Field Type"}
		};

		[UsedImplicitly] public static readonly Column FieldAllowedEntity = new FieldColumn
		{
			Getter = i => i.AllowedEntity,
			Setter = (v, i) => i.AllowedEntity = v,
			ObsoleteNames = new[] {"Field Allowed Entity"}
		};

        [UsedImplicitly]
        public static readonly Column CodeDescription = new CommonColumn
        {
            TypeGetter = i => i.CodeDescription,
            TypeSetter = (v, i) => i.CodeDescription = v,
            FieldGetter = i => i.CodeDescription,
            FieldSetter = (v, i) => i.CodeDescription = v
        };

        [UsedImplicitly] public static readonly Column Description = new CommonColumn
		{
			TypeGetter = i => i.Description,
			TypeSetter = (v, i) => i.Description = v,
			FieldGetter = i => i.Description,
			FieldSetter = (v, i) => i.Description = v
		};

		[UsedImplicitly] public static readonly Column Keywords = new CommonColumn
		{
			TypeGetter = i => i.Keywords,
			TypeSetter = (v, i) => i.Keywords = v,
			FieldGetter = i => i.Keywords,
			FieldSetter = (v, i) => i.Keywords = v
		};
		
		[UsedImplicitly] public static readonly Column Link = new CommonColumn()
		{
			TypeGetter = i => i.Link,
			TypeSetter = (v, i) => i.Link = v,
			FieldGetter = i => i.Link,
			FieldSetter = (v, i) => i.Link = v
		};

		[UsedImplicitly] public static readonly Column Removed = new CommonColumn
		{
			TypeGetter = i => i.IsRemoved.ToString(),
			TypeSetter = (v, i) => i.IsRemoved = StringToBool(v),
			FieldGetter = i => i.IsRemoved.ToString(),
			FieldSetter = (v, i) => i.IsRemoved = StringToBool(v),
		};

		[UsedImplicitly] public static readonly Column Obsolete = new CommonColumn
		{
			TypeGetter = i => i.IsObsolete.ToString(),
			TypeSetter = (v, i) => i.IsObsolete = StringToBool(v),
			FieldGetter = i => i.IsObsolete.ToString(),
			FieldSetter = (v, i) => i.IsObsolete = StringToBool(v),
		};

	#endregion
		
		private static readonly List<Column> ColumnsCache;
		
		public static readonly IReadOnlyList<Column> DefaultColumns;

		static KnowledgeDatabaseColumns()
		{
			var fields = typeof(KnowledgeDatabaseColumns).GetFields(BindingFlags.Public | BindingFlags.Static);
			var columns = new List<Column>();
			foreach (var field in fields)
			{
				if (!typeof(Column).IsAssignableFrom(field.FieldType))
					continue;

				var column = (Column)field.GetValue(null);
				column.Name = field.Name;
				columns.Add(column);
			}

			DefaultColumns = columns.ToArray();
			ColumnsCache = columns.ToList();
		}

		private static bool StringToBool(string value)
			=> bool.TryParse(value, out bool result) && result;

		public static Column Get(string name)
		{
			var column = ColumnsCache.FirstItem(i => i.Name == name || i.ObsoleteNames.Contains(name));
			if (column == null)
			{
				column = new MetaColumn(name);
				ColumnsCache.Add(column);
			}

			return column;
		}

		[NotNull]
		public delegate string TypeGetter([NotNull] KnowledgeDatabaseType type);
		
		[NotNull]
		public delegate string FieldGetter([NotNull] KnowledgeDatabaseField field);

		public delegate void TypeSetter([NotNull] string value, [NotNull] KnowledgeDatabaseType type);
		public delegate void FieldSetter([NotNull] string value, [NotNull] KnowledgeDatabaseField field);

		public abstract class Column
		{
			protected TypeGetter TypeGetter;
			protected TypeSetter TypeSetter;

			protected FieldGetter FieldGetter;
			protected FieldSetter FieldSetter;
			
			protected bool EveryRow;

			// ReSharper disable once MemberHidesStaticFromOuterClass
			public string Name { get; protected internal set; }
			public string[] ObsoleteNames { get; protected internal set; } = {};

			[NotNull]
			public string GetValue([CanBeNull] KnowledgeDatabaseType type, [CanBeNull] KnowledgeDatabaseField field)
			{
				string result = string.Empty;
				if (field != null && FieldGetter != null)
					result = FieldGetter.Invoke(field);
				else if (type != null && TypeGetter != null && (field == null || EveryRow))
					result = TypeGetter.Invoke(type);
				
				return result ?? string.Empty;
			}

			public void SetValue(
				[NotNull] string value, [CanBeNull] KnowledgeDatabaseType type, [CanBeNull] KnowledgeDatabaseField field)
			{
				if (field != null)
					FieldSetter?.Invoke(value, field);
				else if (type != null)
					TypeSetter?.Invoke(value, type);
			}
		}

		private class CommonColumn : Column
		{
			public new TypeGetter TypeGetter
			{
				set => base.TypeGetter = value;
			}

			public new TypeSetter TypeSetter
			{
				set => base.TypeSetter = value;
			}

			public new FieldGetter FieldGetter
			{
				set => base.FieldGetter = value;
			}

			public new FieldSetter FieldSetter
			{
				set => base.FieldSetter = value;
			}
		}

		private class TypeColumn : Column
		{
			public TypeGetter Getter
			{
				set => TypeGetter = value;
			}

			public TypeSetter Setter
			{
				set => TypeSetter = value;
			}

			public new bool EveryRow
			{
				set => base.EveryRow = value;
			}
		}

		private class FieldColumn : Column
		{
			public FieldGetter Getter
			{
				set => FieldGetter = value;
			}

			public FieldSetter Setter
			{
				set => FieldSetter = value;
			}
		}

		private class MetaColumn : Column
		{
			public MetaColumn(string name)
			{
				Name = name;
				TypeGetter = i => i.Meta.Get(Name) ?? string.Empty;
				TypeSetter = (v, i) => i.Meta[Name] = v;
				FieldGetter = i => i.Meta.Get(Name) ?? string.Empty;
				FieldSetter = (v, i) => i.Meta[Name] = v;
			}
		}
	}
}