using Editor.BundleSceneViewerStarter;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Editor.Scene
{
    public enum AssetDatabaseRequestType : byte
    {
        Scene
    };

    public class AssetDatabaseConnection : IDisposable
    {
        public IEnumerable<string> Scenes => _scenes;

        public delegate void OnReceivedSceneListDelegate();
        public delegate void OnReceivedDataDelegate(AssetDatabaseRequestType request, Stream readStream);

        public event OnReceivedSceneListDelegate OnReceivedSceneList;
        public event OnReceivedDataDelegate OnReceivedData;

        public void Start()
        {
            _thread = new(ConnectionThread) { IsBackground = true };
            _thread.Start();
        }

        public bool Send(AssetDatabaseRequestType request, string data)
        {
            if (!_running)
            {
                return false;
            }

            Stream stream = _client.GetWriteStream();

            Span<byte> lengthBuffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, data.Length);

            stream.WriteByte((byte)request);
            stream.Write(lengthBuffer);
            stream.Write(Encoding.ASCII.GetBytes(data));

            return true;
        }

        public void Update()
        {
            if (_failed)
            {
                Dispose();
                Start();
            }
        }

        private bool _quitting;
        private bool _running;
        private bool _failed;
        private readonly List<string> _scenes = new();
        private IDbClient _client;
        private Thread _thread;
        
        private void ConnectionThread()
        {
            _running = false;
            _quitting = false;

            Debug.Log($"Connecting to server.");

            _client = new DbPipeClient();
            
            if (!_client.Connect())
            {
                _failed = true;
                Debug.LogWarning($"Failed to connect to server.");
                return;
            }

            Debug.Log($"Connected to server.");

            Stream readStream = _client.GetReadStream();
            ReadSceneList(readStream);
            OnReceivedSceneList?.Invoke();

            Span<byte> buffer = stackalloc byte[1];

            try
            {
                _running = true;

                while (!_quitting)
                {
                    if (readStream.Read(buffer) == -1)
                    {
                        throw new IOException("Read -1 from stream");
                    }

                    AssetDatabaseRequestType request = (AssetDatabaseRequestType)buffer[0];
                    OnReceivedData?.Invoke(request, readStream);
                }
            }
            catch (IOException ex)
            {
                _failed = true;
                Debug.LogError(ex.ToString());
            }
            finally
            {
                _running = false;
            }

            Debug.Log($"Disconnected from server.");
        }

        private void ReadSceneList(Stream stream)
        {
            int sceneCount = stream.ReadInt32();
            for (int i = 0; i < sceneCount; ++i)
            {
                string scene = stream.ReadString();
                _scenes.Add(scene);
            }
        }

        private static string ReadString(Stream stream, int length)
        {
            Span<byte> stringBuffer = stackalloc byte[length];
            int bytesRead = stream.Read(stringBuffer);
            Debug.Assert(bytesRead == stringBuffer.Length);
            return Encoding.ASCII.GetString(stringBuffer);
        }

        public void Dispose()
        {
            _failed = false;
            _quitting = true;
            _client.Dispose();
            BundleSceneServerStarter.KillServer();
            _thread.Join();
        }
    }
}

public interface IDbClient : IDisposable
{
    public bool Connect();
    public Stream GetReadStream();
    public Stream GetWriteStream();
}

public class DbTcpClient : IDbClient
{
    public bool Connect()
    {
        _client = new TcpClient();

        CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.CancelAfter(1000);

        try
        {
            _client.ConnectAsync(IPAddress.Loopback.ToString(), 16253).Wait(cancellationTokenSource.Token);
            _readStream = new(_client.GetStream());
            return true;
        }
        catch (OperationCanceledException)
        {
            _client.Dispose();
        }

        return false;
    }

    public Stream GetReadStream() => _readStream;
    public Stream GetWriteStream() => _client.GetStream();

    public void Dispose()
    {
        _client.Dispose();
    }

    private TcpClient _client;
    private BufferedStream _readStream;
}

public class DbPipeClient : IDbClient
{
    public bool Connect()
    {
        _stream = new(".", "RogueTraderPipe", PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            _stream.Connect(1000);
            _readStream = new(_stream);
            return true;
        }
        catch (TimeoutException)
        {
            _stream.Dispose();
        }

        return false;
    }

    public Stream GetReadStream() => _readStream;
    public Stream GetWriteStream() => _stream;

    public void Dispose()
    {
        _stream.Dispose();
    }

    private NamedPipeClientStream _stream;
    private BufferedStream _readStream;
}
