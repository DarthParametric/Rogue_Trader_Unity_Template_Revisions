using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    public class FileUtility
    {
        public static IEnumerable<string> EnumerateFilesChangedSince(string dirPath, string pattern, DateTime minTime)
        {
            var time = Directory.GetLastWriteTimeUtc(dirPath);
            // The current folder's LastWriteTime DOES update every time a child FILE is written to,
            // so if the current folder does not pass the date filter, we already know that no files within will pass, either
            if (time >= minTime)
            {
                foreach (string filePath in Directory.EnumerateFiles(dirPath, pattern, SearchOption.TopDirectoryOnly))
                {
                    // do NOT check file time b/c we may want to check files that were moved
                    // and thus have an old write time but are still "new" for us
                    //if (File.GetLastWriteTimeUtc(filePath) >= minTime)
                        yield return filePath;
                }
            }
            else
            {
                //Debug.Log("skip files in "+dirPath);
            }

            // Unfortunately, the current folder's LastWriteTime does NOT update every time a child FOLDER is written to,
            // so we have to search ALL subdirectories regardless of the current folder's LastWriteTime
            foreach (string subDirPath in Directory.GetDirectories(dirPath))
            {
                var inner = EnumerateFilesChangedSince(subDirPath, pattern, minTime);
                foreach (var path in inner)
                    yield return path;
            }
        }

        public static string ReadAllText(string path, ILogger logger)
        {
            int maxAttempts = 5;
            while (maxAttempts-- > 0)
            {
                try
                {
                    using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    return sr.ReadToEnd();
                }
                catch (IOException e)
                {
                    logger.LogError($"Failed reading {path}\n{e.Message}{(e.StackTrace != null ? "\n" + e.StackTrace : "")}");
                    if (maxAttempts == 0)
                        throw; // on last attempt, actually throw so we can see exception details
                    Thread.Sleep(50);
                }
            }

            return "we should NEVER get here anyway"; // we should NEVER get here anyway
        }
    }
}