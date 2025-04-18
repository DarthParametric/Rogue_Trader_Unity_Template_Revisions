using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[FilePath("Assets/Editor/Naming/" + nameof(LocationsByChapter) + ".asset", FilePathAttribute.Location.ProjectFolder)]
	public class LocationsByChapter : ScriptableSingleton<LocationsByChapter>
	{
		[Serializable]
		public class ChapterAreas
		{
			public Chapter? Chapter;
			public Location[]? Locations;
		}

		[SerializeField]
		public ChapterAreas[]? Items;

		public IEnumerable<string>? GetLocationNames(string chapterName)
		{
			var locations = Items?
				.FirstOrDefault(item => item.Chapter != null && item.Chapter.name == chapterName)?
				.Locations;
			return locations?.Select(location => location.name);
		}
	}
}