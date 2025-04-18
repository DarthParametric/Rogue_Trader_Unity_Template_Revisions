using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Owlcat.Blueprints.Server.Communication.Commands.Processing.Factory;

namespace Owlcat.Blueprints.Server.Communication
{
    public static class Server
    {
        public static (int commandPort, int watcherPort, Task task) Start(FileDatabase.FileDatabase database, IProcessingFactory processingFactory, CancellationToken ct)
        {
            var logger = Program.LogFactory.CreateLogger("Starter");

            logger.LogInformation("Starting server");

            var commandServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            commandServer.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            commandServer.Listen(10);

            var commandTask = Task.Run(() => CommandServerProc(commandServer, database, processingFactory, ct));
            var commandPort = (commandServer.LocalEndPoint as IPEndPoint)?.Port ?? throw new Exception("Cant get port");

            var watcherServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            watcherServer.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            watcherServer.Listen(10);

            var watcherTask = Task.Run(() => WatcherServerProc(watcherServer, database, ct));
            var watcherPort = (watcherServer.LocalEndPoint as IPEndPoint)?.Port ?? throw new Exception("Cant get port");

            return (commandPort, watcherPort, Task.WhenAll(watcherTask, commandTask));

        }

        private static void WatcherServerProc(Socket watcherServer, FileDatabase.FileDatabase database, CancellationToken ct)
        {
            var logger = Program.LogFactory.CreateLogger("WatcherServer");
            ct.Register(() => watcherServer.Close());

            using (watcherServer)
            {
                logger.LogInformation("Watcher server waiting for a client on port {Port}...", (watcherServer.LocalEndPoint as IPEndPoint)?.Port ?? -1);
                while (true)
                {
                    var watcherClient = watcherServer.Accept();
                    _ = Task.Run(() => WatcherClientProc(watcherClient, database));

                }
            }
        }

        private static void WatcherClientProc(Socket client, FileDatabase.FileDatabase database)
        {
            var logger = Program.LogFactory.CreateLogger("WatcherClient");
            using (client)
            {
                logger.LogInformation("Watcher client connected, remote port {Port}", (client.RemoteEndPoint as IPEndPoint)?.Port ?? -1);
                using var stream = new NetworkStream(client);

                void ReportFileChangeToClient(string id)
                {
                    try
                    {
                        logger.LogInformation("Trying to send changed id {id}", id);
                        Memory<byte> bytes = new byte[1024 * 1024];
                        if (client.Connected)
                        {
                            var len = Encoding.UTF8.GetBytes(id, bytes[sizeof(int)..].Span);
                            BitConverter.TryWriteBytes(bytes[..sizeof(int)].Span, len);
                            stream.Write(bytes[..(sizeof(int) + len)].Span);
                        }
                        else
                        {
                            logger.LogInformation("Skipped sending {id}, not connected", id);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Exception sending changes");
                    }
                }
                Memory<byte> bytes = new byte[1024 * 1024];

                database.OnFileChange += ReportFileChangeToClient;
                try
                {
                    while (client.Connected)
                    {
                        stream.SureRead(bytes);
                    }
                }
                catch (Exception x)
                {
                    logger.LogError(x, "Watcher client exception");
                }
                finally
                {
                    logger.LogInformation("Watcher client disconnected");
                    database.OnFileChange -= ReportFileChangeToClient;
                }
            }

        }

        private static void CommandServerProc(Socket commandServer, FileDatabase.FileDatabase database, IProcessingFactory processingFactory, CancellationToken ct)
        {
            var logger = Program.LogFactory.CreateLogger("CommandServer");
            ct.Register(() =>
            {
                commandServer.Close();
            }
            );
            using (commandServer)
            {
                logger.LogInformation("Command server waiting for a client on port {Port}...", (commandServer.LocalEndPoint as IPEndPoint)?.Port ?? -1);
                while (true)
                {
                    // Wait for a client to connect
                    var commandClient = commandServer.Accept();
                    _ = Task.Run(() => CommandClientProc(commandClient, database, processingFactory));
                }
            }
        }

        private static void CommandClientProc(Socket client, FileDatabase.FileDatabase database, IProcessingFactory processingFactory)
        {
            var logger = Program.LogFactory.CreateLogger("CommandClient");
            using (client)
            {
                Memory<byte> buffer = new byte[1024 * 1024 * 50];
                logger.LogInformation("Command client connected, remote port {Port}", (client.RemoteEndPoint as IPEndPoint)?.Port ?? -1);
                using var stream = new NetworkStream(client);
                try
                {
                    while (client.Connected)
                    {
                        ReadCommand(stream, database, processingFactory, buffer);
                    }
                }
                catch (Exception x)
                {
                    logger.LogError(x, "Command client exception");
                }
                finally
                {
                    logger.LogInformation("Command client disconnected");
                }
            }
        }

        private static void WriteCommandImpl(Command command, Stream stream, Memory<byte> buffer)
        {
            BitConverter.TryWriteBytes(buffer[..sizeof(int)].Span, (int)command.Type);
            stream.Write(buffer[..sizeof(int)].Span);

            int payloadLen = Encoding.UTF8.GetBytes(command.Payload, buffer[sizeof(int)..].Span);
            BitConverter.TryWriteBytes(buffer[..sizeof(int)].Span, payloadLen);
            stream.Write(buffer[..(sizeof(int) + payloadLen)].Span);
        }

        private static int SureRead(this Stream stream, Memory<byte> mem)
        {
            var readCount = stream.Read(mem.Span);
            if (readCount != mem.Length)
                throw new IOException("Failed to read requested length");
            return readCount;
        }

        private static Command ReadCommandImpl(Stream stream, Memory<byte> buffer)
        {
            var cmd = new Command();

            var readCount = stream.SureRead(buffer[..sizeof(int)]);
            cmd.Type = (CommandTypes)BitConverter.ToInt32(buffer[..sizeof(int)].Span);

            readCount = stream.SureRead(buffer[..sizeof(int)]);
            var payloadLen = BitConverter.ToInt32(buffer[..sizeof(int)].Span);
            if (payloadLen > 0)
            {
                readCount = stream.SureRead(buffer[..payloadLen]);
                cmd.Payload = Encoding.UTF8.GetString(buffer[..payloadLen].Span);
            }
            else
                cmd.Payload = "";

            return cmd;
        }

        private static void ReadCommand(Stream stream, FileDatabase.FileDatabase database, IProcessingFactory processingFactory, Memory<byte> buffer)
        {
            var logger = Program.LogFactory.CreateLogger("ReadCommand");
            var cmd = ReadCommandImpl(stream, buffer);

            var sw = new Stopwatch();
            sw.Start();

            logger.LogInformation("Client has sent: {CmdType}, {CmdPayload}", cmd.Type, cmd.Payload);

            Command? result = null;
            try
            {
                result = HandleCommand(cmd, database, processingFactory);
                if (result != null)
                    WriteCommandImpl(result, stream, buffer);
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("Server has replied in {TotalMillis}ms: {ResultType}, {ResultPayload}",
                    sw.Elapsed.TotalMilliseconds,
                    result?.Type.ToString() ?? "<no result>",
                    result != null ? result.Payload.Length > 60 ? result.Payload.Substring(0, 60) + "..." : result.Payload : "<no result>");
            }
        }

        private static Command? HandleCommand(Command request, FileDatabase.FileDatabase database, IProcessingFactory processingFactory)
        {
            var logger = Program.LogFactory.CreateLogger("HandleCommand");

            if (!processingFactory.TryProcessCommand(request, database, out var result))
            {
                logger.LogWarning("Command {CmdId} not recognized", request.Type);
                return null;
            }

            return result;
            
            // switch (request.Type)
            // {
            //     case CommandTypes.GetBaseFolder:
            //         return CreateResult(CommandTypes.Result, database.BasePath);
            //     case CommandTypes.GetIdFromPath:
            //         return CreateResult(CommandTypes.Result, database.PathToId(request.Payload));
            //     case CommandTypes.GetPathFromId:
            //         return CreateResult(CommandTypes.Result, database.IdToPath(request.Payload));
            //     case CommandTypes.GetNameFromId:
            //         return CreateResult(CommandTypes.Result, "NOT IMPLEMENTED");
            //     case CommandTypes.GetTypeIdFromId:
            //         return CreateResult(CommandTypes.Result, database.IdToType(request.Payload));
            //     case CommandTypes.SearchByName:
            //         {
            //             var ids = database.SearchByName(request.Payload);
            //
            //             StringBuilder sb = new StringBuilder();
            //             for (int ii = 0; ii < ids.Count; ii++)
            //             {
            //                 sb.Append(ids[ii]).Append(database.IdToPath(ids[ii]))
            //                     .Append("?"); // ? is a good delimiter since paths cannot contain it
            //             }
            //
            //             return new Command { Type = CommandTypes.SearchResult, Payload = sb.ToString() };
            //         }
            //
            //     case CommandTypes.SearchByNameExact:
            //         {
            //             var id = database.GetByName(request.Payload);
            //             return new Command { Type = CommandTypes.SearchResult, Payload = id ?? "" };
            //         }
            //
            //     case CommandTypes.SearchByType:
            //         {
            //             var types = request.Payload.Split(' ');
            //             var ids = database.SearchByTypeList(types);
            //             StringBuilder sb = new StringBuilder();
            //             for (int ii = 0; ii < ids.Count; ii++)
            //             {
            //                 sb.Append(ids[ii]).Append(database.IdToPath(ids[ii]))
            //                     .Append("?"); // ? is a good delimiter since paths cannot contain it
            //             }
            //
            //             return new Command { Type = CommandTypes.SearchResult, Payload = sb.ToString() };
            //         }
            //     case CommandTypes.Result:
            //         return null;
            //     case CommandTypes.SearchResult:
            //         return null;
            //     case CommandTypes.GetListOfDuplicates:
            //         return CreateResult(CommandTypes.Result, string.Join(" ", database.GetDuplicatedIds()));
            //     case CommandTypes.PauseIndexing:
            //         database.PauseIndexing();
            //         return CreateResult(CommandTypes.Result, "OK");
            //     case CommandTypes.ResumeIndexing:
            //         database.ResumeIndexing();
            //         return CreateResult(CommandTypes.Result, "OK");
            //     default:
            //         logger.LogWarning("Command {CmdId} not recognized", request.Type);
            //         return null;
            // }
        }

        private static Command CreateResult(CommandTypes types, string result)
            => new Command { Type = types, Payload = result ?? "" };

        private static string GetCurrentTimeString()
            => DateTime.Now.ToString("HH:mm:ss.fff");
    }
}