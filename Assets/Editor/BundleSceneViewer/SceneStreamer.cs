using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor.Scene
{
    public class SceneStreamer : IDisposable
    {
        public delegate void OnCreateOneObjectDelegate(SceneBuilderObject obj, SceneBuilderObject parent);
        public delegate void OnCreateOneMeshDelegate(SceneBuilderMesh mesh, int meshId);
        public delegate void OnCreateOneMaterialDelegate(SceneBuilderMaterial mat, int matId);
        public delegate void OnCreateOneTextureDelegate(SceneBuilderTexture tex, int texId);
        public delegate void OnEndSceneDelegate();

        public event OnCreateOneObjectDelegate OnCreateOneObject;
        public event OnCreateOneMeshDelegate OnCreateOneMesh;
        public event OnCreateOneMaterialDelegate OnCreateOneMaterial;
        public event OnCreateOneTextureDelegate OnCreateOneTexture;
        public event OnEndSceneDelegate OnEndScene;

        public SceneStreamer(AssetDatabaseConnection database)
        {
            _connection = database;
            _connection.OnReceivedData += OnReceivedData;
        }

        public bool Begin(string sceneName)
        {
            return _connection.Send(AssetDatabaseRequestType.Scene, sceneName);
        }

        public void Dispose()
        {
            _connection.OnReceivedData -= OnReceivedData;
        }

        private readonly AssetDatabaseConnection _connection;

        private void OnReceivedData(AssetDatabaseRequestType request, Stream readStream)
        {
            try
            {
                int rootObjectCount = readStream.ReadInt32();
                Debug.Log($"Receiving {request} with {rootObjectCount} root objects");

                SceneBuilderObject[] rootObjects = new SceneBuilderObject[rootObjectCount];

                for (int i = 0; i < rootObjectCount; ++i)
                {
                    rootObjects[i] = SceneBuilderObject.Read(readStream, (obj, parent) => OnCreateOneObject?.Invoke(obj, parent));
                }

                int meshCount = readStream.ReadInt32();
                SceneBuilderMesh[] meshes = new SceneBuilderMesh[meshCount];
                for (int i = 0; i < meshCount; ++i)
                {
                    meshes[i] = readStream.ReadMesh();
                    OnCreateOneMesh?.Invoke(meshes[i], i);
                }

                int matCount = readStream.ReadInt32();
                SceneBuilderMaterial[] materials = new SceneBuilderMaterial[matCount];
                for (int i = 0; i < matCount; ++i)
                {
                    materials[i] = readStream.ReadMaterial();
                    OnCreateOneMaterial?.Invoke(materials[i], i);
                }

                int texCount = readStream.ReadInt32();
                SceneBuilderTexture[] textures = new SceneBuilderTexture[texCount];
                for (int i = 0; i < texCount; ++i)
                {
                    textures[i] = readStream.ReadTexture();
                    OnCreateOneTexture?.Invoke(textures[i], i);
                }

                Debug.Log("Finished reading scene!");
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read stream: {ex}");
            }
            finally
            {
                OnEndScene?.Invoke();
            }
        }
    }

    public class SceneBuilderObject
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int MeshId;
        public int[] MaterialIds;
        public SceneBuilderObject[] Children;

        public static SceneBuilderObject Read(
            Stream stream,
            SceneStreamer.OnCreateOneObjectDelegate fnOnCreateOneObject,
            SceneBuilderObject parent = null)
        {
            string name = stream.ReadString();
            Vector3 position = stream.ReadVector3();
            Quaternion rotation = stream.ReadQuaternion();
            Vector3 scale = stream.ReadVector3();
            int meshId = stream.ReadBoolean() ? stream.ReadInt32() : -1;
            int[] materialIds = new int[stream.ReadInt32()];

            for (int i = 0; i < materialIds.Length; ++i)
            {
                materialIds[i] = stream.ReadInt32();
            }

            int childCount = stream.ReadInt32();
            SceneBuilderObject[] children = new SceneBuilderObject[childCount];

            SceneBuilderObject obj = new()
            {
                Name = name,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                MeshId = meshId,
                MaterialIds = materialIds,
                Children = children
            };

            fnOnCreateOneObject.Invoke(obj, parent);

            for (int i = 0; i < childCount; ++i)
            {
                children[i] = Read(stream, fnOnCreateOneObject, obj);
            }

            return obj;
        }
    }

    public class SceneBuilderMesh
    {
        public string Name;

        public NativeArray<SubMeshDescriptor> SubMeshDescriptors;

        public int IndexCount;
        public IndexFormat IndexFormat;
        public NativeArray<ushort> IndexData16;
        public NativeArray<uint> IndexData32;

        public int VertexCount;
        public int VertexSize;
        public NativeArray<VertexAttributeDescriptor> VertexDescriptors;
        public NativeArray<byte> VertexStream;
    }

    public class SceneBuilderMaterial
    {
        public string Name;
        public string ShaderName;
        public int CustomRenderQueue;
        public bool DoubleSidedGI;
        public bool EnableInstancingVariants;
        public Dictionary<string, Color> Colors;
        public Dictionary<string, float> Floats;
        public Dictionary<string, int> Ints;
        public Dictionary<string, int> Textures;
        public string[] DisabledShaderPasses;
        public string[] InvalidKeywords;
        public string[] ValidKeywords;
        public Dictionary<string, string> StringTagMap;
    }

    public class SceneBuilderTexture
    {
        public string Name;
        public int Width;
        public int Height;
        public TextureFormat Format;
        public int FilterMode;
        public int Aniso;
        public float MipBias;
        public int WrapU;
        public int WrapV;
        public int WrapW;
        public NativeArray<byte> Data;
    }

    public static class StreamExtensions
    {
        public static string ReadString(this Stream stream)
        {
            int length = stream.ReadInt32();
            byte[] buffer = new byte[length];
            stream.ReadFully(buffer);
            return Encoding.ASCII.GetString(buffer);
        }

        public static int ReadInt32(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadFully(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadFully(buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public static Vector3 ReadVector3(this Stream stream)
        {
            float x = stream.ReadSingle();
            float y = stream.ReadSingle();
            float z = stream.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static Quaternion ReadQuaternion(this Stream stream)
        {
            float x = stream.ReadSingle();
            float y = stream.ReadSingle();
            float z = stream.ReadSingle();
            float w = stream.ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        public static float ReadSingle(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadFully(buffer);
            return BinaryPrimitivesExtensions.ReadSingleBigEndian(buffer);
        }

        public static bool ReadBoolean(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[1];
            stream.ReadFully(buffer);
            return buffer[0] == 1;
        }

        public static SceneBuilderMesh ReadMesh(this Stream stream)
        {
            string name = stream.ReadString();
            int subMeshCount = stream.ReadInt32();
            NativeArray<SubMeshDescriptor> subMeshDescriptors = new(subMeshCount, Allocator.Persistent);

            for (int i = 0; i < subMeshCount; ++i)
            {
                int smIndexStart = stream.ReadInt32();
                int smIndexCount = stream.ReadInt32();
                int topology = stream.ReadInt32();

                float centerX = stream.ReadSingle();
                float centerY = stream.ReadSingle();
                float centerZ = stream.ReadSingle();

                float extentX = stream.ReadSingle();
                float extentY = stream.ReadSingle();
                float extentZ = stream.ReadSingle();

                int baseVertex = stream.ReadInt32();
                int firstVertex = stream.ReadInt32();
                int smVertexCount = stream.ReadInt32();

                subMeshDescriptors[i] = new(smIndexStart, smIndexCount, (MeshTopology)topology)
                {
                    bounds = new(new(centerX, centerY, centerZ), new(extentX, extentY, extentZ)),
                    baseVertex = baseVertex,
                    firstVertex = firstVertex,
                    vertexCount = smVertexCount
                };
            }

            int indexCount = stream.ReadInt32();
            IndexFormat indexFormat = (IndexFormat)stream.ReadInt32();

            NativeArray<ushort> indices16 = new();
            NativeArray<uint> indices32 = new();

            if (indexFormat == IndexFormat.UInt16)
            {
                indices16 = new(indexCount, Allocator.Persistent);
                stream.ReadFully(MemoryMarshal.AsBytes(indices16.AsSpan()));
            }
            else
            {
                indices32 = new(indexCount, Allocator.Persistent);
                stream.ReadFully(MemoryMarshal.AsBytes(indices32.AsSpan()));
            }

            int vertexCount = stream.ReadInt32();
            int vertexSize = stream.ReadInt32();
            int vertexChannels = stream.ReadInt32();
            NativeArray<VertexAttributeDescriptor> vertexAttributeDescriptors = new(vertexChannels, Allocator.Persistent);

            for (int i = 0; i < vertexChannels; ++i)
            {
                VertexAttribute attribute = (VertexAttribute)stream.ReadInt32();
                VertexAttributeFormat format = (VertexAttributeFormat)stream.ReadInt32();
                int dimension = stream.ReadInt32();
                vertexAttributeDescriptors[i] = new(attribute, format, dimension);
            }

            int vertexStreamLength = stream.ReadInt32();
            NativeArray<byte> vertexStream = new(vertexStreamLength, Allocator.Persistent);
            stream.ReadFully(vertexStream.AsSpan());

            return new()
            {
                Name = name,
                SubMeshDescriptors = subMeshDescriptors,
                IndexCount = indexCount,
                IndexFormat = indexFormat,
                IndexData16 = indices16,
                IndexData32 = indices32,
                VertexCount = vertexCount,
                VertexSize = vertexSize,
                VertexDescriptors = vertexAttributeDescriptors,
                VertexStream = vertexStream
            };
        }

        public static SceneBuilderMaterial ReadMaterial(this Stream stream)
        {
            string name = stream.ReadString();
            string shaderName = stream.ReadString();
            int customRenderQueue = stream.ReadInt32();
            bool doubleSidedGI = stream.ReadBoolean();
            bool enableInstancingVariants = stream.ReadBoolean();

            int colorCount = stream.ReadInt32();
            Dictionary<string, Color> colors = new(colorCount);
            for (int i = 0; i < colorCount; i++)
            {
                string colorName = stream.ReadString();
                float r = stream.ReadSingle();
                float g = stream.ReadSingle();
                float b = stream.ReadSingle();
                float a = stream.ReadSingle();
                colors[colorName] = new Color(r, g, b, a);
            }

            int floatCount = stream.ReadInt32();
            Dictionary<string, float> floats = new(floatCount);
            for (int i = 0; i < floatCount; i++)
            {
                string floatName = stream.ReadString();
                float value = stream.ReadSingle();
                floats[floatName] = value;
            }

            int intCount = stream.ReadInt32();
            Dictionary<string, int> ints = new(intCount);
            for (int i = 0; i < intCount; i++)
            {
                string intName = stream.ReadString();
                int value = stream.ReadInt32();
                ints[intName] = value;
            }

            int textureCount = stream.ReadInt32();
            Dictionary<string, int> textures = new(textureCount);
            for (int i = 0; i < textureCount; i++)
            {
                string textureName = stream.ReadString();
                int textureId = stream.ReadInt32();
                textures[textureName] = textureId;
            }

            int disabledShaderPassesCount = stream.ReadInt32();
            string[] disabledShaderPasses = new string[disabledShaderPassesCount];
            for (int i = 0; i < disabledShaderPassesCount; i++)
            {
                disabledShaderPasses[i] = stream.ReadString();
            }

            int invalidKeywordsCount = stream.ReadInt32();
            string[] invalidKeywords = new string[invalidKeywordsCount];
            for (int i = 0; i < invalidKeywordsCount; i++)
            {
                invalidKeywords[i] = stream.ReadString();
            }

            int validKeywordsCount = stream.ReadInt32();
            string[] validKeywords = new string[validKeywordsCount];
            for (int i = 0; i < validKeywordsCount; i++)
            {
                validKeywords[i] = stream.ReadString();
            }

            int stringTagMapCount = stream.ReadInt32();
            Dictionary<string, string> stringTagMap = new(stringTagMapCount);
            for (int i = 0; i < stringTagMapCount; i++)
            {
                string tagName = stream.ReadString();
                string tagValue = stream.ReadString();
                stringTagMap[tagName] = tagValue;
            }

            return new SceneBuilderMaterial
            {
                Name = name,
                ShaderName = shaderName,
                CustomRenderQueue = customRenderQueue,
                DoubleSidedGI = doubleSidedGI,
                EnableInstancingVariants = enableInstancingVariants,
                Colors = colors,
                Floats = floats,
                Ints = ints,
                Textures = textures,
                DisabledShaderPasses = disabledShaderPasses,
                InvalidKeywords = invalidKeywords,
                ValidKeywords = validKeywords,
                StringTagMap = stringTagMap
            };
        }

        public static SceneBuilderTexture ReadTexture(this Stream stream)
        {
            string name = stream.ReadString();
            int width = stream.ReadInt32();
            int height = stream.ReadInt32();
            TextureFormat format = (TextureFormat)stream.ReadInt32();
            int filterMode = stream.ReadInt32();
            int aniso = stream.ReadInt32();
            float mipBias = stream.ReadSingle();
            int wrapU = stream.ReadInt32();
            int wrapV = stream.ReadInt32();
            int wrapW = stream.ReadInt32();
            int textureSize = stream.ReadInt32();

            NativeArray<byte> data = new(textureSize, Allocator.Persistent);
            stream.ReadFully(data.AsSpan());

            return new()
            {
                Name = name,
                Width = width,
                Height = height,
                Format = format,
                FilterMode = filterMode,
                Aniso = aniso,
                MipBias = mipBias,
                WrapU = wrapU,
                WrapV = wrapV,
                WrapW = wrapW,
                Data = data
            };
        }

        public static void ReadFully(this Stream stream, Span<byte> buffer)
        {
            int totalRead = 0;
            int toRead = buffer.Length;
            while (totalRead < toRead)
            {
                int read = stream.Read(buffer[totalRead..]);
                if (read == 0)
                {
                    throw new EndOfStreamException("Stream closed too early.");
                }
                totalRead += read;
            }
        }
    }

    public static class BinaryPrimitivesExtensions
    {
        public static float ReadSingleBigEndian(ReadOnlySpan<byte> buffer)
        {
            uint intValue = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            return MemoryMarshal.Cast<uint, float>(MemoryMarshal.CreateReadOnlySpan(ref intValue, 1))[0];
        }
    }
}
