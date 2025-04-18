using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Code.Editor.KnowledgeDatabase.Data;
using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Utility.DotNetExtensions;
namespace Code.Editor.KnowledgeDatabase.Serialization
{
	public static class KnowledgeDatabaseSerializer
	{
		private const string Comma = ",";
		private const char Quote = '\'';
		private const char QuoteReplacement = '‘';
		
		public static void Serialize(StreamWriter stream, KnowledgeDatabase database)
		{
			var columns = database.Columns.EmptyIfNull()
				.Concat(KnowledgeDatabaseColumns.DefaultColumns.Except(database.Columns.EmptyIfNull()))
				.ToArray();

			// header
			WriteLine(stream, columns.Select(i => i.Name));

			foreach (var type in database.RecordsValues)
			{
				WriteRow(stream, columns, type, null);
				foreach (var field in type.Fields)
				{
					WriteRow(stream, columns, type, field.Value);
				}
			}
		}

		public static KnowledgeDatabase Deserialize(StreamReader stream)
		{
			string[] header = stream.ReadLine()?.Split(Comma);
			if (header == null ||
			    header.Any(i => i.Empty()) ||
			    header.GroupBy(i => i).Any(i => i.Count() > 1))
			{
				throw new Exception("Cannot deserialize KnowledgeDatabase");
			}

			var columns = header.Select(KnowledgeDatabaseColumns.Get).ToArray();
			var guidColumn = KnowledgeDatabaseColumns.Guid;
			int guidColumnIndex = columns.IndexOf(guidColumn);
			if (guidColumnIndex < 0)
				throw new Exception("Can't find Guid column");
			
			var records = new Dictionary<string, KnowledgeDatabaseType>();
			while (ReadRow(stream) is {} row)
			{
				string guid = row.Get(guidColumnIndex);
				if (guid == null)
					throw new Exception("Can't find Guid in row: " + string.Join(" ## ", row));
				
				var type = records.Get(guid);
				if (type == null)
				{
					type = records[guid] = new KnowledgeDatabaseType();
					ParseRow(row, columns, type, null);
					continue;
				}

				var field = new KnowledgeDatabaseField();
				ParseRow(row, columns, type, field);
				
				records[guid].Fields[field.FieldName] = field;
			}

			return new KnowledgeDatabase(records, columns);
		}

	#region Helpers

		private static void WriteRow(
			StreamWriter stream,
			IEnumerable<KnowledgeDatabaseColumns.Column> columns,
			[NotNull] KnowledgeDatabaseType type,
			[CanBeNull] KnowledgeDatabaseField field)
		{
			var values = columns.Select(i =>
			{
				string value = i.GetValue(type, field);
				if (value.Contains(Quote))
					value = value.Replace(Quote, QuoteReplacement);
				if (value.Contains('\n') || value.Contains(','))
					value = $"{Quote}{value}{Quote}";
				return value;
			});
			
			WriteLine(stream, values);
		}

		private static void WriteLine(StreamWriter stream, IEnumerable<string> values)
		{
			bool first = true;
			foreach (string value in values)
			{
				if (!first)
					stream.Write(Comma);
				first = false;
				
				stream.Write(value);
			}
			
			stream.WriteLine();
		}

		[CanBeNull]
		private static string[] ReadRow(StreamReader stream)
		{
			if (stream.EndOfStream)
				return null;
			
			var result = new List<string>();
			var buffer = new StringBuilder();

			bool insideQuotes = false;
			do
			{
				if (stream.ReadLine() is not {} line)
					return result.Empty() ? null : result.ToArray(); // end of stream

				// skip not quoted empty lines
				if (!insideQuotes && string.IsNullOrWhiteSpace(line))
					continue;

				if (insideQuotes)
					buffer.AppendLine();

				bool prevIsQuoted = insideQuotes;
				string[] quoteParts = line.Split(Quote);
				for (int i = 0; i < quoteParts.Length; i++)
				{
					string quotePart = quoteParts[i];
					
					if (insideQuotes)
					{
						buffer.Append(quotePart);
						insideQuotes = i + 1 >= quoteParts.Length;
						if (!insideQuotes)
						{
							result.Add(buffer.ToString());
							buffer.Clear();
						}

						prevIsQuoted = true;
						continue;
					}

					bool nextIsQuoted = i + 1 < quoteParts.Length;

					if (!quotePart.Equals(Comma))
					{
						IEnumerable<string> parts = quotePart.Split(Comma);
						if (nextIsQuoted)
							parts = parts.SkipLast(1);
						if (prevIsQuoted)
							parts = parts.Skip(1);

						result.AddRange(parts);
					}

					prevIsQuoted = false;
					insideQuotes = nextIsQuoted;
				}
			} while (insideQuotes && !stream.EndOfStream);

			return result.Empty() ? null : result.ToArray();
		}

		private static void ParseRow(
			string[] parts,
			KnowledgeDatabaseColumns.Column[] columns,
			[NotNull] KnowledgeDatabaseType type,
			[CanBeNull] KnowledgeDatabaseField field)
		{
			for (int i = 0; i < columns.Length && i < parts.Length; i++)
			{
				columns[i].SetValue(parts[i], type, field);
			}
		}

	#endregion
	}
}