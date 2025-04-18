using Kingmaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public sealed class Param
        {
            [Serializable]
            public class Mod
            {
                [XmlAttribute]
                public string Id;
                [XmlAttribute]
                public bool Enabled = true;
            }

            public static KeyBinding DefaultHotkey = new KeyBinding { keyCode = KeyCode.F10, modifiers = 1 };
            public static KeyBinding EscapeHotkey = new KeyBinding { keyCode = KeyCode.Escape };
            public KeyBinding Hotkey = new KeyBinding();
            public int CheckUpdates = 1;
            public int ShowOnStart = 1;
            public float WindowWidth;
            public float WindowHeight;
            public float UIScale = 1f;
            public string UIFont = null;

            public List<Mod> ModParams = new List<Mod>();

            static readonly string filepath = Path.Combine(Application.persistentDataPath, "UnityModManager", "Params.xml");

            public void Save()
            {
                try
                {
                    ModParams.Clear();
                    foreach (var mod in ModEntries)
                    {
                        ModParams.Add(new Mod { Id = mod.Info.Id, Enabled = mod.Enabled });
                    }
                    using (var writer = new StreamWriter(filepath))
                    {
                        var serializer = new XmlSerializer(typeof(Param));
                        serializer.Serialize(writer, this);
                    }
                }
                catch (Exception e)
                {
                    PFLog.UnityModManager.Error($"Can't write file '{filepath}'.");
                    Debug.LogException(e);
                }
            }

            public static Param Load()
            {
                if (File.Exists(filepath))
                {
                    try
                    {
                        using (var stream = File.OpenRead(filepath))
                        {
                            var serializer = new XmlSerializer(typeof(Param));
                            var result = serializer.Deserialize(stream) as Param;
                            
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        PFLog.UnityModManager.Error($"Can't read file '{filepath}'.");
                        Debug.LogException(e);
                    }
                }
                return new Param();
            }

            internal void ReadModParams()
            {
                foreach (var item in ModParams)
                {
                    var mod = FindMod(item.Id);
                    if (mod != null)
                    {
                        mod.Enabled = item.Enabled;
                    }
                }
            }
        }

        public class GameInfo
        {
            public string Name;
            public string ModsDirectory;
            public string ModInfo;
            public string MinimalManagerVersion;

            public GameInfo()
            {
                MinimalManagerVersion = "0.22.15";
                ModInfo = "Info.json";
                ModsDirectory = "UnityModManagerMods";
            }
        }
    }
}
