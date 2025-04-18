using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Code.Editor.KnowledgeDatabase.Data;
using Code.GameCore.Editor.Utility;
using Kingmaker;
using Kingmaker.Editor;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Utility;
using RectEx;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Code.Editor.KnowledgeDatabase.Inspector
{
	public class KnowledgeDatabaseInspectorEntry
	{
		public KnowledgeDatabaseType Type { get;}
		public float RectHeight { get;}
		
		public bool IsExpanded { get; set; }

		public KnowledgeDatabaseInspectorEntry(KnowledgeDatabaseType type, float rectHeight, bool isExpanded)
		{
			Type = type;
			RectHeight = rectHeight;
			IsExpanded = isExpanded;
		}
	}
	
	public class KnowledgeDatabaseInspectorWindow : KingmakerWindowBase 
	{
		private enum SearchTypes
		{
			Name,
			Type,
			
			Description,
			Keywords,

			AllowedOn,
		}

		private enum KeywordsSearchType
		{
			Any,
			All
		}
		
		private enum ShowOptions
		{
			Default,
			OnlyWithTypeDescription,
			OnlyWithoutTypeDescription
		}

		private const float DefaultSize = 800f;
		
		private bool m_ExcludeObsoleteAndRemoved = true;

		private Vector2 m_ScrollTree;

		private object m_SplitterState;

		private Vector2 m_ScrollDescription;
		private Vector2 m_ScrollCodeDescription;
		private Vector2 m_ScrollFieldDescription;
		private Vector2 m_ScrollKeywords;
		
		private SearchTypes m_SearchTypes;
		private ShowOptions m_ShowOptions;
		private static KeywordsSearchType s_KeywordsSearchType = KeywordsSearchType.All;
		private static bool s_SearchInFieldsDescriptions = false;
		
		private string m_SearchInput = "";
		private string m_FieldDescriptionInput = "";
		private string m_TypeDescriptionInput = "";
		private string m_NewKeywordsInput = "";

		private KnowledgeDatabaseInspectorEntry m_CurrentlyShownEntry = null;
		private KnowledgeDatabaseField m_CurrentlyShownField = null;
		
		private readonly List<KnowledgeDatabaseInspectorEntry> m_CurrentlyFilteredEntries = new();
		private readonly HashSet<string> m_CurrentlyShownKeywords = new(); 
		private readonly HashSet<string> m_KeywordsToRemove = new();
		private int m_MaxFieldCount;

		private static Rect s_CurrentlySelectedRect = new Rect();
		private Rect m_KeywordAreaRect = new Rect();


		[MenuItem("Design/Knowledge Database/KnowledgeDatabaseInspector &#k", false, priority: 4)] //bind on shortcut k d b
		public static void ShowWindow()
		{
			Show();
		}

		private new static void Show()
		{
			if (KnowledgeDatabase.Instance == null)
			{
				PFLog.System.Error($"KnowledgeDatabase is not initialized. Maybe there's no KnowledgeDataBase.csv file found in project at path {KnowledgeDatabase.GetDatabaseFilePath()}");
				return;
			}

			var window = GetWindow<KnowledgeDatabaseInspectorWindow>("KnowledgeDatabaseInspector");
			window.minSize = new Vector2(DefaultSize, DefaultSize);
			window.ShowAuxWindow();
			window.Focus();
			window.UpdateCurrentlyFilteredTypes();
			window.m_CurrentlyShownEntry = new KnowledgeDatabaseInspectorEntry(KnowledgeDatabase.Instance.Records.First().Value, EditorGUIUtility.singleLineHeight, true);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_SplitterState = SplitterLayout.CreateSplitterState(0.3f, 0.7f);
		}
		
		protected void OnDestroy()
		{
			var window = GetWindow<KnowledgeDatabaseInspectorWindow>("KnowledgeDatabaseInspector");
			window.ResetInspectorToDefault();
		}

		protected override void OnGUI()
		{
			if (Application.isPlaying)
			{
				EditorGUILayout.HelpBox("KnowledgeDatabaseInspector doesn't work while the game is running", MessageType.Info);
				return;
			}

			if (KnowledgeDatabase.Instance == null)
			{
				EditorGUILayout.HelpBox("KnowledgeDatabaseInspector doesn't work while KnowledgeDatabase.csv is open", MessageType.Info);
				return;
			}

			using (new EditorGUILayout.VerticalScope())
			{
				DrawToolbar();

				DrawMainPart();

				DrawBottomPart(); //save
			}
		}

	#region DrawToolbar
		private void DrawToolbar()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.toolbar))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					DrawUpdateButton();
					GUILayout.Space(40);
					DrawSearchLineAndSettings();
				}
			}
		}

		private static void DrawUpdateButton()
		{
			if (GUILayout.Button(new GUIContent("Update"), GUILayout.ExpandWidth(false)))
			{
				KnowledgeDatabase.UpdateDatabase();
			}
		}

	#region Search
		private void DrawSearchLineAndSettings()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("search by:", GUILayout.Width(60f));
				m_SearchTypes = (SearchTypes)EditorGUILayout.EnumPopup(m_SearchTypes, GUILayout.Width(90f));

				DrawSearchTypesSpecialOptions();

				if (GUILayout.Button(new GUIContent("Search"), GUILayout.ExpandWidth(false)))
				{
					UpdateCurrentlyFilteredTypes();
				}
				
				m_SearchInput = GUILayout.TextArea(
					m_SearchInput, GUILayout.MinWidth(250f), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight));
				
				if (GUILayout.Button(new GUIContent("Clear"), GUILayout.ExpandWidth(false)))
				{
					m_SearchInput = "";
					UpdateCurrentlyFilteredTypes();
				}

				GUILayout.Space(20f);
					EditorGUILayout.LabelField("Show options", GUILayout.Width(80f));
					m_ShowOptions = (ShowOptions)EditorGUILayout.EnumPopup(m_ShowOptions, GUILayout.ExpandWidth(false));

				GUILayout.Space(10f);
				m_ExcludeObsoleteAndRemoved = GUILayout.Toggle(m_ExcludeObsoleteAndRemoved, "Exclude Obsolete and Removed",GUILayout.ExpandWidth(false));
			}
		}

		private void DrawSearchTypesSpecialOptions()
		{
			if (m_SearchTypes == SearchTypes.Keywords)
			{
				EditorGUILayout.LabelField("Keywords result match:", GUILayout.Width(150f));
				s_KeywordsSearchType =
					(KeywordsSearchType)EditorGUILayout.EnumPopup(s_KeywordsSearchType, GUILayout.Width(40f));
			}

			if (m_SearchTypes == SearchTypes.Description)
			{
				s_SearchInFieldsDescriptions = GUILayout.Toggle(s_SearchInFieldsDescriptions, "Search in Fields' descriptions", GUILayout.ExpandWidth(false));
			}
		}

		public void UpdateCurrentlyFilteredTypes()
		{
			ResetInspectorToDefault();

			var filteredValues = new List<KnowledgeDatabaseType>();
			foreach (var type in KnowledgeDatabase.Instance.Records.Values)
			{
				if (ShouldShow(type))
				{
					filteredValues.Add(type);
				}
			}

			foreach (var type in filteredValues)
			{
				int fieldCount = type.Fields.Count(field
					=> !m_ExcludeObsoleteAndRemoved || (!field.Value.IsObsolete && !field.Value.IsRemoved));

				if (m_MaxFieldCount < fieldCount) // TODO WH-242613 перепроверить надо ли чинить отображение 
				{
					m_MaxFieldCount = fieldCount;
				}
				
				int typeCountAndEase = 2;
				float rectHeight = (fieldCount + typeCountAndEase) * EditorGUIUtility.singleLineHeight;
				var entry = new KnowledgeDatabaseInspectorEntry(type, rectHeight, false);
				m_CurrentlyFilteredEntries.Add(entry);
			}
		}

		private void ResetInspectorToDefault()
		{
			m_MaxFieldCount = 0;
			m_CurrentlyFilteredEntries.Clear();
			s_CurrentlySelectedRect = default;
			
			m_ScrollTree = default;
			m_ScrollDescription = default;
			m_ScrollKeywords = default;
			m_ScrollCodeDescription = default;
			m_ScrollFieldDescription = default;

			m_CurrentlyShownEntry = null;
		}

		private bool ShouldShow(KnowledgeDatabaseType entry)
		{
			if (m_ExcludeObsoleteAndRemoved && (entry.IsObsolete || entry.IsRemoved))
			{
				return false;
			}

			switch (m_ShowOptions)
			{
				case ShowOptions.OnlyWithTypeDescription when entry.Description.Empty():
				case ShowOptions.OnlyWithoutTypeDescription when !entry.Description.Empty():
					return false;
			}

			if (m_SearchInput == "")
			{
				return true;
			}

			return m_SearchTypes switch
			{
				SearchTypes.Name => SearchByName(entry, m_SearchInput.Trim()),
				SearchTypes.Type => SearchByType(entry, m_SearchInput.Trim()),
				SearchTypes.Keywords => SearchByKeywords(entry, m_SearchInput.Trim()),
				SearchTypes.AllowedOn => SearchByAllowedOn(entry, m_SearchInput.Trim()),
				SearchTypes.Description => SearchByDescription(entry, m_SearchInput.Trim()),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private void UpdateSearchTypeToKeyword(string keyword)
		{
			m_SearchTypes = SearchTypes.Keywords;
			m_SearchInput = keyword;
		}

		private static bool SearchByAllowedOn(KnowledgeDatabaseType entry, string searchInput)
		{
			if (entry.AllowedOn == "")
			{
				return true;
			}

			if (entry.AllowedOn.Contains(searchInput, StringComparison.CurrentCultureIgnoreCase))
			{
				return true;
			}

			return false;
		}

		private static bool SearchByKeywords(KnowledgeDatabaseType entry, string searchInput)
		{
			if (entry.Keywords == "")
			{
				return false;
			}
			
			var keywords = searchInput.Split(",").Select(i => i.Trim()).ToHashSet();
			int timesResultOverlaps = 0;

			foreach (string keyword in keywords)
			{
				if (s_KeywordsSearchType == KeywordsSearchType.Any && entry.Keywords.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
				{
					return true; // Todo: display found words 
				}
				
				if (s_KeywordsSearchType == KeywordsSearchType.All && entry.Keywords.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
				{
					timesResultOverlaps++;
				}
			}
			
			if (s_KeywordsSearchType == KeywordsSearchType.All && keywords.Count == timesResultOverlaps)
			{
				return true;
			}
			
			return false;
		}

		private static bool SearchByType(KnowledgeDatabaseType entry, string searchInput)
		{
			return entry.Type.Contains(searchInput, StringComparison.CurrentCultureIgnoreCase);
		}

		private static bool SearchByName(KnowledgeDatabaseType entry, string searchInput)
		{
			return entry.Name.Contains(searchInput, StringComparison.CurrentCultureIgnoreCase);
		}
		
		private static bool SearchByDescription(KnowledgeDatabaseType entry, string searchInput)
		{
			if (entry.Description == "" && entry.CodeDescription == "" && !s_SearchInFieldsDescriptions)
			{
				return false;
			}
			
			return entry.Description.Contains(searchInput, StringComparison.CurrentCultureIgnoreCase)
			       || entry.CodeDescription.Contains(searchInput, StringComparison.CurrentCultureIgnoreCase)
			       || TrySearchInFieldsDescriptions(entry, searchInput);
		}

		private static bool TrySearchInFieldsDescriptions(KnowledgeDatabaseType entry, string searchInput)
		{
			if (!s_SearchInFieldsDescriptions)
			{
				return false;
			}

			foreach (var field in entry.Fields)
			{
				if (field.Value.Description.Contains(searchInput, StringComparison.CurrentCultureIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

	#endregion

	#endregion
		
	#region DrawMainPart
		private void DrawMainPart()
		{
			using (new EditorGUILayout.VerticalScope())
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					SplitterLayout.BeginVertical(m_SplitterState);
					
					// left side
					using (new EditorGUILayout.HorizontalScope(GUILayout.MinWidth(DefaultSize * 0.4f)))
					{
						using (new EditorGUILayout.VerticalScope())
						{
							EditorGUILayout.LabelField($"Search result: {m_CurrentlyFilteredEntries.Count} types");
							GUILayout.Space(10f);
							using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollTree))
							{
								m_ScrollTree = scrollScope.scrollPosition;

								DisplayKnowledgeDatabaseTree(m_ScrollTree);
							}

							GUILayout.Box(
								"", OwlcatEditorStyles.Instance.Splitter, GUILayout.ExpandHeight(true),
								GUILayout.Width(1));
						}
					}
					

					// right side
					using (new EditorGUILayout.HorizontalScope(GUILayout.MinWidth(DefaultSize * 0.6f)))
					{
						using (new EditorGUILayout.VerticalScope())
						{
							if (m_CurrentlyShownEntry == null) 
							{
								return;
							}
							
							DrawTypeDescription();

							TryDrawCodeDescription();
							TryDrawFieldDescription();

							// TODO add here link to confluence textArea

							GUILayout.FlexibleSpace();

							DrawAdditionalInfoAboutType();

							GUILayout.Space(10f);

							DrawKeywordsPart();
						}
					}

					SplitterLayout.EndVertical();
				}
			}
		}

	#region DisplayKnowledgeDatabaseTree

		private void DisplayKnowledgeDatabaseTree(Vector2 scrollTree)
		{
			float lh = EditorGUIUtility.singleLineHeight;

			int count = KnowledgeDatabase.Instance.Records.Values.Count + m_MaxFieldCount;
            KnowledgeDatabaseInspectorEntry prevType = null;

            var r = GUILayoutUtility.GetRect(0, position.width, count * lh, count * lh); 
            float y = 0;
            
            bool even = false;
            bool selected = false;
            
            foreach (var entry in m_CurrentlyFilteredEntries)
            {
	            bool isExpanded = entry.IsExpanded;
	            if (y < scrollTree.y - lh || y > position.height + scrollTree.y) 
	            {
		            y = isExpanded ? y + entry.RectHeight : y + lh;
		            if (prevType != entry)
		            {
			            prevType = entry;
			            even = !even;
			            y += lh;
		            }

		            continue;
	            }

	            var line = new Rect(r.x, y, r.width, lh);
                    
                if (prevType != entry)
                {
	                prevType = entry;
                    even = !even;
                    
					if (even)
                        GUI.Box(line, GUIContent.none);

                    DrawType(line, entry, ref selected);

                    if (selected)
                    {
	                    float newY = y > s_CurrentlySelectedRect.y ? y - s_CurrentlySelectedRect.height + 2*lh : y;
	                    line = new Rect(r.x, newY, r.width, m_CurrentlyShownEntry.RectHeight);
	                    s_CurrentlySelectedRect = line;
	                    selected = false;
	                    y += lh;
                    }
                    else
                    {
	                    y = isExpanded ? y + entry.RectHeight : y + lh;
	                    line = new Rect(r.x, y, r.width, lh);
                    }
                }
                
                if (even)
                    GUI.Box(line, GUIContent.none);
                    
                
                y += lh;
            }
            
            if (s_CurrentlySelectedRect != default)
            {
	            EditorGUI.DrawRect(s_CurrentlySelectedRect, new Color(1.0f, 1.0f, 0f, 0.1f)); 
	          
            }
            
            if (s_CurrentlySelectedRect != default && m_CurrentlyShownEntry != null)
            {
	            DrawFields(m_CurrentlyShownEntry.Type, s_CurrentlySelectedRect);
            }
		}
		
		private void DrawType(Rect r, KnowledgeDatabaseInspectorEntry entry, ref bool selected)
		{
			var rect = CutFromExtensions.SliceFromLeft(ref r, 250);
			
			if (GUI.Button(rect,new GUIContent(entry.Type.Name), EditorStyles.boldLabel))
			{
				UpdateFieldDescriptionState();
				UpdateKeywordsState();
				m_CurrentlyShownKeywords.Clear();
				UpdatePreviousSelectedTypeState(entry, out selected);

				entry.IsExpanded = true;
				m_CurrentlyShownEntry = entry;
			}
			
			GUILayout.FlexibleSpace();
			EditorGUI.LabelField(CutFromExtensions.SliceFromLeft(ref r, 250), entry.Type.Type);
		}

		private void UpdatePreviousSelectedTypeState(KnowledgeDatabaseInspectorEntry entry, out bool selected)
		{
			selected = true;
			m_CurrentlyShownEntry ??= entry;
			m_CurrentlyShownEntry.IsExpanded = false;
		}

		private void UpdateFieldDescriptionState()
		{
			if (m_CurrentlyShownField != null && !m_FieldDescriptionInput.Empty())
			{
				KnowledgeDatabaseSearch.SetDescription(
					m_CurrentlyShownEntry.Type, m_CurrentlyShownField.FieldName, m_FieldDescriptionInput);
			}

			m_CurrentlyShownField = null;
			m_FieldDescriptionInput = "";
		}
		
		private void DrawFields(KnowledgeDatabaseType type, Rect r)
		{
			using (new EditorGUILayout.VerticalScope())
			{ 
				float lh = EditorGUIUtility.singleLineHeight;
				var rect = new Rect(r.x, r.y + lh, r.width, r.height);
				
				foreach (var field in type.Fields)
				{
					if (m_ExcludeObsoleteAndRemoved && (field.Value.IsObsolete || field.Value.IsRemoved))
					{
						continue;
					}

					var fieldButtonStyle = new GUIStyle(EditorStyles.label);
					fieldButtonStyle.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
					if (GUI.Button(CutFromExtensions.SliceFromTop(ref rect, lh - 1), new GUIContent(field.Value.FieldName), fieldButtonStyle))
					{
						UpdateFieldDescriptionState();
						m_CurrentlyShownField = field.Value;
						m_FieldDescriptionInput = m_CurrentlyShownField.Description;
					}
				}
			}
		}
	#endregion

	#region DrawDescriptions

		private void DrawTypeDescription()
		{
			using var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollDescription);
			m_ScrollDescription = scrollScope.scrollPosition;
				
			string description = KnowledgeDatabase.Instance.Records[m_CurrentlyShownEntry.Type.Guid].Description;
			if (description == null)
			{
				GUILayout.Label("Try to update database (Designers/Knowledge Database/Update)");
				GUILayout.Label("If update doesn't help then current pair of (Type, Field name) isn't supported yet");
				return;
			}
			EditorGUILayout.LabelField($"Type Description of {m_CurrentlyShownEntry.Type.Name}");
			string newDescription = GUILayout.TextArea(description, GUILayout.MinHeight(250));
			KnowledgeDatabaseSearch.SetDescription(m_CurrentlyShownEntry.Type, null, newDescription);
			
		}

		private void TryDrawCodeDescription()
		{
			using var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollCodeDescription);
			m_ScrollCodeDescription = scrollScope.scrollPosition;
				
			string codeDescription = KnowledgeDatabase.Instance.Records[m_CurrentlyShownEntry.Type.Guid].CodeDescription;
			if (codeDescription == "")
			{
				return;
			}

			using (new EditorGUI.DisabledGroupScope(true))
			{
				EditorGUILayout.LabelField("Type Programmer's remarks: ");
				GUILayout.TextArea(codeDescription, GUILayout.MinHeight(50));
			}
		}
		
		private void TryDrawFieldDescription()
		{
			using var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollFieldDescription);
			m_ScrollFieldDescription = scrollScope.scrollPosition;
				
			if (m_CurrentlyShownField == null)
			{
				return;
			}
			
			var field = KnowledgeDatabaseSearch.GetFieldRecord(m_CurrentlyShownEntry.Type, m_CurrentlyShownField.FieldName);
			
			if (field == null)
			{
				return;
			}
			
			string description = field.Description;
			EditorGUILayout.LabelField($"Field Description of {m_CurrentlyShownField.FieldName}");

			string newDescription = GUILayout.TextArea(description, GUILayout.MinHeight(50));
			KnowledgeDatabaseSearch.SetDescription(m_CurrentlyShownEntry.Type, m_CurrentlyShownField.FieldName, newDescription);

			TryDrawFieldCodeDescription();
		}

		private void TryDrawFieldCodeDescription()
		{
			if (m_CurrentlyShownField.CodeDescription == "")
			{
				return;
			}

			using (new EditorGUI.DisabledGroupScope(true))
			{
				EditorGUILayout.LabelField($"Field Programmer's remarks: {m_CurrentlyShownField.FieldName}");
				GUILayout.TextArea(m_CurrentlyShownField.CodeDescription, GUILayout.MinHeight(50));
			}
		}

	#endregion
		
		private void DrawAdditionalInfoAboutType()
		{
			StringBuilder indentCollection = new();

			if (m_CurrentlyShownEntry.Type.IsObsolete)
			{
				indentCollection.Append("[Obsolete] ");
			}
			
			if(m_CurrentlyShownEntry.Type.IsRemoved)
			{
				indentCollection.Append("[Removed] ");
			}
			
			if(m_CurrentlyShownEntry.Type.HasRuntime)
			{
				indentCollection.Append("[HasRuntime] ");
			}
			if(!m_CurrentlyShownEntry.Type.AllowedOn.Empty())
			{
				indentCollection.Append($"[AllowedOn: {m_CurrentlyShownEntry.Type.AllowedOn}] ");
			}

			if (indentCollection.Length < 1)
			{
				return;
			}
			
			GUILayout.Label("Additional information: " + indentCollection.ToString(), EditorStyles.boldLabel);
		}

	#region DrawKeywords

		private void DrawKeywordsPart()
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
			{
				DrawKeywords(); 

				using (new EditorGUILayout.VerticalScope())
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						m_NewKeywordsInput = GUILayout.TextArea(m_NewKeywordsInput,
							GUILayout.MinHeight(EditorGUIUtility.singleLineHeight));
						
						if (GUILayout.Button(new GUIContent("Add new keywords"),
							    GUILayout.ExpandWidth(false)))
						{
							AddKeywordsToHashset();
							UpdateDisplayedKeywords();
							UpdateKeywordsState();
						}
					}
				}
			}
		}
		
		private void UpdateKeywordsState()
		{
			m_KeywordsToRemove.Clear(); 
		}

	#region DrawCurrentKeywords

		private void DrawKeywords()
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
			{
				if (m_CurrentlyShownEntry == null)
				{
					return;
				}
				
				var labelStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter};
				EditorGUILayout.LabelField($"{m_CurrentlyShownEntry.Type.Name} keywords:", labelStyle);

				if (KnowledgeDatabaseSearch.GetKeywords(m_CurrentlyShownEntry.Type).Empty())
				{
					EditorGUILayout.LabelField($"There're none at the moment", EditorStyles.centeredGreyMiniLabel);
					EditorGUILayout.LabelField($"But if you'd added it, that would help a lot!",
						EditorStyles.centeredGreyMiniLabel);
					return;
				}
				if(GUILayout.Button("Remove all keywords"))
				{
					m_KeywordsToRemove.AddRange(m_CurrentlyShownKeywords);
					UpdateDisplayedKeywords();
				}

				UpdateDisplayedKeywords();
			}
		}
		
		private void UpdateDisplayedKeywords()
		{
			if (!m_KeywordsToRemove.Empty())
			{
				foreach (string keyword in m_KeywordsToRemove)
				{
					m_CurrentlyShownKeywords.Remove(keyword);
				}
				KnowledgeDatabaseSearch.AddKeywords(m_CurrentlyShownEntry.Type, string.Join(",", m_CurrentlyShownKeywords));
				m_KeywordsToRemove.Clear();
			}
			
			m_CurrentlyShownKeywords.Clear();
			foreach (string keyword in KnowledgeDatabaseSearch.GetKeywords(m_CurrentlyShownEntry.Type).Split(","))
			{
				m_CurrentlyShownKeywords.Add(keyword.Trim());
			}

			using (new EditorGUILayout.VerticalScope(GUILayout.MaxHeight(300)))
			{
				using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollKeywords))
				{
					m_ScrollKeywords = scrollScope.scrollPosition;

					float lineHeight = EditorGUIUtility.singleLineHeight;
					
					const float splitterSize = 1f;
					const float rowNumbers = 16f;
					float keywordsBoxHeight = lineHeight * rowNumbers;
					float keywordsBoxSize = DefaultSize;
					
					var rect = GUILayoutUtility.GetRect(0 + splitterSize, keywordsBoxSize, 0, keywordsBoxHeight); 
                                                                           
					if (Event.current.type == EventType.Repaint)
						m_KeywordAreaRect = rect;

					Rect[] currentLine = m_KeywordAreaRect.CutFromLeft(splitterSize)[1].CutFromTop(lineHeight);
					Rect currentRect = currentLine[0].CutFromLeft(splitterSize)[0];
					
					Vector2 removeButtonSize = GUI.skin.box.CalcSize(new GUIContent($"  [Del]"));
					
					foreach (string keyword in m_CurrentlyShownKeywords)
					{
						Vector2 keywordSize = GUI.skin.box.CalcSize(new GUIContent($"  {keyword}"));
						var twoPartRect = currentRect.CutFromLeft(keywordSize.x + removeButtonSize.x);

						currentRect = twoPartRect[0];
						
						if (currentRect.xMax > keywordsBoxSize) //next line
						{
							currentLine = currentLine[1].CutFromTop(lineHeight);
							var nextLineRects = currentLine[0].CutFromLeft(keywordSize.x + removeButtonSize.x);
							currentRect = nextLineRects[0];
							
							DrawSingleKeyword(keyword, currentRect, removeButtonSize);
							currentRect = nextLineRects[1];
							continue;
						}

						DrawSingleKeyword(keyword, currentRect, removeButtonSize); //add to current line
						currentRect = twoPartRect[1];
					}
				}
			}
		}
		
		private void DrawSingleKeyword(string keyword, Rect currentRect, Vector2 removeButtonSize)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				GUI.Box(currentRect, "");
				var twoPartRect = currentRect.CutFromRight(removeButtonSize.x);
				
				var keywordButtonStyle = new GUIStyle(EditorStyles.label) { hover = {textColor = new Color(1f, 1f, 0.4f)} };

				if (GUI.Button(twoPartRect[0], $"{keyword}", keywordButtonStyle))
				{
					UpdateSearchTypeToKeyword(keyword);
				}

				var removeButtonStyle = new GUIStyle(EditorStyles.label) { normal = {textColor = new Color(0.4f, 0.4f, 0.4f)} };
				
				if (GUI.Button(twoPartRect[1], "Del", removeButtonStyle))
				{
					m_KeywordsToRemove.Add(keyword);
				}
			}
		}

	#endregion

	#region AddNewKeywords

		private void AddKeywordsToHashset()
		{
			if (m_NewKeywordsInput != null && !m_NewKeywordsInput.Empty())
			{
				string[] keywords = m_NewKeywordsInput.Split(",");
				foreach (string keyword in keywords)
				{
					bool success = m_CurrentlyShownKeywords.Add(keyword.ToLower().Trim());
					if (!success) // todo: not showing up
					{
						EditorGUILayout.HelpBox($"This type already has keyword [{keyword}]", MessageType.Info);
					}
				}
			}
			KnowledgeDatabaseSearch.AddKeywords(m_CurrentlyShownEntry.Type, string.Join(",", m_CurrentlyShownKeywords));
		}

	#endregion
	#endregion
	#endregion
		
	#region Bottom (Save)
		private void DrawBottomPart()
		{
			using (new EditorGUILayout.VerticalScope())
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("SAVE"))
					{
						KnowledgeDatabase.Save();
					}
				}
			}
		}
	#endregion
		
	}
}
