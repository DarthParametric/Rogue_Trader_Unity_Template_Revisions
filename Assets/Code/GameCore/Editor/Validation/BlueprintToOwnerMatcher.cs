using System.Collections.Generic;
using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Validation
{
	public class BlueprintToOwnerMatcher
	{
		public static readonly BlueprintToOwnerMatcher Instance = new BlueprintToOwnerMatcher();

		public ReferenceGraph ReferenceGraph { get; set; }

		private Dictionary<string, string> m_SceneToDesignerCache;

		private Dictionary<string, string> m_AssetToOwnerCache;

		public string TryMatchToOwner(BlueprintScriptableObject obj)
		{
			var area = obj as BlueprintArea;
			if (area)
				return area.Author.ToString();

			// todo: check owner cache
			if (m_AssetToOwnerCache == null)
			{
				LoadOwnerCache();
			}

			string owner;
			if (m_AssetToOwnerCache.TryGetValue(obj.AssetGuid, out owner))
				return owner;

			// try to find area in references
			string s= TryMatchByReferenceGraph(obj);
			if (s!=null)
			{
				m_AssetToOwnerCache[obj.AssetGuid] = s;
				return s;
			}

			return "";
		}

		private string TryMatchByReferenceGraph(BlueprintScriptableObject obj)
		{
			var entry = ReferenceGraph?.FindEntryByGuid(obj.AssetGuid);
			if (entry != null)
			{
				foreach (var refData in entry.References)
				{
					if (refData.AssetType == nameof(BlueprintArea))
                    {
                        BlueprintArea area = BlueprintsDatabase.LoadAtPath<BlueprintArea>(refData.AssetPath);
						return area.Author.ToString();
					}
					if (refData.IsScene)
					{
						string d;
						if (m_SceneToDesignerCache.TryGetValue(Path.GetFileNameWithoutExtension(refData.AssetPath), out d))
						{
							return d;
						}
					}
				}
			}
			return null;
		}

		private void LoadOwnerCache()
		{
			m_AssetToOwnerCache = new Dictionary<string, string>();
			using (var sr = new StreamReader("ownercache.txt"))
			{
			    while (!sr.EndOfStream)
			    {
			        var l = sr.ReadLine()?.Split('=');
			        if (l?.Length == 2)
			        {
			            m_AssetToOwnerCache[l[0]] = l[1];
			        }
			    }
			}
		}

		public void SaveCache()
		{
			using (var sw = new StreamWriter("ownercache.txt"))
			{
				foreach (var pair in m_AssetToOwnerCache)
				{
					if (pair.Value != null)
					{
						sw.WriteLine($"{pair.Key}={pair.Value}");
					}
				}
			}
		}


		public void AddAreaToSceneCache(BlueprintArea area)
		{
			m_SceneToDesignerCache = m_SceneToDesignerCache ?? new Dictionary<string, string>();
			AddAreaToSceneCache(area, area.Author.ToString());
			foreach (var part in area.GetParts())
			{
				if (part.DynamicScene.IsDefined && !m_SceneToDesignerCache.ContainsKey(part.DynamicScene.SceneName))
				{
					m_SceneToDesignerCache[part.DynamicScene.SceneName] = area.Author.ToString();
				}
			}
		}
		void AddAreaToSceneCache(BlueprintArea area, string owner)
		{
			var scene = area.DynamicScene;
			{
				if (scene.IsDefined && !m_SceneToDesignerCache.ContainsKey(scene.SceneName))
				{
					m_SceneToDesignerCache[scene.SceneName] = owner;
				}
			}
		}

		public void UpdateCache(string objectGuid, string ownerName)
		{
			if (m_AssetToOwnerCache == null)
			{
				LoadOwnerCache();
			}
			m_AssetToOwnerCache[objectGuid] = ownerName;
		}
	}
}