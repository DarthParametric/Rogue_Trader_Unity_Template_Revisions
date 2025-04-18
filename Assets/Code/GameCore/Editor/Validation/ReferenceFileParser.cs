using System;
using System.Collections.Generic;
using System.IO;

namespace Kingmaker.Editor.Validation
{
    public class ReferenceFileParser
    {
        enum StateType
        {
            FindObject,
            FindScript,
            FindName,
            FindRefs,
            FindEntityRef,
            RefSecondLine,
        }

        private EntityRefParser m_EntityRefParser = new EntityRefParser();
        private StateType m_State;
        private string m_ScriptGuid;
        private string m_ObjectName;
        private bool m_InGameObject;
        private List<ObjRef> m_Refs = new List<ObjRef>();
        private Action m_Callback;
        private string m_Path;
        private bool m_CheckWeakResourcesLinks = false;

        public string ScriptGuid => m_ScriptGuid;

        public string ObjectName => m_ObjectName;

        public List<ObjRef> Refs => m_Refs;

        public void ParseFile(string path, Action onAssetParsed, bool checkWeakResourceLinks)
        {
            m_Path = path;
            m_CheckWeakResourcesLinks = checkWeakResourceLinks;
            m_EntityRefParser.Clear();
            using (var sr = new StreamReader(path))
            {
                m_State = StateType.FindObject;
                m_Callback = onAssetParsed;
                while (!sr.EndOfStream)
                {
                    Parse(sr.ReadLine());
                }
                if (ScriptGuid != null)
                {
                    m_Callback();
                }
            }
        }

        public void ParseFile(string path, Action onAssetParsed)
        {
            ParseFile(path, onAssetParsed, false);
        }

        private void Parse(string line)
        {
            switch (m_State)
            {
                case StateType.FindObject:
                    if (line.StartsWith("MonoBehaviour:"))
                    {
                        m_InGameObject = false;
                        m_State = StateType.FindScript;
                    }
                    if (line.StartsWith("GameObject:"))
                    {
                        m_InGameObject = true;
                        m_State = StateType.FindName;
                        m_ObjectName = null;
                    }
                    break;
                case StateType.FindName:
                    if (line.TrimStart(' ').StartsWith("m_Name:"))
                    {
                        var name = line.Substring(10);
                        if (!string.IsNullOrEmpty(name) || m_InGameObject)
                            m_ObjectName = name;
                        m_State = m_InGameObject ? StateType.FindObject : StateType.FindRefs;
                        Refs.Clear();
                    }
                    if (m_InGameObject && line.StartsWith("--- !u!"))
                    {
                        m_ObjectName = null;
                        m_State = StateType.FindObject; // did not find a name. Probably this is a prefab instance
                    }
                    break;
                case StateType.FindScript:
                    if (line.TrimStart(' ').StartsWith("m_Script:"))
                    {
                        var idx = line.IndexOf("guid:");
                        if (idx >= 0 && line.Length>=idx+32+6)
                        {
                            m_ScriptGuid = line.Substring(idx + 6, 32);
                            m_State = StateType.FindName;
                            Refs.Clear();
                        }
                        else
                        {
                            PFLog.Default.Error($"Found missing script in line [{line}] in [{m_Path}]");
                        }
                    }
                    break;
                case StateType.FindRefs:
                    if (line.StartsWith("--- !u!"))
                    {
                        m_Callback();
                        m_State = StateType.FindObject;
                        m_ScriptGuid = null;
                    }
                    else
                    {
                        var result = m_EntityRefParser.CheckLine(line, out var guid);
                        if (result != ParsRefResult.None)
                        {
                            if (result == ParsRefResult.IsRef)
                            {
                                if (!string.IsNullOrEmpty(guid))
                                {
                                    Refs.Add(new ObjRef(RefType.SceneObject, guid));
                                }
                            }
                            
                            return;
                        }

                        var idx = line.IndexOf(" guid:");
                        if (idx >= 0)
                        {
                            if (line.Length >= idx + 32 + 7)
                            {
                                var targetGuid = line.Substring(idx + 7, 32);
                                Refs.Add(new ObjRef(RefType.Asset, targetGuid));
                            }
                            else if (line.Length == idx + 6 || line.Length == idx + 7 /* a single space after the colon */)
                            {
                                // just empty bp reference
                            }
                            else
                            {
                                PFLog.Default.Error($"Found strange guid in line [{line}] in [{m_Path}]");
                            }
                        }

                        if (m_CheckWeakResourcesLinks)
                        {
                            idx = line.IndexOf("AssetId:");
                            if (idx >= 0)
                            {
                                if (line.Length >= idx + 32 + 9)
                                {
                                    Refs.Add(new ObjRef(RefType.Asset, line.Substring(idx + 9, 32)));
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    class EntityRefParser
	{
        const string GUID_FIELD = "UniqueId: ";
        const string NAME_FIELD = "EntityNameInEditor: ";
        const string SCENE_FIELD = "SceneAsset: ";

        int m_State = 0;

        public ParsRefResult CheckLine(string line, out string guid)
		{
            guid = null;
            switch (m_State)
            {
                case 0:
                    if (line.TrimStart(' ').StartsWith(SCENE_FIELD))
                    {
                        m_State++;
                        return ParsRefResult.IsRefPart;
                    }
                    break;

                case 1:
                    if (line.TrimStart(' ').StartsWith(NAME_FIELD))
                    {
                        m_State++;
                        return ParsRefResult.IsRefPart;
                    }
                    break;

                case 2:
                    var idx = line.IndexOf(GUID_FIELD);
                    if (idx != -1)
                    {
                        m_State = 0;
                        var startIndex = idx + GUID_FIELD.Length;
                        guid = startIndex < line.Length ? line.Substring(startIndex) : string.Empty;
                        return ParsRefResult.IsRef;
                    }
                    break;
            }

            m_State = 0;
            return ParsRefResult.None;
		}

        public void Clear()
		{
            m_State = 0;
		}
	}

    public struct ObjRef
	{
        public ObjRef(RefType type, string guid)
		{
            RefType = type;
            Guid = guid;
		}

        public RefType RefType { get; }
        public string Guid { get; }
    }

    public enum RefType
	{
        SceneObject,
        Asset
	}

    enum ParsRefResult
	{
        None,
        IsRefPart,
        IsRef
	}
}