using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Owlcat.Blueprints.Server.Communication;
using Owlcat.Blueprints.Server.Communication.Commands.Processing;
using Owlcat.Blueprints.Server.Communication.Commands.Processing.Factory;
using Owlcat.Blueprints.Server.FileDatabase;
using Owlcat.Blueprints.Server.Utils;
using Serilog;

namespace Owlcat.Blueprints.Server
{
    internal class Program
    {
        private const string MainNameWin = "BlueprintIndexingServer.exe";
        private const string MainNameMac = "BlueprintIndexingServer";
        private const string MainNameLinux = "BlueprintIndexingServer";
        private const string BackupNameTemplateWin = "BlueprintIndexingServer-LocalCopy{0}.exe";
        private const string BackupNameTemplateMac = "BlueprintIndexingServer-LocalCopy{0}";
        private const string BackupNameTemplateLinux = "BlueprintIndexingServer-LocalCopy{0}";

        private static string GetMainName()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return MainNameWin;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return MainNameMac;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return MainNameLinux;

            throw new PlatformNotSupportedException("Unknown platform");
        }

        private static string GetBackupTemplate()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return BackupNameTemplateWin;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return BackupNameTemplateMac;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return BackupNameTemplateLinux;

            throw new PlatformNotSupportedException("Unknown platform");
        }

        public static ILoggerFactory LogFactory;
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Need args for database folder and index folder");
                return;
            }

            var databaseFolder = new DirectoryInfo(args[0]);
            var indexFolder = new DirectoryInfo(args[1]);
            if (!databaseFolder.Exists)
            {
                Console.WriteLine("Database folder {0} not found", databaseFolder.FullName);
                return;
            }
            if (!indexFolder.Exists)
            {
                Console.WriteLine("Index folder {0} not found", indexFolder.FullName);
                return;
            }

            Log.Logger = new LoggerConfiguration()
                                .MinimumLevel.Information()
                                .WriteTo.Async(a => a.Console())
                                .WriteTo.Async(a => a.File(Path.Combine(indexFolder.FullName, "biServer.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5))
                                .CreateLogger();

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(dispose: true));
            LogFactory = loggerFactory;
            var logger = loggerFactory.CreateLogger<Program>();

            RelaunchLocalCopyIfNeeded(logger);


            var db = new FileDatabase.FileDatabase(databaseFolder.FullName, "*.jbp", new FileReader(), loggerFactory);

            // try to read index
            var indexPath = Path.Combine(indexFolder.FullName, "blueprints-index");
            var hasIndex = false;
            if (File.Exists(indexPath))
            {
                try
                {
                    logger.LogInformation("Loading index file from {IndexPath}", indexPath);
                    db.LoadIndex(indexPath);
                    logger.LogInformation("Updating index with new changes");
                    db.UpdateIndex();
                    hasIndex = true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ex");
                }
            }

            // index not found or broken
            if (!hasIndex)
            {
                logger.LogInformation("Creating new index. This will take a minute.");
                db.CreateIndex();
                logger.LogInformation("Index built, saving to {IndexPath}", indexPath);
                db.SaveIndex(indexPath);
            }

            // set up watcher to update index
            var fsw = new FileSystemWatcher(databaseFolder.FullName, "*.*")
            {
                NotifyFilter = NotifyFilters.LastWrite
                               | NotifyFilters.FileName
                               | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
            };

            fsw.Changed += (s, e) => db.HandleFileChange(e.FullPath);
            fsw.Created += (s, e) => db.HandleFileChange(e.FullPath);
            fsw.Deleted += (s, e) => db.HandleFileDeleted(e.FullPath, false);
            fsw.Renamed += (s, e) =>
            {
                logger.LogInformation("Detected rename from {OldFullPath} to {FullPath}", e.OldFullPath, e.FullPath);
                if (Directory.Exists(e.FullPath))
                {
                    // it's whole folder, do full index update
                    db.UpdateIndex();
                    db.RaiseFileChange("all"); // this is an ugly hck to tell client to invalidate all caches
                    return;
                }

                db.HandleFileDeleted(e.OldFullPath, true);
                db.HandleFileChange(e.FullPath);
            };
            fsw.EnableRaisingEvents = true;

            var pf = ProcessingFactory.Create()
                .RegistrationProcessCommand(CommandTypes.GetBaseFolder, ProcessingGetBaseFolder.Processing)
                .RegistrationProcessCommand(CommandTypes.GetPathFromId, ProcessingGetPathFromId.Processing)
                .RegistrationProcessCommand(CommandTypes.GetIdFromPath, ProcessingGetIdFromPath.Processing)
                .RegistrationProcessCommand(CommandTypes.GetNameFromId, ProcessingGetNameFromId.Processing)
                .RegistrationProcessCommand(CommandTypes.GetIsShadowDeletedFromId, ProcessingGetIsShadowDeletedFromId.Processing)
                .RegistrationProcessCommand(CommandTypes.GetContainsShadowDeletedBlueprintsFromId, ProcessingGetContainsShadowDeletedBlueprintsFromId.Processing)
                .RegistrationProcessCommand(CommandTypes.GetListOfContainsShadowDeletedBlueprints, ProcessingGetListOfContainsShadowDeletedBlueprints.Processing)
                .RegistrationProcessCommand(CommandTypes.GetListOfContainsRemoveBlueprints, ProcessingGetListOfContainsRemoveBlueprints.Processing)
                .RegistrationProcessCommand(CommandTypes.GetTypeIdFromId, ProcessingGetTypeIdFromId.Processing)
                .RegistrationProcessCommand(CommandTypes.SearchByName, ProcessingSearchByName.Processing)
                .RegistrationProcessCommand(CommandTypes.SearchByNameExact, ProcessingSearchByNameExact.Processing)
                .RegistrationProcessCommand(CommandTypes.SearchByType, ProcessingSearchByType.Processing)
                .RegistrationProcessCommand(CommandTypes.Result, ProcessingResult.Processing)
                .RegistrationProcessCommand(CommandTypes.SearchResult, ProcessingSearchResult.Processing)
                .RegistrationProcessCommand(CommandTypes.GetListOfDuplicates, ProcessingGetListOfDuplicates.Processing)
                .RegistrationProcessCommand(CommandTypes.PauseIndexing, ProcessingPauseIndexing.Processing)
                .RegistrationProcessCommand(CommandTypes.ResumeIndexing, ProcessingResumeIndexing.Processing);

            using var cts = new CancellationTokenSource();
            // start named pipe
            var (commandPort, watcherPort, task) = Communication.Server.Start(db, pf, cts.Token);

            // put a mark into index folder to inform of pipe name for this folder
            var pipeFilePath = Path.Combine(indexFolder.FullName, "ports");
            try
            {
                using var sw = new StreamWriter(pipeFilePath + "_");
                sw.WriteLine(commandPort);
                sw.WriteLine(watcherPort);
                sw.WriteLine(Environment.ProcessId);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Cannot write to file {file}", pipeFilePath + "_");
                return;
            }
            // first write out the file and then move it: the client might be waiting
            // to read the file the moment it appears, so we may not be able to write to "pipe" once we create it
            File.Move(pipeFilePath + "_", pipeFilePath, true);

            // setup cleanup on process exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                logger.LogCritical("Exiting process");
                File.Delete(pipeFilePath);
                db.SaveIndex(indexPath);
                Log.CloseAndFlush();
            };

            Console.CancelKeyPress += (s, e) => cts.Cancel();

            // also if we crash, too
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.Delete(pipeFilePath);
                logger.LogCritical((Exception)e.ExceptionObject, "Unhandled exception");
                Log.CloseAndFlush();
            };

            // exit on revision change, will be restarted by Unity
            var _ = new VCSWatcher(() => Environment.Exit(0));

            // do not exit until server is done
            task.Wait();
            logger.LogCritical("Server finished, exiting");
            Log.CloseAndFlush();
        }

        private static FileInfo ConstuctBackupLocation(DirectoryInfo ownLocation, Microsoft.Extensions.Logging.ILogger logger)
        {
            for(int i = 0; i<10;++i)
            {
                var backupName = string.Format(GetBackupTemplate(), i);
                var backupLocation = new FileInfo(Path.Combine(ownLocation.FullName, backupName));
                if (backupLocation.Exists)
                {
                    try
                    {
                        backupLocation.Delete();
                    }
                    catch(Exception e)
                    {
                        logger.LogWarning(e, "Cant delete file {BackupFileName}", backupLocation.FullName);
                        continue;
                    }
                }
                return backupLocation;
            }
            throw new InvalidOperationException("Cant construct backup location");
        }

        private static void RelaunchIfNeeded(Microsoft.Extensions.Logging.ILogger logger)
        {
            var ownLocation = new FileInfo(Environment.GetCommandLineArgs()[0]);
            if (ownLocation.Directory == null)
                return;

            var mainLocation = new FileInfo(Path.Combine(ownLocation.Directory.FullName, GetMainName()));

            if (ownLocation.Name == GetMainName())
            {
                var backupLocation = ConstuctBackupLocation(ownLocation.Directory, logger);

                ownLocation.MoveTo(backupLocation.FullName);
                backupLocation.CopyTo(mainLocation.FullName);
            }
            else
            {
                if (mainLocation.Exists && mainLocation.LastWriteTimeUtc != ownLocation.LastWriteTimeUtc)
                {
                    const ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal;
                    var startInfo = new ProcessStartInfo(mainLocation.FullName)
                    {
                        WindowStyle = windowStyle,
                    };
                    Process.Start(startInfo);

                    Environment.Exit(0);
                }
            }

            // todo: watch main location for modifications
        }

        private static void RelaunchLocalCopyIfNeeded(Microsoft.Extensions.Logging.ILogger logger)
        {
            const int maxTriesCount = 10;
            int triesCount = 0;
            while (true)
            {
                triesCount++;

                try
                {
                    RelaunchIfNeeded(logger);
                    break;
                }
                catch (Exception e)
                {
                    if (triesCount >= maxTriesCount)
                    {
                        Log.Fatal(e, "Exception occured in RelaunchLocalCopyIfNeeded");
                        throw;
                    }

                    Thread.Sleep(100);
                }
            }
        }
    }
}
