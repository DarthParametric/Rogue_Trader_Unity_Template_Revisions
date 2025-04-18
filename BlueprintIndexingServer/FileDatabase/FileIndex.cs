using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    class FileIndex
    {
        public const int FormatVersion = 3;
        
        private readonly FileDatabase m_FileDatabase;
        private Dictionary<string, IndexEntry> m_IdToEntry = new();
        private Dictionary<string, IndexEntry> m_PathToEntry = new();
        private Dictionary<string, HashSet<IndexEntry>> m_TypeToEntry = new();
        private Dictionary<string, HashSet<string>> m_IdToDuplicatePath = new();
        private HashSet<string> m_PathsWithErrors = new();

        private Dictionary<string, HashSet<string>> m_DependsOn = new();
        private Dictionary<string, HashSet<string>> m_DependingOn = new();

        private readonly ILogger Logger;

        public FileIndex(FileDatabase fileDatabase, ILoggerFactory loggerFactory)
        {
            m_FileDatabase = fileDatabase;
            Logger = loggerFactory.CreateLogger<FileIndex>();
        }

        public DateTime LastUpdateTime { get; set; }
        

        public void AddOrUpdate(IFileData data, string path, DateTime lastWriteTime)
        {
            if (data.UniqueId?.Length != 32)
            {
                // this bp is malformed, do not even add it to the index
                Logger.LogError($"Error in file {path}: uid is {data.UniqueId}");
            }

            // todo: check if nothing is actually changed?
            var entry = new IndexEntry(data.UniqueId, data.Name, data.TypeId, path, data.IsShadowDeleted, data.UsesBlueprints);
            AddOrUpdate(entry);
        }

        public void Remove(string path)
        {
            if (m_PathToEntry.TryGetValue(path, out var e))
            {
                Remove(e);
            }
            else
            {
                // check if duplicates can be removed
                List<string>? toRemove = null;
                foreach (var p in m_IdToDuplicatePath)
                {
                    if (p.Value.Remove(path))
                    {
                        if (p.Value.Count == 1)
                        {
                            // no longer a duplicate, should reread
                            m_FileDatabase.HandleFileChange(Path.Combine(m_FileDatabase.BasePath, p.Value.First()));
                            (toRemove??=new List<string>()).Add(p.Key);
                        }

                        m_PathsWithErrors.Remove(path);
                    }
                }
                if(toRemove!=null)
                {
                    foreach (var id in toRemove)
                    {
                        m_PathsWithErrors.Remove(m_IdToDuplicatePath[id].FirstOrDefault());
                        m_IdToDuplicatePath.Remove(id);
                    }
                }
            }
        }

        public string? IdToPath(string id)
        {
            // special case: if a given id has duplicates, return all paths to signal an error
            if (m_IdToDuplicatePath.TryGetValue(id, out var paths) && paths.Count > 1)
            {
                return string.Join(';', paths);
            }

            if (m_IdToEntry.TryGetValue(id, out var entry))
                return entry.Path;
            return null;
        }

        public bool? IdToIsShadowDeleted(string id)
        {
            if (m_IdToEntry.TryGetValue(id, out var entry))
            {
                return entry.IsShadowDeleted;
            }

            return null;
        }

        public string? PathToId(string path)
        {
            if (m_PathsWithErrors.Contains(path))
            {
                Logger.LogError("Error: dupe at path "+path);
                // todo: return error id?
            }
            if (m_PathToEntry.TryGetValue(path, out var entry))
                return entry.Id;
            return null;
        }

        public string? IdToTypeId(string id)
        {
            if (m_IdToEntry.TryGetValue(id, out var entry))
                return entry.TypeId;
            return null;
        }
        
        public string? PathToTypeId(string path)
        {
            if (m_PathToEntry.TryGetValue(path, out var entry))
                return entry.TypeId;
            return null;
        }

        private void AddOrUpdate(IndexEntry data)
        {
            if (m_PathToEntry.TryGetValue(data.Path, out var prevAtPath))
            {
                // this path used to have a different file, remove it from lookup
                Remove(prevAtPath);
            }

            if (m_IdToEntry.TryGetValue(data.Id, out var prevWithId))
            {
                if(m_FileDatabase.OnDuplicateId(data, prevWithId))
                    return;
                // this id used to be in a different path
                Remove(prevWithId);
            }

            Add(data);
        }

        private void AddDepending(string id, string dependingId)
        {
            if (!m_DependingOn.ContainsKey(id))
            {
                m_DependingOn.Add(id, new HashSet<string>());
            }

            if (m_DependingOn[id].Contains(dependingId))
            {
                return;
            }

            m_DependingOn[id].Add(dependingId);
        }

        private void RemoveDepending(string id, string dependingId)
        {
            if (!m_DependingOn.ContainsKey(id))
            {
                return;
            }

            m_DependingOn[id].Remove(dependingId);

            if (m_DependingOn[id].Count <= 0)
            {
                m_DependingOn.Remove(id);
            }
        }

        private void Add(IndexEntry entry)
        {
            {
                if (entry.DependsOnBlueprintsId.Count > 0
                    && !m_DependsOn.ContainsKey(entry.Id))
                {
                    m_DependsOn.Add(entry.Id, new HashSet<string>());
                }

                var currentDepends = m_DependsOn.TryGetValue(entry.Id, out var value) ? value : null;
                var addDepends = currentDepends != null
                    ? entry.DependsOnBlueprintsId.Except(currentDepends).ToArray()
                    : entry.DependsOnBlueprintsId.ToArray();
                var removeDepends = currentDepends?.Except(entry.DependsOnBlueprintsId).ToArray();

                if (addDepends.Length > 0)
                {
                    if (!m_DependsOn.ContainsKey(entry.Id))
                    {
                        m_DependsOn.Add(entry.Id, new HashSet<string>());
                    }

                    foreach (var addDepend in addDepends)
                    {
                        if (!m_DependsOn[entry.Id].Contains(addDepend))
                        {
                            m_DependsOn[entry.Id].Add(addDepend);
                        }

                        AddDepending(addDepend, entry.Id);
                    }
                }

                if (removeDepends is { Length: > 0 })
                {
                    var depend = m_DependsOn.TryGetValue(entry.Id, out var dp) ? dp : null;
                    foreach (var removeDepend in removeDepends)
                    {
                        depend?.Remove(removeDepend);
                        RemoveDepending(removeDepend, entry.Id);
                    }

                    if (depend is { Count: <= 0 })
                    {
                        m_DependsOn.Remove(entry.Id);
                    }
                }
            }
            
            m_IdToEntry[entry.Id] = entry;
            m_PathToEntry[entry.Path] = entry;
            if (!m_TypeToEntry.TryGetValue(entry.TypeId, out var set))
            {
                m_TypeToEntry[entry.TypeId] = set = new HashSet<IndexEntry>();
            }

            set.Add(entry);

            if (m_PathsWithErrors.Contains(entry.Path))
            {
                // check if this has fixed the guid duplication problem
                var p = m_IdToDuplicatePath.FirstOrDefault(p => p.Value.Contains(entry.Path));
                if (p.Key != null)
                {
                    p.Value.Remove(entry.Path);
                    m_PathsWithErrors.Remove(entry.Path);
                    if (p.Value.Count < 2)
                    {
                        m_PathsWithErrors.ExceptWith(p.Value);
                        m_IdToDuplicatePath.Remove(p.Key);
                    }
                }
            }
        }
        

        private void Remove(IndexEntry entry)
        {
            {
                if (m_DependsOn.TryGetValue(entry.Id, out var depends))
                {
                    foreach (var depend in depends)
                    {
                        RemoveDepending(depend, entry.Id);
                    }

                    m_DependsOn.Remove(entry.Id);
                }
            }
            
            m_TypeToEntry.TryGetValue(entry.TypeId, out var list);
            list?.Remove(entry);

            m_IdToEntry.Remove(entry.Id);
            m_PathToEntry.Remove(entry.Path);
        }

        internal void SaveToFile(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fileStream))
            {
                bw.Write(FormatVersion); // format version
                bw.Write(LastUpdateTime.ToBinary());
                bw.Write(m_IdToEntry.Count);
                foreach (var entry in m_IdToEntry.Values)
                {
                    bw.Write(entry.Id);
                    bw.Write(entry.Name);
                    bw.Write(entry.TypeId);
                    bw.Write(entry.Path);
                    bw.Write(entry.IsShadowDeleted);
                    bw.Write(entry.DependsOnBlueprintsId.Count);
                    foreach (var blueprintId in entry.DependsOnBlueprintsId)
                    {
                        bw.Write(blueprintId);
                    }
                }
            }
        }

        internal void LoadFromFile(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open))
            using (var br = new BinaryReader(fileStream))
            {
                var v = br.ReadInt32();
                if (v != FormatVersion)
                {
                    throw new Exception("Wrong index file version: "+v);
                }

                LastUpdateTime = DateTime.FromBinary(br.ReadInt64());

                var count = br.ReadInt32();
                while (count-->0)
                {
                    var id = br.ReadString();
                    var name = br.ReadString();
                    var typeId = br.ReadString();
                    var entryPath = br.ReadString();
                    var isShadowDeleted = br.ReadBoolean();
                    var dpc = br.ReadInt32();
                    var dp = new HashSet<string>();
                    while (dpc-- > 0)
                    {
                        dp.Add(br.ReadString());
                    }

                    var e = new IndexEntry(id, name, typeId, entryPath, isShadowDeleted, dp);

                    if (m_IdToEntry.TryGetValue(e.Id, out var dupe))
                    {
                        StoreDuplicateGuid(e.Id, dupe.Path, e.Path);
                    }
                    else
                    {
                        Add(e);
                    }
                }
            }
        }

        public bool GetEntryByPath(string path, out IndexEntry entry)
        {
            return m_PathToEntry.TryGetValue(path, out entry);
        }

        public void RemoveAllBut(HashSet<string> found)
        {
            List<IndexEntry>? toRemove = null;
            foreach (var entry in m_IdToEntry.Values)
            {
                if (!found.Contains(entry.Path))
                {
                    toRemove ??= new List<IndexEntry>();
                    toRemove.Add(entry);
                }
            }

            if (toRemove != null)
            {
                foreach (var entry in toRemove)
                {
                    Remove(entry);
                }
            }

            if (m_PathsWithErrors.Count > 0)
            {
                var removedErrors = new HashSet<string>(m_PathsWithErrors);
                removedErrors.ExceptWith(found);
                foreach (var ep in removedErrors)
                {
                    Remove(ep);
                }
            }
        }

        public IEnumerable<IndexEntry> EnumerateAll()
        {
            return m_IdToEntry.Values;
        }

        public List<IndexEntry>? TypeIdToList(string typeId)
        {
            if (m_TypeToEntry.TryGetValue(typeId, out var list))
            {
                return list.ToList();
            }

            return null;
        }

        public void StoreDuplicateGuid(string id, string path1, string path2)
        {
            if (!m_IdToDuplicatePath.TryGetValue(id, out var paths))
            {
                m_IdToDuplicatePath[id] = paths = new HashSet<string>();
            }

            paths.Add(path1);
            paths.Add(path2);
            m_PathsWithErrors.Add(path1);
            m_PathsWithErrors.Add(path2);
        }

        public IEnumerable<string> GetDuplicatedIds()
        {
            return m_IdToDuplicatePath.Keys;
        }

        public bool? ContainsShadowDeletedBlueprints(string id)
        {
            if (m_DependsOn.TryGetValue(id, out var depends))
            {
                foreach (var depend in depends)
                {
                    if (m_IdToEntry.TryGetValue(depend, out var entry) && entry.IsShadowDeleted)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public List<string> GetAllRemoveBlueprints()
        {
            var result = new List<string>();
            foreach (var guid in m_DependingOn.Keys)
            {
                if (!m_IdToEntry.ContainsKey(guid) && !result.Contains(guid))
                {
                    result.Add(guid);
                }
            }

            return result;
        }

        public List<string> GetAllDependingOn(string guid)
        {
            return m_DependingOn.TryGetValue(guid, out var res) ? new List<string>(res) : new List<string>();
        }
    }
}