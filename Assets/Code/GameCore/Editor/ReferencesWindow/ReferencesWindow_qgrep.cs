using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
// using UnityEditor.Build.Pipeline;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.ReferencesWindow
{
	public partial class ReferencesWindow
	{
		private bool m_QgrepExists;
		private bool m_ProjectExists;
		private bool m_IndexExists;
		private DateTime m_IndexTimeStamp;
		string m_ResultOutput = string.Empty;

		internal static readonly string BasePath = "qgrep";
		private QGrepThread m_QgrepThread;
		private string m_LastLineFromQGrep;
		private string m_ErrorFromQGrep;
		private int m_Progress = -1;
		private DateTime m_LastFileCheck = DateTime.MinValue;
		private readonly HashSet<string> m_FoundPaths = new();
		private readonly HashSet<(string targetGuid, string foundPath)> m_FoundMultiPaths = new();
		private bool m_NeedToLoadRefs;

		private readonly HashSet<string> m_AllFilesGuids = new HashSet<string>();
		
		//crutches
		private string[] m_SavedArgs;
		private int m_ArgCurrentIndex = 0;
		private Action<(string targetGuid, string grepResult)> m_SavedMultiSearchLineCallback;
		private Action m_SavedMultiSearchCompleteCallback;
		private bool m_Abort = false; //TODO: need to replace by CancellationToken

		void CheckGrepSettings()
		{
			if ((DateTime.UtcNow - m_LastFileCheck).TotalSeconds < 1)
				return;

			//using (new CodeTimer("CheckGrepSettings"))
			{
				m_LastFileCheck = DateTime.UtcNow;

				m_QgrepExists = File.Exists(Path.Combine(BasePath, "qgrep.exe"));
				if (!m_QgrepExists)
					return;

				m_ProjectExists = File.Exists(Path.Combine(BasePath, "assets.cfg"));
				m_IndexExists = File.Exists(Path.Combine(BasePath, "assets.qgd"));

				if (m_IndexExists)
				{
					m_IndexTimeStamp = File.GetLastWriteTime(Path.Combine(BasePath, "assets.qgd"));
				}
			}
		}

		void DrawGrepGUI()
		{
			if (m_NeedToLoadRefs)
			{
				m_References.Clear();
				m_MultiReferences.Clear();
				if(!m_SeparateSearch)
				{
					IEnumerable<UnityEngine.Object> objects = m_FoundPaths
						.Select(p =>
						{
							if (p.EndsWith(".jbp"))
							{
								//string pathRelative = BlueprintsDatabase.FullToRelativePath(p).Replace('/', '\\');
								string pathRelative = p.Replace('/', '\\');
								SimpleBlueprint simpleBlueprint =
									BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(pathRelative);
								ScriptableObject scriptableObject = BlueprintEditorWrapper.Wrap(simpleBlueprint);
								return scriptableObject;
							}
							else
							{
								return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
							}
						})
						.Valid()
						.Distinct();
					m_References.AddRange(objects);
				}
				else
				{
					//multi-target search
					IEnumerable<(UnityEngine.Object target, UnityEngine.Object result)> objects = m_FoundMultiPaths
						.Where(tuple => !string.IsNullOrEmpty(tuple.foundPath) && tuple.foundPath != AssetDatabase.GUIDToAssetPath(tuple.targetGuid))
						.Select(tuple =>
						{
							var target =
								AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
									AssetDatabase.GUIDToAssetPath(tuple.targetGuid));
							if (tuple.foundPath.EndsWith(".jbp"))
							{
								//string pathRelative = BlueprintsDatabase.FullToRelativePath(p).Replace('/', '\\');
								string pathRelative = tuple.foundPath.Replace('/', '\\');
								SimpleBlueprint simpleBlueprint =
									BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(pathRelative);
								ScriptableObject scriptableObject = BlueprintEditorWrapper.Wrap(simpleBlueprint);

								return (target, scriptableObject);
							}
							else
							{
								return (target, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tuple.foundPath));
							}
						});
						// .Valid() //TODO: need other way to make sure null is empty?
						// .Distinct(); //HashSet makes it unique anyway
					m_MultiReferences.AddRange(objects);
				}
				m_NeedToLoadRefs = false;
			}

			CheckGrepSettings();

			bool isHaveError = !string.IsNullOrEmpty(m_ErrorFromQGrep);
			var hasQgrepInProgress = m_QgrepThread?.IsRunning ?? false;
			if (hasQgrepInProgress)
			{
				GUILayout.Label("QGrep process in progress:");
				GUILayout.Label(m_LastLineFromQGrep);

				if (m_Progress >= 0)
				{
					var r = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.box, GUILayout.Height(2), GUILayout.ExpandWidth(true));
					r.y -= 2;
					EditorGUI.DrawRect(r, Color.white);
					r.width = r.width * m_Progress / 100f;
					EditorGUI.DrawRect(r, Color.green);
				}
				if (m_FoundPaths.Count > 0)
				{
					GUILayout.Label($"Found {m_FoundPaths.Count} lines");
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Kill", GUILayout.ExpandWidth(false)))
					{
						m_Abort = true;
						m_QgrepThread.Kill();
					}
				}
			}
			else if (!string.IsNullOrEmpty(m_ResultOutput) || !string.IsNullOrEmpty(m_ErrorFromQGrep))
			{
				EditorGUILayout.BeginVertical("Box");
				if (!string.IsNullOrEmpty(m_ResultOutput))
				{
					GUILayout.Label(m_ResultOutput);
				}
				if (!string.IsNullOrEmpty(m_ErrorFromQGrep))
				{
					GUILayout.Label(m_ErrorFromQGrep);
				}
				EditorGUILayout.EndVertical();
			}

			if (!m_QgrepExists)
			{
				EditorGUILayout.HelpBox("qgrep.exe not found", MessageType.Error, true);
				return;
			}

			if (!m_ProjectExists)
			{
				EditorGUILayout.HelpBox("qgrep project not found", MessageType.Error, true);
				if (!hasQgrepInProgress && GUILayout.Button("Create project file"))
				{
					File.WriteAllText(Path.Combine(BasePath, "assets.cfg"),
					@"
					group
						path ../Assets

						include \.asset$
						include \.prefab$
						include \.unity$

						exclude LightingData\.asset$
						exclude Foliage\sData\.asset$
						exclude NavmeshMasks\.asset$
						exclude Foliage(Low)?\.unity$
					endgroup

					group
						path ../Blueprints
	
						include \.jbp$
					endgroup
					");
				}
			}

			if (!m_IndexExists)
			{
				EditorGUILayout.HelpBox("Index not found. Update index to start searching", MessageType.Error, true);
			}
			else
			{
				var old = (DateTime.Now - m_IndexTimeStamp);
				GUILayout.Label($"Last index update time: {m_IndexTimeStamp:s} ({old.TotalHours:###}:{old.Minutes:00} ago)");
			}
			if (!hasQgrepInProgress && m_ProjectExists)
			{
				if (!isHaveError && GUILayout.Button("Update Index"))
					StartProcess("update ./assets.cfg", OnLineReadUpdate, OnTaskComplete);
				else if (GUILayout.Button("Force Rebuild (if update not work)"))
					StartProcess("build ./assets.cfg", OnLineReadUpdate, OnTaskComplete);
			}
		}

		void StartProcess(string args, Action<string> lineUpdate, Action onComplete)
		{
			m_ErrorFromQGrep = string.Empty;
			m_ResultOutput = string.Empty;
			m_LastLineFromQGrep = $"Invoke {args}";
			Repaint();
			m_QgrepThread = QGrepThread.Start(
				args, 
				lineUpdate,
				OnError, 
				onComplete);
		}
		
		void StartMultiProcess(string[] args, Action<(string targetGuid, string grepResult)> argLineUpdate, Action onComplete)
		{
			m_Abort = false;
			m_ErrorFromQGrep = string.Empty;
			m_ResultOutput = string.Empty;
			Repaint();

			m_SavedArgs = args;

			//after every search iteration we should start next iteration
			//every QGrepThread will call lineCallback several times, on every found result
			//and when it finishes search - it will call finishCallback
			//line callback will contain whole found result, it should be wrapped with it's target search and send to argLineUpdate
			//finishCallback should start next item search until there is none left

			//this was clever but didn't work - will come to this later again
			// IEnumerator enumerator = args.GetEnumerator();
			// enumerator.MoveNext();//need to move it first
			
			//will use dumber approach with crutches
			m_ArgCurrentIndex = 0;
			m_SavedMultiSearchLineCallback = argLineUpdate;
			m_SavedMultiSearchCompleteCallback = onComplete;

			m_LastLineFromQGrep = $"Invoke {args[m_ArgCurrentIndex]}";
			//overcomplicated recursive call with possible stack overflow
			//TODO!!!! need to launch them one by one but without capturing all previous calls!
			//NOTE!!! DO NOT LAUNCH THEM ALL AT ONCE, THEY WILL MURDER I/O! GREP IS NOT PARALLEL DB!
			/*m_QgrepThread = QGrepThread.Start(
				(string)enumerator.Current,
				result => argLineUpdate.Invoke(((string)enumerator.Current, result)),
				OnError,
				() => OnSingleSearchComplete(enumerator, argLineUpdate, onComplete)
			);*/
			m_QgrepThread = QGrepThread.Start(
				MakeSearchQuery(m_SavedArgs[m_ArgCurrentIndex]),
				OnMultiSearchLineFound,
				OnError,
				OnMultiSearchIterationComplete
			);
		}

		private string MakeSearchQuery(string guid)
			=> $"search ./assets.cfg \"{guid}\"";

		private void OnMultiSearchIterationComplete()
		{
			PFLog.Default.Log("Iteration {0} of {1} complete", m_ArgCurrentIndex+1, m_SavedArgs.Length);
			if (!m_Abort && m_ArgCurrentIndex < m_SavedArgs.Length - 1)
			{
				m_ArgCurrentIndex++;
				PFLog.Default.Log("Starting Iteration {0} of {1}", m_ArgCurrentIndex+1, m_SavedArgs.Length);
				m_QgrepThread = QGrepThread.Start(
					MakeSearchQuery(m_SavedArgs[m_ArgCurrentIndex]),
					OnMultiSearchLineFound,
					OnError,
					OnMultiSearchIterationComplete
				);
				return;
			}

			PFLog.Default.Log("All Iterations complete, calling back");
			m_SavedMultiSearchCompleteCallback?.Invoke();
		}

		void OnMultiSearchLineFound(string result)
		{
			m_SavedMultiSearchLineCallback?.Invoke((m_SavedArgs[m_ArgCurrentIndex], result));
		}

		void OnMultiSearchIterationComplete(IEnumerator targetEnumerator, Action<(string targetGuid, string grepResult)> lineUpdate, Action onComplete)
		{
			if (targetEnumerator.MoveNext())
			{
				m_QgrepThread = QGrepThread.Start(
					(string)targetEnumerator.Current,
					result => lineUpdate.Invoke(((string)targetEnumerator.Current, result)),
					OnError,
					() => OnMultiSearchIterationComplete(targetEnumerator, lineUpdate, onComplete)
				);
			}
			else
			{
				onComplete?.Invoke();
			}
		}

		void OnTaskComplete()
		{
			if (!string.IsNullOrEmpty(m_LastLineFromQGrep))
				m_ResultOutput = $"Result: {m_LastLineFromQGrep}";
			Repaint();
		}

		private void OnLineReadUpdate(string data)
		{
			if (string.IsNullOrEmpty(data))
				return;

			if (data[0] == '[')
			{
				m_Progress = int.Parse(data.Substring(1, 3).TrimStart(' '));
			}
			else
			{
				m_Progress = -1;
			}
			m_LastLineFromQGrep = data;
			EditorApplication.delayCall+=Repaint;
		}

		private void FindReferencesQgrep()
		{
			if (m_Target)
			{
				string guid;
				// Blueprint
				if (m_Target is BlueprintEditorWrapper)
				{
					guid = ((BlueprintEditorWrapper)m_Target).Blueprint.AssetGuid;
				}
				// Asset. Folders have guids too!
				else if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_Target, out guid, out long _))
				{
					PFLog.Default.Error($"Cannot find guid for {m_Target}: not a blueprint and not an asset?");
					return;
				}

				//might be folder too
				//TODO: refactor
				//NOTE: multi-threading QGrep query does not accelerate the search due to immediate I/O bottleneck
				var path = AssetDatabase.GetAssetPath(m_Target);
				if (AssetDatabase.IsValidFolder(path)) //isFolder. Bit safer than Directory.Exists
				{
					var fileNames = CollectFilesInDir(path, m_IncludeSubDirs);
					m_AllFilesGuids.Clear();
					//surprisingly, we can use full paths from System.IO for AssetDatabase functions
					foreach (string fileEntry in fileNames)
					{
						if (AssetDatabase.IsValidFolder(fileEntry))
						{
							continue;
						}

						var fileGuid = AssetDatabase.AssetPathToGUID(fileEntry); //will be empty if the file is not asset, e.g. *.meta
						if (string.IsNullOrEmpty(fileGuid))
						{
							continue;
						}

						m_AllFilesGuids.Add(fileGuid); //guids SHOULD be unique!
					}

					m_TargetString = string.Join("|", m_AllFilesGuids);
				}
				else
				{
					m_TargetString = guid;
				}
			}

			if (m_AllFilesGuids.Count > 1 & m_SeparateSearch)
			{
				m_FoundMultiPaths.Clear();
				// var args = m_AllFilesGuids.Select(guid => $"search./ assets.cfg \"{guid}\"").ToArray();
				var args = m_AllFilesGuids.ToArray();
				
				foreach (var (arg, index) in args.WithIndex())
				{
					PFLog.Default.Log($"{index}: {arg}");
				}
				StartMultiProcess(args, OnMultiLineReadSearch, OnQgrepSearchEnded);
			}
			else
			{
				m_FoundPaths.Clear();
				if(string.IsNullOrEmpty(m_TargetString))
				{
					return;
				}
				StartProcess($"search ./assets.cfg \"{m_TargetString}\"", OnLineReadSearch, OnQgrepSearchEnded);
				PFLog.Default.Log($"search ./assets.cfg \"{m_TargetString}");
			}
			Repaint();
		}

		private static List<string> CollectFilesInDir(string targetDirectory, bool includeSubdirs)
		{
			List<string> result = new List<string>();
			// Process the list of files found in the directory.
			string[] fileEntries = Directory.GetFiles(targetDirectory);
			foreach (string fileName in fileEntries)
			{
				result.Add(fileName);
			};

			// Recurse into subdirectories of this directory.
			if(includeSubdirs)
			{
				string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
				foreach (string subdirectory in subdirectoryEntries)
				{
					result.AddRange(CollectFilesInDir(subdirectory, includeSubdirs));
				}
			}

			return result;
		}

		private void OnQgrepSearchEnded()
		{
			PFLog.Default.Log("Finished");
			m_NeedToLoadRefs = true; 
			OnTaskComplete();
		}

		private void OnMultiLineReadSearch((string targetGuid, string resultRawData) data)
		{
			if (string.IsNullOrEmpty(data.resultRawData))
				return;

			PFLog.Default.Log("data: {0} = {1}", data.targetGuid, data.resultRawData);
			string resultPath = data.resultRawData[(Directory.GetCurrentDirectory().Length + 1)..data.resultRawData.IndexOf(":", 3, StringComparison.Ordinal)];
			
			if (resultPath == "")
			{
				PFLog.Default.Error("Could not extract path");
			}

			m_FoundMultiPaths.Add((data.targetGuid, resultPath));
			m_LastLineFromQGrep = $"Found {m_FoundMultiPaths.Count} lines";

			EditorApplication.delayCall += Repaint;
		}

		private void OnLineReadSearch(string data)
		{
			if (string.IsNullOrEmpty(data))
				return;

			PFLog.Default.Log("{0}", data);

			string path = data[(Directory.GetCurrentDirectory().Length + 1)..data.IndexOf(":", 3, StringComparison.Ordinal)];
			
			if (path == "")
			{
				PFLog.Default.Error("Could not extract path");
			}

			m_FoundPaths.Add(path);
			m_LastLineFromQGrep = $"Found {m_FoundPaths.Count} lines";

			EditorApplication.delayCall += Repaint;
		}

		void OnError(string mess)
			=> m_ErrorFromQGrep = $"Thread Error: {mess}\nPlease use Force Rebuild for fix it";

        
		public static List<string> FindReferences(string to)
		{
			List<string> results = new List<string>();
			bool done = false;
			var proc = QGrepThread.Start("search ./assets.cfg \"" + to,
				s =>
				{
					var path = s.Substring(0, s.IndexOf(":", 3, StringComparison.Ordinal))
						.Substring(Directory.GetCurrentDirectory().Length + 1);
					results.Add(path);
				}, s => {}, () => done = true);
			while (!done)
			{
				Thread.Sleep(100);
			}
			return results;
		}

		// Distinct() for Tuples
		// private class SameTuplesComparer<T1, T2> : EqualityComparer<Tuple<T1, T2>>
		// {
		// 	public override bool Equals(Tuple<T1, T2> t1, Tuple<T1, T2> t2)
		// 	{
		// 		return t1.Item1.Equals(t2.Item1) && t1.Item2.Equals(t2.Item2);
		// 	}
		//
		//
		// 	public override int GetHashCode(Tuple<T1, T2> t)
		// 	{
		// 		return base.GetHashCode();
		// 	}
		// }
	}
}