﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UnityEngine;
using dnlib.DotNet;
using Kingmaker;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private static readonly Version VER_0 = new Version();
        private static readonly Version VER_0_13 = new Version(0, 13);

        /// <summary>
        /// Contains version of UnityEngine
        /// </summary>
        public static Version unityVersion { get; private set; }

        /// <summary>
        /// Contains version of a game, if configured [0.15.0]
        /// </summary>
        public static Version gameVersion { get; private set; } = new Version();

        /// <summary>
        /// Contains version of UMM
        /// </summary>
        public static Version version { get; private set; } = new(0, 25, 0);

        private static ModuleDefMD thisModuleDef = ModuleDefMD.Load(typeof(UnityModManager).Module);

        private static bool forbidDisableMods;

        public class Repository
        {
            [Serializable]
            public class Release : IEquatable<Release>
            {
                public string Id;
                public string Version;
                public string DownloadUrl;

                public bool Equals(Release other)
                {
                    return Id.Equals(other.Id);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj))
                    {
                        return false;
                    }
                    return obj is Release obj2 && Equals(obj2);
                }

                public override int GetHashCode()
                {
                    return Id.GetHashCode();
                }
            }

            public Release[] Releases;
        }

        public class ModSettings
        {
            public virtual void Save(ModEntry modEntry)
            {
                Save(this, modEntry);
            }

            public virtual string GetPath(ModEntry modEntry)
            {
                return Path.Combine(modEntry.Path, "Settings.xml");
            }

            public static void Save<T>(T data, ModEntry modEntry) where T : ModSettings, new()
            {
                Save<T>(data, modEntry, null);
            }

            /// <summary>
            /// [0.20.0]
            /// </summary>
            public static void Save<T>(T data, ModEntry modEntry, XmlAttributeOverrides attributes) where T : ModSettings, new()
            {
                var filepath = data.GetPath(modEntry);
                try
                {
                    using (var writer = new StreamWriter(filepath))
                    {
                        var serializer = new XmlSerializer(typeof(T), attributes);
                        serializer.Serialize(writer, data);
                    }
                }
                catch (Exception e)
                {
                    modEntry.Logger.Error($"Can't save {filepath}.");
                    modEntry.Logger.LogException(e);
                }
            }

            public static T Load<T>(ModEntry modEntry) where T : ModSettings, new()
            {
                var t = new T();
                var filepath = t.GetPath(modEntry);
                if (File.Exists(filepath))
                {
                    try
                    {
                        using (var stream = File.OpenRead(filepath))
                        {
                            var serializer = new XmlSerializer(typeof(T));
                            var result = (T)serializer.Deserialize(stream);
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        modEntry.Logger.Error($"Can't read {filepath}.");
                        modEntry.Logger.LogException(e);
                    }
                }

                return t;
            }

            public static T Load<T>(ModEntry modEntry, XmlAttributeOverrides attributes) where T : ModSettings, new()
            {
                var t = new T();
                var filepath = t.GetPath(modEntry);
                if (File.Exists(filepath))
                {
                    try
                    {
                        using (var stream = File.OpenRead(filepath))
                        {
                            var serializer = new XmlSerializer(typeof(T), attributes);
                            var result = (T)serializer.Deserialize(stream);
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        modEntry.Logger.Error($"Can't read {filepath}.");
                        modEntry.Logger.LogException(e);
                    }
                }

                return t;
            }
        }

        public class ModInfo : IEquatable<ModInfo>
        {
            public string Id;

            public string DisplayName;

            public string Author;

            public string Version;

            public string ManagerVersion;

            public string GameVersion;

            public string[] Requirements;

            public string[] LoadAfter;

            public string AssemblyName;

            public string EntryMethod;

            public string HomePage;

            public string Repository;

            /// <summary>
            /// Used for RoR2 game [0.17.0]
            /// </summary>
            [NonSerialized]
            public bool IsCheat = true;

            public static implicit operator bool(ModInfo exists)
            {
                return exists != null;
            }

            public bool Equals(ModInfo other)
            {
                return Id.Equals(other.Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                return obj is ModInfo modInfo && Equals(modInfo);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        public partial class ModEntry
        {
            public readonly ModInfo Info;

            /// <summary>
            /// Path to mod folder
            /// </summary>
            public readonly string Path;

            Assembly mAssembly = null;
            public Assembly Assembly => mAssembly;

            /// <summary>
            /// Version of a mod
            /// </summary>
            public readonly Version Version = null;

            /// <summary>
            /// Required UMM version
            /// </summary>
            public readonly Version ManagerVersion = null;

            /// <summary>
            /// Required game version [0.15.0]
            /// </summary>
            public readonly Version GameVersion = null;

            /// <summary>
            /// Not used
            /// </summary>
            public Version NewestVersion;

            /// <summary>
            /// Required mods
            /// </summary>
            public readonly Dictionary<string, Version> Requirements = new Dictionary<string, Version>();

            /// <summary>
            /// List of mods after which this mod should be loaded [0.22.5]
            /// </summary>
            public readonly List<string> LoadAfter = new List<string>();

            /// <summary>
            /// Displayed in UMM UI. Add <color></color> tag to change colors. Can be used when custom verification game version [0.15.0]
            /// </summary>
            public string CustomRequirements = String.Empty;

            public readonly ModLogger Logger = null;

            /// <summary>
            /// Not used
            /// </summary>
            public bool HasUpdate = false;

            //public ModSettings Settings = null;

            /// <summary>
            /// Show button to reload the mod [0.14.0]
            /// </summary>
            public bool CanReload { get; private set; }

            /// <summary>
            /// Called to unload old data for reloading mod [0.14.0]
            /// </summary>
            public Func<ModEntry, bool> OnUnload = null;

            /// <summary>
            /// Called to activate / deactivate the mod
            /// </summary>
            public Func<ModEntry, bool, bool> OnToggle = null;

            /// <summary>
            /// Called by MonoBehaviour.OnGUI when mod options are visible.
            /// </summary>
            public Action<ModEntry> OnGUI = null;

            /// <summary>
            /// Called by MonoBehaviour.OnGUI, always [0.21.0]
            /// </summary>
            public Action<ModEntry> OnFixedGUI = null;

            /// <summary>
            /// Called when opening mod GUI [0.16.0]
            /// </summary>
            public Action<ModEntry> OnShowGUI = null;

            /// <summary>
            /// Called when closing mod GUI [0.16.0]
            /// </summary>
            public Action<ModEntry> OnHideGUI = null;

            /// <summary>
            /// Called when the game closes
            /// </summary>
            public Action<ModEntry> OnSaveGUI = null;

            /// <summary>
            /// Called by MonoBehaviour.Update [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnUpdate = null;

            /// <summary>
            /// Called by MonoBehaviour.LateUpdate [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnLateUpdate = null;

            /// <summary>
            /// Called by MonoBehaviour.FixedUpdate [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnFixedUpdate = null;

            Dictionary<long, MethodInfo> mCache = new Dictionary<long, MethodInfo>();

            bool mStarted = false;
            public bool Started => mStarted;

            bool mErrorOnLoading = false;
            public bool ErrorOnLoading => mErrorOnLoading;

            /// <summary>
            /// UI checkbox
            /// </summary>
            public bool Enabled = true;
            //public bool Enabled => Enabled;

            /// <summary>
            /// If OnToggle exists
            /// </summary>
            public bool Toggleable => OnToggle != null;

            /// <summary>
            /// If Assembly is loaded [0.13.1]
            /// </summary>
            public bool Loaded => Assembly != null;

            bool mFirstLoading = true;
            int mReloaderCount = 0;

            bool mActive = false;
            public bool Active {
                get => mActive;
                set {
                    if (value && !Loaded)
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        Load();
                        Logger.NativeLog($"Loading time {(stopwatch.ElapsedMilliseconds / 1000f):f2} s.");
                        return;
                    }

                    if (!mStarted || mErrorOnLoading)
                        return;

                    try
                    {
                        if (value)
                        {
                            if (mActive)
                                return;

                            if (OnToggle == null || OnToggle(this, true))
                            {
                                mActive = true;
                                this.Logger.Log($"Active.");
                                // GameScripts.OnModToggle(this, true);
                            }
                            else
                            {
                                this.Logger.Log($"Unsuccessfully.");
                                this.Logger.NativeLog($"OnToggle(true) failed.");
                            }
                        }
                        else if (!forbidDisableMods)
                        {
                            if (!mActive)
                                return;

                            if (OnToggle != null && OnToggle(this, false))
                            {
                                mActive = false;
                                this.Logger.Log($"Inactive.");
                                // GameScripts.OnModToggle(this, false);
                            }
                            else if (OnToggle != null)
                            {
                                this.Logger.NativeLog($"OnToggle(false) failed.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.Logger.LogException("OnToggle", e);
                    }
                }
            }

            public ModEntry(ModInfo info, string path)
            {
                Info = info;
                Path = path;
                Logger = new ModLogger(Info.Id);
                Version = ParseVersion(info.Version);
                ManagerVersion = !string.IsNullOrEmpty(info.ManagerVersion) ? ParseVersion(info.ManagerVersion) : !string.IsNullOrEmpty(Config.MinimalManagerVersion) ? ParseVersion(Config.MinimalManagerVersion) : new Version();
                GameVersion = !string.IsNullOrEmpty(info.GameVersion) ? ParseVersion(info.GameVersion) : new Version();

                if (info.Requirements != null && info.Requirements.Length > 0)
                {
                    var regex = new Regex(@"(.*)-(\d+\.\d+\.\d+).*");
                    foreach (var id in info.Requirements)
                    {
                        var match = regex.Match(id);
                        if (match.Success)
                        {
                            Requirements.Add(match.Groups[1].Value, ParseVersion(match.Groups[2].Value));
                            continue;
                        }
                        if (!Requirements.ContainsKey(id))
                            Requirements.Add(id, null);
                    }
                }

                if (info.LoadAfter != null && info.LoadAfter.Length > 0)
                {
                    LoadAfter.AddRange(info.LoadAfter);
                }
            }

            public bool Load()
            {
                if (Loaded)
                    return !mErrorOnLoading;

                mErrorOnLoading = false;

                this.Logger.Log($"Version '{Info.Version}'. Loading.");
                if (string.IsNullOrEmpty(Info.AssemblyName))
                {
                    mErrorOnLoading = true;
                    this.Logger.Error($"{nameof(Info.AssemblyName)} is null.");
                }

                if (string.IsNullOrEmpty(Info.EntryMethod))
                {
                    mErrorOnLoading = true;
                    this.Logger.Error($"{nameof(Info.EntryMethod)} is null.");
                }

                if (!string.IsNullOrEmpty(Info.ManagerVersion))
                {
                    if (ManagerVersion > GetVersion())
                    {
                        mErrorOnLoading = true;
                        this.Logger.Error($"Mod Manager must be version '{Info.ManagerVersion}' or higher.");
                    }
                }

                if (!string.IsNullOrEmpty(Info.GameVersion))
                {
                    if (gameVersion != VER_0 && GameVersion > gameVersion)
                    {
                        mErrorOnLoading = true;
                        this.Logger.Error($"Game must be version '{Info.GameVersion}' or higher.");
                    }
                }

                if (Requirements.Count > 0)
                {
                    foreach (var item in Requirements)
                    {
                        var id = item.Key;
                        var mod = FindMod(id);
                        if (mod == null)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Error($"Required mod '{id}' missing.");
                            continue;
                        }
                        else if (item.Value != null && item.Value > mod.Version)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Error($"Required mod '{id}' must be version '{item.Value}' or higher.");
                            continue;
                        }

                        if (!mod.Active)
                        {
                            mod.Enabled = true;
                            mod.Active = true;
                            if (!mod.Active)
                                this.Logger.Log($"Required mod '{id}' inactive.");
                        }
                    }
                }

                if (LoadAfter.Count > 0)
                {
                    foreach (var id in LoadAfter)
                    {
                        var mod = FindMod(id);
                        if (mod == null)
                        {
                            this.Logger.Log($"Optional mod '{id}' not found.");
                            continue;
                        }

                        if (!mod.Active && mod.Enabled)
                        {
                            mod.Active = true;
                            if (!mod.Active)
                                this.Logger.Log($"Optional mod '{id}' enabled, but inactive.");
                        }
                    }
                }

                if (mErrorOnLoading)
                    return false;

                string assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                string pdbPath = assemblyPath.Replace(".dll", ".pdb");

                var replacedAssemblyPath = string.Empty;
                var commandArgs = Environment.GetCommandLineArgs();
                var idx = Array.IndexOf(commandArgs, $"--umm-{Info.Id}-assembly-path");
                if (idx != -1 && commandArgs.Length > idx + 1)
                {
                    replacedAssemblyPath = assemblyPath = commandArgs[idx + 1];
                }

                if (File.Exists(assemblyPath))
                {
                    if (!string.IsNullOrEmpty(replacedAssemblyPath))
                    {
                        try
                        {
                            mAssembly = Assembly.LoadFile(assemblyPath);
                            mFirstLoading = false;
                        }
                        catch (Exception exception)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Error($"Error loading file '{assemblyPath}'.");
                            this.Logger.LogException(exception);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            var assemblyCachePath = assemblyPath;
                            var pdbCachePath = pdbPath;
                            var cacheExists = false;

                            if (mFirstLoading)
                            {
                                var fi = new FileInfo(assemblyPath);
                                var hash = (ushort)((long)fi.LastWriteTimeUtc.GetHashCode() + version.GetHashCode() + ManagerVersion.GetHashCode()).GetHashCode();
                                assemblyCachePath = assemblyPath + $".{hash}.cache";
                                pdbCachePath = assemblyCachePath + ".pdb";
                                cacheExists = File.Exists(assemblyCachePath);

                                if (!cacheExists)
                                {
                                    foreach (var filepath in Directory.GetFiles(Path, "*.cache*"))
                                    {
                                        try
                                        {
                                            File.Delete(filepath);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                            }

                            if (ManagerVersion >= VER_0_13)
                            {
                                if (mFirstLoading)
                                {
                                    if (!cacheExists)
                                    {
                                        bool hasChanges = false;
                                        var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                        foreach (var item in modDef.GetAssemblyRefs())
                                        {
                                            if (item.FullName.StartsWith("0Harmony, Version=1."))
                                            {
                                                item.Name = "0Harmony-1.2";
                                                hasChanges = true;
                                            }
                                        }
                                        if (hasChanges)
                                        {
                                            modDef.Write(assemblyCachePath);
                                        }
                                        else
                                        {
                                            File.Copy(assemblyPath, assemblyCachePath, true);
                                        }
                                        if (File.Exists(pdbPath))
                                        {
                                            File.Copy(pdbPath, pdbCachePath, true);
                                        }
                                    }

                                    mAssembly = Assembly.LoadFile(assemblyCachePath);

                                    foreach (var type in mAssembly.GetTypes())
                                    {
                                        if (type.GetCustomAttributes(typeof(EnableReloadingAttribute), true).Any())
                                        {
                                            CanReload = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                    modDef.Assembly.Name += ++mReloaderCount;

                                    using (var buf = new MemoryStream())
                                    {
                                        modDef.Write(buf);
                                        if (File.Exists(pdbPath))
                                        {
                                            mAssembly = Assembly.Load(buf.ToArray(), File.ReadAllBytes(pdbPath));
                                        }
                                        else
                                        {
                                            mAssembly = Assembly.Load(buf.ToArray());
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //var asmDef = AssemblyDefinition.ReadAssembly(assemblyPath);
                                //var modDef = asmDef.MainModule;
                                //if (modDef.TryGetTypeReference("UnityModManagerNet.UnityModManager", out var typeRef))
                                //{
                                //    var managerAsmRef = new AssemblyNameReference("UnityModManager", version);
                                //    if (typeRef.Scope is AssemblyNameReference asmNameRef)
                                //    {
                                //        typeRef.Scope = managerAsmRef;
                                //        modDef.AssemblyReferences.Add(managerAsmRef);
                                //        asmDef.Write(assemblyCachePath);
                                //    }
                                //}
                                if (!cacheExists)
                                {
                                    bool hasChanges = false;
                                    var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                    foreach (var item in modDef.GetTypeRefs())
                                    {
                                        if (item.FullName == "UnityModManagerNet.UnityModManager")
                                        {
                                            item.ResolutionScope = new AssemblyRefUser(thisModuleDef.Assembly);
                                            hasChanges = true;
                                        }
                                    }
                                    foreach (var item in modDef.GetMemberRefs().Where(member => member.IsFieldRef))
                                    {
                                        if (item.Name == "modsPath" && item.Class.FullName == "UnityModManagerNet.UnityModManager")
                                        {
                                            item.Name = "OldModsPath";
                                            hasChanges = true;
                                        }
                                    }
                                    foreach (var item in modDef.GetAssemblyRefs())
                                    {
                                        if (item.FullName.StartsWith("0Harmony, Version=1."))
                                        {
                                            item.Name = "0Harmony-1.2";
                                            hasChanges = true;
                                        }
                                    }
                                    if (hasChanges)
                                    {
                                        modDef.Write(assemblyCachePath);
                                    }
                                    else
                                    {
                                        File.Copy(assemblyPath, assemblyCachePath, true);
                                    }
                                }
                                mAssembly = Assembly.LoadFile(assemblyCachePath);
                            }

                            mFirstLoading = false;
                        }
                        catch (Exception exception)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Error($"Error loading file '{assemblyPath}'.");
                            this.Logger.LogException(exception);
                            return false;
                        }
                    }
                    try
                    {
                        object[] param = new object[] { this };
                        Type[] types = new Type[] { typeof(ModEntry) };
                        if (FindMethod(Info.EntryMethod, types, false) == null)
                        {
                            param = null;
                            types = null;
                        }

                        if (!Invoke(Info.EntryMethod, out var result, param, types) || result != null && (bool)result == false)
                        {
                            mErrorOnLoading = true;
                            this.Logger.Log($"Not loaded.");
                        }
                    }
                    catch (Exception e)
                    {
                        mErrorOnLoading = true;
                        this.Logger.Log(e.ToString());
                        return false;
                    }

                    mStarted = true;

                    if (!mErrorOnLoading)
                    {
                        Active = true;
                        return true;
                    }
                }
                else
                {
                    mErrorOnLoading = true;
                    this.Logger.Error($"File '{assemblyPath}' not found.");
                }

                return false;
            }

            internal void Reload()
            {
                if (!mStarted || !CanReload)
                    return;

                if (OnSaveGUI != null)
                    OnSaveGUI.Invoke(this);

                this.Logger.Log("Reloading...");

                if (Toggleable)
                {
                    var b = forbidDisableMods;
                    forbidDisableMods = false;
                    Active = false;
                    forbidDisableMods = b;
                }
                else
                {
                    mActive = false;
                }

                try
                {
                    if (!Active && (OnUnload == null || OnUnload.Invoke(this)))
                    {
                        mCache.Clear();
                        var AccessCacheType = typeof(HarmonyLib.Traverse).Assembly.GetType("HarmonyLib.AccessCache");
                        var accessCache = typeof(HarmonyLib.Traverse).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                        string[] fields = { "declaredFields", "declaredProperties", "declaredMethods", "inheritedFields", "inheritedProperties", "inheritedMethods" };
                        foreach (var field in fields)
                        {
                            var accessCacheDict = (System.Collections.IDictionary)AccessCacheType.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(accessCache);
                            accessCacheDict.Clear();
                        }

                        var oldAssembly = Assembly;
                        mAssembly = null;
                        mStarted = false;
                        mErrorOnLoading = false;

                        OnToggle = null;
                        OnGUI = null;
                        OnFixedGUI = null;
                        OnShowGUI = null;
                        OnHideGUI = null;
                        OnSaveGUI = null;
                        OnUnload = null;
                        OnUpdate = null;
                        OnFixedUpdate = null;
                        OnLateUpdate = null;
                        CustomRequirements = null;

                        if (Load())
                        {
                            var allTypes = oldAssembly.GetTypes();
                            foreach (var type in allTypes)
                            {
                                var t = Assembly.GetType(type.FullName);
                                if (t != null)
                                {
                                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                    {
                                        if (field.GetCustomAttributes(typeof(SaveOnReloadAttribute), true).Any())
                                        {
                                            var f = t.GetField(field.Name);
                                            if (f != null)
                                            {
                                                this.Logger.Log($"Copying field '{field.DeclaringType.Name}.{field.Name}'");
                                                try
                                                {
                                                    if (field.FieldType != f.FieldType)
                                                    {
                                                        if (field.FieldType.IsEnum && f.FieldType.IsEnum)
                                                        {
                                                            f.SetValue(null, Convert.ToInt32(field.GetValue(null)));
                                                        }
                                                        else if (field.FieldType.IsClass && f.FieldType.IsClass)
                                                        {
                                                            //f.SetValue(null, Convert.ChangeType(field.GetValue(null), f.FieldType));
                                                        }
                                                        else if (field.FieldType.IsValueType && f.FieldType.IsValueType)
                                                        {
                                                            //f.SetValue(null, Convert.ChangeType(field.GetValue(null), f.FieldType));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        f.SetValue(null, field.GetValue(null));
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    this.Logger.Error(ex.ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return;
                    }
                    else if (Active)
                    {
                        this.Logger.Log("Must be deactivated.");
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error(e.ToString());
                }

                this.Logger.Log("Reloading canceled.");
            }

            public bool Invoke(string namespaceClassnameMethodname, out object result, object[] param = null, Type[] types = null)
            {
                result = null;
                try
                {
                    var methodInfo = FindMethod(namespaceClassnameMethodname, types);
                    if (methodInfo != null)
                    {
                        result = methodInfo.Invoke(null, param);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    this.Logger.Error($"Error trying to call '{namespaceClassnameMethodname}'.");
                    this.Logger.LogException(exception);
                }

                return false;
            }

            MethodInfo FindMethod(string namespaceClassnameMethodname, Type[] types, bool showLog = true)
            {
                long key = namespaceClassnameMethodname.GetHashCode();
                if (types != null)
                {
                    foreach (var val in types)
                    {
                        key += val.GetHashCode();
                    }
                }

                if (!mCache.TryGetValue(key, out var methodInfo))
                {
                    if (mAssembly != null)
                    {
                        string classString = null;
                        string methodString = null;
                        var pos = namespaceClassnameMethodname.LastIndexOf('.');
                        if (pos != -1)
                        {
                            classString = namespaceClassnameMethodname.Substring(0, pos);
                            methodString = namespaceClassnameMethodname.Substring(pos + 1);
                        }
                        else
                        {
                            if (showLog)
                                this.Logger.Error($"Function name error '{namespaceClassnameMethodname}'.");

                            goto Exit;
                        }
                        var type = mAssembly.GetType(classString);
                        if (type != null)
                        {
                            if (types == null)
                                types = new Type[0];

                            methodInfo = type.GetMethod(methodString, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
                            if (methodInfo == null)
                            {
                                if (showLog)
                                {
                                    if (types.Length > 0)
                                    {
                                        this.Logger.Log($"Method '{namespaceClassnameMethodname}[{string.Join(", ", types.Select(x => x.Name).ToArray())}]' not found.");
                                    }
                                    else
                                    {
                                        this.Logger.Log($"Method '{namespaceClassnameMethodname}' not found.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (showLog)
                                this.Logger.Error($"Class '{classString}' not found.");
                        }
                    }
                    else
                    {
                        if (showLog)
                           PFLog.UnityModManager.Error($"Can't find method '{namespaceClassnameMethodname}'. Mod '{Info.Id}' is not loaded.");
                    }

                    Exit:

                    mCache[key] = methodInfo;
                }

                return methodInfo;
            }
        }

        public static readonly List<ModEntry> ModEntries = new List<ModEntry>();

        public static string ModsPath
            => Path.Combine(Application.persistentDataPath, "UnityModManager");

        internal static Param Params { get; set; } = new Param();
        internal static GameInfo Config { get; set; } = new GameInfo();

        internal static bool started;
        internal static bool initialized;

        public static bool Initialize()
        {
            if (initialized)
                return true;

            initialized = true;
            PFLog.UnityModManager.Log($"Initialize.");
            PFLog.UnityModManager.Log($"Version: {version}.");
            try
            {
                PFLog.UnityModManager.Log($"OS: {Environment.OSVersion} {Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")}.");
                PFLog.UnityModManager.Log($"Net Framework: {Environment.Version}.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PFLog.UnityModManager.Exception(e);
            }

            unityVersion = ParseVersion(Application.unityVersion);
            PFLog.UnityModManager.Log($"Unity Engine: {unityVersion}.");

            Params = Param.Load();
            if (!Directory.Exists(ModsPath))
            {
                Directory.CreateDirectory(ModsPath);
            }

            PFLog.UnityModManager.Log($"Mods path: {ModsPath}.");
            PFLog.System.Log($"Mods path: {ModsPath}.");

            KeyBinding.Initialize();

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            return true;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            string filename = null;
            if (args.Name.StartsWith("0Harmony12"))
            {
                filename = "0Harmony12.dll";
            }
            else if (args.Name.StartsWith("0Harmony, Version=1.") || args.Name.StartsWith("0Harmony-1.2"))
            {
                filename = "0Harmony-1.2.dll";
            }
            else if (args.Name.StartsWith("0Harmony, Version=2."))
            {
                filename = "0Harmony.dll";
            }

            if (filename != null)
            {
                string filepath = Path.Combine(Path.GetDirectoryName(typeof(UnityModManager).Assembly.Location), filename);
                if (File.Exists(filepath))
                {
                    try
                    {
                        return Assembly.LoadFile(filepath);
                    }
                    catch (Exception e)
                    {
                        PFLog.UnityModManager.Error(e.ToString());
                    }
                }
                else
                {
                    Debug.LogError("No harmony found.");
                }
            }
            
             var location = args.RequestingAssembly?.Location;
             if (location == null)
                return null;
            
            var dir = Path.GetDirectoryName(location);
            var name = new AssemblyName(args.Name);
            var file = Directory.EnumerateFiles(dir, "*.dll", SearchOption.TopDirectoryOnly)
                             .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(name.Name, StringComparison.InvariantCultureIgnoreCase)); 
            if(!string.IsNullOrEmpty(file))
                return Assembly.LoadFile(file);

            return null;
        }

        /// <summary>
        /// Do not remove: called via reflection
        /// Starting point of ModManager.
        /// Should be called before any other method
        /// </summary>
        /// <param name="passedGameVersion"></param>
        public static void Start(string passedGameVersion)
        {
            try
            {
                _Start(passedGameVersion);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PFLog.UnityModManager.Exception(e);
                OpenUnityFileLog();
            }
        }

        /// <summary>
        /// Do not remove: called via reflection
        /// Should be called only after Start(string passedGameVersion)
        /// </summary>
        public static void UiFirstLaunch()
        {
            if (!initialized || UI.Instance == null)
            {
                PFLog.UnityModManager.Error("UI.Instance is null." +
                                            " Either UnityModManager start failed or you call" +
                                            " UnityModManager.UI.Instance before" +
                                            " UnityModManager.Start");   
                return;
            }
            UI.Instance.FirstLaunch();
        }

        private static void _Start(string passedGameVersion)
        {
            if (!Initialize())
            {
                PFLog.UnityModManager.Log($"Cancel start due to an error.");
                OpenUnityFileLog();
                return;
            }
            if (started)
            {
                PFLog.UnityModManager.Log($"Cancel start. Already started.");
                return;
            }
            started = true;

            ParseGameVersion(passedGameVersion);

            if (Directory.Exists(ModsPath))
            {
                PFLog.UnityModManager.Log($"Parsing mods.");

                Dictionary<string, ModEntry> mods = new Dictionary<string, ModEntry>();

                int countMods = 0;

                foreach (string dir in Directory.GetDirectories(ModsPath))
                {
                    string jsonPath = Path.Combine(dir, Config.ModInfo);
                    if (!File.Exists(Path.Combine(dir, Config.ModInfo)))
                    {
                        jsonPath = Path.Combine(dir, Config.ModInfo.ToLower());
                    }
                    if (File.Exists(jsonPath))
                    {
                        countMods++;
                        PFLog.UnityModManager.Log($"Reading file '{jsonPath}'.");
                        try
                        {
                            //ModInfo modInfo = JsonUtility.FromJson<ModInfo>(File.ReadAllText(jsonPath));
                            ModInfo modInfo = TinyJson.JSONParser.FromJson<ModInfo>(File.ReadAllText(jsonPath));
                            if (string.IsNullOrEmpty(modInfo.Id))
                            {
                                PFLog.UnityModManager.Error($"Id is null.");
                                continue;
                            }
                            if (mods.ContainsKey(modInfo.Id))
                            {
                                PFLog.UnityModManager.Error($"Id '{modInfo.Id}' already uses another mod.");
                                continue;
                            }
                            if (string.IsNullOrEmpty(modInfo.AssemblyName))
                                modInfo.AssemblyName = modInfo.Id + ".dll";

                            ModEntry modEntry = new ModEntry(modInfo, dir + Path.DirectorySeparatorChar);
                            mods.Add(modInfo.Id, modEntry);
                        }
                        catch (Exception exception)
                        {
                            PFLog.UnityModManager.Error($"Error parsing file '{jsonPath}'.");
                            Debug.LogException(exception);
                        }
                    }
                    else
                    {
                        PFLog.UnityModManager.Log($"File not found {jsonPath}");
                    }
                }
                if (mods.Count > 0)
                {
                    PFLog.UnityModManager.Log($"Sorting mods.");
                    TopoSort(mods);

                    Params.ReadModParams();

                    PFLog.UnityModManager.Log($"Loading mods.");
                    foreach (var mod in ModEntries)
                    {
                        if (!mod.Enabled)
                        {
                            mod.Logger.Log("To skip (disabled).");
                        }
                        else
                        {
                            mod.Active = true;
                        }
                    }
                }

                PFLog.UnityModManager.Log($"Finish. Successful loaded {ModEntries.Count(x => !x.ErrorOnLoading)}/{countMods} mods.".ToUpper());

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.ManifestModule.Name == "UnityModManager.dll");
                if (assemblies.Count() > 1)
                {
                    PFLog.UnityModManager.Error($"Detected extra copies of UMM.");
                    foreach (var ass in assemblies)
                    {
                        PFLog.UnityModManager.Log($"- {ass.CodeBase}");
                    }
                }
            }
            else
            {
                PFLog.UnityModManager.Log($"Directory '{ModsPath}' not exists.");
            }

            // GameScripts.OnAfterLoadMods();

            if (!UI.Load())
            {
                Debug.LogError("UMM can't load UI");
                PFLog.UnityModManager.Error($"Can't load UI.");
            }
        }

        private static void ParseGameVersion(string passedGameVersion)
        {
            #if UNITY_EDITOR
            gameVersion = new Version(1, 0, 0);
            return;
            #endif

            try
            {
                gameVersion = ParseVersion(passedGameVersion);
                //TODO: fix after release
                if (gameVersion.Major < 1)
                {
                    gameVersion = new Version(1, gameVersion.Minor, gameVersion.Build);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PFLog.UnityModManager.Exception(e, "while parsing game version");
                OpenUnityFileLog();
            }
        }
        
        private static bool GetValueFromMember(Type cls, ref object instance, string name, bool _static)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | (_static ? BindingFlags.Static : BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            var field = cls.GetField(name, flags);
            if (field != null)
            {
                instance = field.GetValue(instance);
                return true;
            }

            var property = cls.GetProperty(name, flags);
            if (property != null)
            {
                instance = property.GetValue(instance, null);
                return true;
            }

            var method = cls.GetMethod(name, flags, null, Type.EmptyTypes, null);
            if (method != null)
            {
                instance = method.Invoke(instance, null);
                return true;
            }

            PFLog.UnityModManager.Error($"Class '{cls.FullName}' does not have a {(_static ? "static" : "non-static")} member '{name}'");
            return false;
        }

        private static void DFS(string id, Dictionary<string, ModEntry> mods)
        {
            if (ModEntries.Any(m => m.Info.Id == id))
            {
                return;
            }
            foreach (var req in mods[id].Requirements.Keys)
            {
                if (mods.ContainsKey(req))
                    DFS(req, mods);
            }
            foreach (var req in mods[id].LoadAfter)
            {
                if (mods.ContainsKey(req))
                    DFS(req, mods);
            }
            ModEntries.Add(mods[id]);
        }

        private static void TopoSort(Dictionary<string, ModEntry> mods)
        {
            foreach (var id in mods.Keys)
            {
                DFS(id, mods);
            }
        }

        public static ModEntry FindMod(string id)
        {
            return ModEntries.FirstOrDefault(x => x.Info.Id == id);
        }

        public static Version GetVersion()
        {
            return version;
        }

        public static void SaveSettingsAndParams()
        {
            Params.Save();
            foreach (var mod in ModEntries)
            {
                if (mod.Active && mod.OnSaveGUI != null)
                {
                    try
                    {
                        mod.OnSaveGUI(mod);
                    }
                    catch (Exception e)
                    {
                        mod.Logger.LogException("OnSaveGUI", e);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Copies a value from an old assembly to a new one [0.14.0]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveOnReloadAttribute : Attribute
    {
    }

    /// <summary>
    /// Allows reloading [0.14.1]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EnableReloadingAttribute : Attribute
    {
    }
}
