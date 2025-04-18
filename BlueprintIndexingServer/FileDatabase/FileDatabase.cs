using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    public class FileDatabase
    {
        private class ScheduledChange
        {
            public string FullPath { get; set; }
            public bool Deleted { get; set; }
            public bool Silent { get; set; }
        }

        private readonly object m_SyncObject = new object();
        
        public readonly IFileReader Reader;
        public readonly string BasePath;
        public readonly string FilePattern;
        private string m_PathPrefix;
        private FileIndex m_Index;

        private bool m_IndexingPaused;
        private readonly List<ScheduledChange> m_ScheduledChanges = new List<ScheduledChange>();

        private readonly ILoggerFactory LoggerFactory;
        private readonly ILogger Logger;

        public FileDatabase(string basePath, string filePattern, IFileReader reader, ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger<FileDatabase>();
            Reader = reader;
            if (!Path.IsPathRooted(basePath))
            {
                basePath = new DirectoryInfo(basePath).FullName;
            }
            BasePath = basePath;
            m_PathPrefix = basePath + Path.DirectorySeparatorChar;
            FilePattern = filePattern;
        }

        public void CreateIndex()
        {
            lock (m_SyncObject)
            {
                m_Index = new FileIndex(this, LoggerFactory);
                var files = Directory.EnumerateFiles(BasePath, FilePattern, SearchOption.AllDirectories);
                foreach (var path in files)
                {
                    var data = Reader.ReadFile(path, Logger);
                    var relpath = path[m_PathPrefix.Length..]; // to relative;
                    m_Index.AddOrUpdate(data, relpath, File.GetLastWriteTimeUtc(path));
                    m_Index.LastUpdateTime = DateTime.UtcNow;
                }
            }
        }

        public void UpdateIndex()
        {
            if (m_Index == null)
            {
                return;
            }

            lock (m_SyncObject)
            {
                var files = Directory.EnumerateFiles(BasePath, FilePattern, SearchOption.AllDirectories);
                var found = new HashSet<string>();
                var n = 0;
                foreach (var path in files)
                {
                    n++;
                    var relpath = path[m_PathPrefix.Length..]; // to relative;
                    found.Add(relpath);

                    var time = File.GetLastWriteTimeUtc(path); // if we got here, last write time is already pas
                    var hasEntry = m_Index.GetEntryByPath(relpath, out var entry);
                    
                    if (hasEntry && time <= m_Index.LastUpdateTime) // file not new and not changed
                    {
                        continue;
                    }
                    
                    try
                    {
                        var data = Reader.ReadFile(path, Logger);
                        
                        if (hasEntry 
                            && entry.Name == data.Name 
                            && entry.Id == data.UniqueId 
                            && entry.TypeId == data.TypeId
                            && entry.IsShadowDeleted == data.IsShadowDeleted
                            && entry.DependsOnBlueprintsId.SetEquals(data.UsesBlueprints))
                        {
                            //                    entry.LastWriteTime = time; // file changed, but metadata is not - just update time
                            continue;
                        }

                        m_Index.AddOrUpdate(data, relpath, time);
                    }
                    catch (Exception x)
                    {
                        Logger.LogError(x, "Error reading {relpath}", relpath);
                        m_Index.Remove(relpath); // remove file if it threw an exception when read
                    }
                }

                m_Index.RemoveAllBut(found);
                m_Index.LastUpdateTime = DateTime.UtcNow;
            }
        }

        public void SaveIndex(string path)
        {
            lock (m_SyncObject)
            {
                m_Index?.SaveToFile(path);
            }
        }
        public void LoadIndex(string path)
        {
            lock (m_SyncObject)
            {
                m_Index = new FileIndex(this, LoggerFactory);
                m_Index.LoadFromFile(path);
            }
        }

        public void PauseIndexing()
        {
            m_IndexingPaused = true;
        }

        public void ResumeIndexing()
        {
            m_IndexingPaused = false;
            foreach (var change in m_ScheduledChanges)
            {
                if (change.Deleted)
                {
                    HandleFileDeleted(change.FullPath, change.Silent);
                }
                else
                {
                    HandleFileChange(change.FullPath);
                }
            }
            
            m_ScheduledChanges.Clear();
        }

        public string? IdToPath(string id)
        {
            lock (m_SyncObject)
            {
                return m_Index.IdToPath(id);
            }
        }
        
        public bool? IdToIsShadowDeleted(string id)
        {
            lock (m_SyncObject)
            {
                return m_Index.IdToIsShadowDeleted(id);
            }
        }

        public string? PathToId(string path)
        {
            lock (m_SyncObject)
            {
                return m_Index.PathToId(path);
            }
        }

        public string? IdToType(string id)
        {
            lock (m_SyncObject)
            {
                return m_Index.IdToTypeId(id);
            }
        }

        public string? PathToType(string path)
        {
            lock (m_SyncObject)
            {
                return m_Index.PathToTypeId(path);
            }
        }

        public event Action<string> OnFileChange;

        public void HandleFileChange(string fullPath)
        {
            if (m_IndexingPaused)
            {
                m_ScheduledChanges.Add(new ScheduledChange {FullPath = fullPath});
                return;
            }

            string path = GetRelativePath(ref fullPath);
            if (path == "" || !path.EndsWith(".jbp"))
                return;

            Logger.LogInformation("Change at {path}", path);

            var time = File.GetLastWriteTimeUtc(fullPath);
            try
            {
                var data = Reader.ReadFile(fullPath, Logger);
                IndexEntry entry;
                bool hasEntry;
                lock (m_SyncObject)
                {
                    hasEntry = m_Index.GetEntryByPath(path, out entry);
                    bool noChanges = m_Index.GetEntryByPath(path, out entry) 
                                     && entry.Name == data.Name 
                                     && entry.Id == data.UniqueId 
                                     && entry.TypeId == data.TypeId
                                     && entry.IsShadowDeleted == data.IsShadowDeleted
                                     && entry.DependsOnBlueprintsId.SetEquals(data.UsesBlueprints);

                    if (!noChanges)
                    {
                        m_Index.AddOrUpdate(data, path, time);
                    }
                }

                // invoke change handler even if nothing has changed in the index, b/c we want to report this to 
                // client anyway (client may have its own data to reload)
                OnFileChange?.Invoke(data.UniqueId);
                if (hasEntry && entry.Id != data.UniqueId)
                    OnFileChange?.Invoke(entry.Id);
            }
            catch (Exception x)
            {
                // failed to read file - should probably delete it from the index
                Logger.LogError(x, "Exception handling path {path}", fullPath);
                HandleFileDeleted(fullPath, false);
            }
        }

        private string GetRelativePath(ref string fullPath)
        {
            // path MAY be full and MAY be relative because fuck knows why. We can force full path with this trick, fortunately
            if (!Path.IsPathRooted(fullPath))
                fullPath = new FileInfo(fullPath).FullName;

            if (fullPath.StartsWith(m_PathPrefix))
            {
                return fullPath[m_PathPrefix.Length..];
            }
            Logger.LogWarning("Change at {fullPath}: ignoring, cannot make relative or outside of folder", fullPath);
            return "";
        }

        public void HandleFileDeleted(string fullPath, bool silent)
        {
            if (m_IndexingPaused)
            {
                m_ScheduledChanges.Add(new ScheduledChange {FullPath = fullPath, Deleted = true, Silent = silent});
                return;
            }

            string path = GetRelativePath(ref fullPath);
            if (path == "" || !path.EndsWith(".jbp"))
                return;

            Logger.LogInformation("Removed at {path}", path);

            IndexEntry entry;
            bool hasEntry;
            lock (m_SyncObject)
            {
                hasEntry = m_Index.GetEntryByPath(path, out entry);
                m_Index.Remove(path);
            }

            if (hasEntry)
            {
                if (!silent)
                    OnFileChange?.Invoke(entry.Id);
            }
        }

        public List<string> GetAllRemoveBlueprints()
        {
            lock (m_SyncObject)
            {
                return m_Index.GetAllRemoveBlueprints();
            }
        }

        public List<string> GetAllDependingOn(string guid)
        {
            lock (m_SyncObject)
            {
                return m_Index.GetAllDependingOn(guid);
            }
        }

        public List<string> SearchAllUsingShadowDeletedBlueprints()
        {
            var res = new List<string>();

            lock (m_SyncObject)
            {
                foreach (var e in m_Index.EnumerateAll())
                {
                    var b =m_Index.ContainsShadowDeletedBlueprints(e.Id);
                    if (b is true)
                    {
                        res.Add(e.Id);
                    }
                }
            }
            
            return res;
        }

        public List<string> SearchByName(List<string> nameList)
        {
            var res = new List<string>();
            if (nameList.All(s => s.Length < 3))
            {
                return res; // ignore searches that do not have a long enough substring
            }

            lock (m_SyncObject)
            {
                // simple linear search seems to work all right, around 10-20 ms for a full roundtrip
                foreach (var e in m_Index.EnumerateAll())
                {
                    bool match = true;
                    foreach (var s in nameList)
                    {
                        if (!e.Name.Contains(s, StringComparison.OrdinalIgnoreCase))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        res.Add(e.Id);
                    }
                }
            }

            return res;
        }
        public string? GetByName(string name)
        {
            lock (m_SyncObject)
            {
                // simple linear search seems to work all right, around 10-20 ms for a full roundtrip
                foreach (var e in m_Index.EnumerateAll())
                {
                    if (e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return e.Id;
                    }
                }

                return null;
            }
        }
        
        public List<string> SearchByTypeList(List<string> types)
        {
            lock (m_SyncObject)
            {
                var res = new List<string>();
                // simple linear search seems to work all right, around 10-20 ms for a full roundtrip
                foreach (var t in types)
                {
                    var entries = m_Index.TypeIdToList(t);
                    if (entries != null)
                        res.AddRange(entries.Select(e => e.Id));
                }

                return res;
            }
        }

        public void RaiseFileChange(string id)
        {
            OnFileChange?.Invoke(id);
        }

        internal bool OnDuplicateId(IndexEntry newEntry, IndexEntry prevEntry)
        {
            if (prevEntry.Path == newEntry.Path)
                return false;
            var prevPathFull = m_PathPrefix + prevEntry.Path;
            if (File.Exists(prevPathFull))
            {
                lock (m_SyncObject)
                {
                    m_Index.StoreDuplicateGuid(newEntry.Id, prevEntry.Path, newEntry.Path);
                }

                return true;
            }

            return false;
        }

        public IEnumerable<string> GetDuplicatedIds()
        {
            lock (m_SyncObject)
            {
                return m_Index.GetDuplicatedIds();
            }
        }

        public bool? ContainsShadowDeletedBlueprints(string id)
        {
            lock (m_SyncObject)
            {
                return m_Index.ContainsShadowDeletedBlueprints(id);
            }
        }
    }
}