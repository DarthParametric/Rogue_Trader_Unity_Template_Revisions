using Kingmaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public partial class ModEntry
        {
            public class ModLogger
            {
                protected readonly string Prefix;
                protected readonly string PrefixError;
                protected readonly string PrefixCritical;
                protected readonly string PrefixWarning;
                protected readonly string PrefixException;

                public ModLogger(string Id)
                {
                    Prefix = $"[{Id}] ";
                    PrefixError = $"[{Id}] [Error] ";
                    PrefixCritical = $"[{Id}] [Critical] ";
                    PrefixWarning = $"[{Id}] [Warning] ";
                    PrefixException = $"[{Id}] [Exception] ";
                }

                public void Log(string str)
                {
                    PFLog.UnityModManager.Log($"{Prefix} {str}");
                }

                public void Error(string str)
                {
                    PFLog.UnityModManager.Error($"{PrefixError} {str}");
                }

                public void Critical(string str)
                {
                    PFLog.UnityModManager.Error($"{PrefixCritical} {str}");
                }

                public void Warning(string str)
                {
                    PFLog.UnityModManager.Warning($"{PrefixWarning} {str}");
                }

                public void NativeLog(string str)
                {
                    PFLog.UnityModManager.Log($"{Prefix} {str}");
                }

                /// <summary>
                /// [0.17.0]
                /// </summary>
                public void LogException(string key, Exception e)
                {
                    PFLog.UnityModManager.Exception(e, PrefixException, key);
                }

                /// <summary>
                /// [0.17.0]
                /// </summary>
                public void LogException(Exception e)
                {
                    PFLog.UnityModManager.Exception(e, PrefixException);
                }
            }
        }
    }
}
