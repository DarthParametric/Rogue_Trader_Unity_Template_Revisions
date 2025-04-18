using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Editor.Scene
{
    public class SceneBuilder : IDisposable
    {
        public bool Finished =>
            _streamer == null &&
            _objectCreationQueue.Count == 0 &&
            _meshCreationQueue.Count == 0 &&
            _materialCreationQueue.Count == 0 &&
            _textureCreationQueue.Count == 0;

        public SceneBuilder(AssetDatabaseConnection database)
        {
            _db = database;
        }

        public bool LoadScene(string name, bool additive)
        {
            _objectParentLookup.Clear();
            _meshes.Clear();
            _materials.Clear();
            _textures.Clear();

            _streamer = new(_db);
            _streamer.OnCreateOneObject += (x, y) => _objectCreationQueue.Enqueue((x, y));
            _streamer.OnCreateOneMesh += (x, y) => _meshCreationQueue.Enqueue((x, y));
            _streamer.OnCreateOneMaterial += (x, y) => _materialCreationQueue.Enqueue((x, y));
            _streamer.OnCreateOneTexture += (x, y) => _textureCreationQueue.Enqueue((x, y));
            _streamer.OnEndScene += () => EditorApplication.delayCall += Dispose;

            if (!additive)
            {
                _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                SceneManager.SetActiveScene(_scene);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            
            return _streamer.Begin(name);
        }

        public void Update()
        {
            while (_objectCreationQueue.TryDequeue(out (SceneBuilderObject, SceneBuilderObject) work))
            {
                SceneBuilderObject obj = work.Item1;
                SceneBuilderObject parent = work.Item2;

                GameObject gameObject = new(obj.Name);

                if (obj.MeshId != -1)
                {
                    _objectsAwaitingMesh.Add((gameObject, obj.MeshId));
                    _objectsAwaitingMaterials.Add((gameObject, obj.MaterialIds));
                }

                if (parent == null)
                {
                    SceneManager.MoveGameObjectToScene(gameObject, _scene);
                }
                else
                {
                    GameObject parentGameObject = _objectParentLookup[parent];
                    gameObject.transform.SetParent(parentGameObject.transform);
                }

                gameObject.transform.localPosition = obj.Position;
                gameObject.transform.localRotation = obj.Rotation;
                gameObject.transform.localScale = obj.Scale;

                _objectParentLookup[obj] = gameObject;
            }

            ProcessObjectQueues();
            ProcessUnityQueues();
        }

        public void Dispose()
        {
            _streamer?.Dispose();
            _streamer = null;
        }

        private SceneStreamer _streamer;
        private UnityEngine.SceneManagement.Scene _scene;

        private readonly ConcurrentQueue<(SceneBuilderObject, SceneBuilderObject)> _objectCreationQueue = new();
        private readonly ConcurrentQueue<(SceneBuilderMesh, int)> _meshCreationQueue = new();
        private readonly ConcurrentQueue<(SceneBuilderMaterial, int)> _materialCreationQueue = new();
        private readonly ConcurrentQueue<(SceneBuilderTexture, int)> _textureCreationQueue = new();

        private readonly Dictionary<SceneBuilderObject, GameObject> _objectParentLookup = new();
        private readonly List<(GameObject, int)> _objectsAwaitingMesh = new();
        private readonly List<(GameObject, int[])>  _objectsAwaitingMaterials = new();

        private readonly Dictionary<int, Mesh> _meshes = new();
        private readonly Dictionary<int, (Material, SceneBuilderMaterial)> _materials = new();
        private readonly Dictionary<int, Texture> _textures = new();

        private readonly AssetDatabaseConnection _db;

        private void ProcessObjectQueues()
        {
            List<GameObject> finishedMeshes = new();

            foreach ((GameObject gameObject, int meshId) in _objectsAwaitingMesh)
            {
                if (_meshes.TryGetValue(meshId, out Mesh mesh))
                {
                    MeshFilter filter = gameObject.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    finishedMeshes.Add(gameObject);
                }
            }

            foreach (GameObject gameObject in finishedMeshes)
            {
                _objectsAwaitingMesh.RemoveAll(x => x.Item1 == gameObject);
            }

            List<GameObject> finishedMaterials = new();

            foreach ((GameObject gameObject, int[] materialIds) in _objectsAwaitingMaterials)
            {
                if (!gameObject.TryGetComponent(out MeshRenderer renderer))
                {
                    if (!materialIds.All(x => _materials.ContainsKey(x)))
                    {
                        continue;
                    }

                    renderer = gameObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = materialIds.Select(x => _materials[x].Item1).ToArray();
                }

                Material[] mats = materialIds.Select(x => _materials[x].Item1).ToArray();
                SceneBuilderMaterial[] sbMats = materialIds.Select(x => _materials[x].Item2).ToArray();
                bool allTexturesReady = true;

                for (int i = 0; i < sbMats.Length; ++i)
                {
                    Material mat = mats[i];
                    SceneBuilderMaterial sbMat = sbMats[i];

                    foreach ((string sbName, int texId) in sbMat.Textures
                        .Where(x => mat.HasTexture(x.Key) /* for fallback */))
                    {
                        bool textureReady = _textures.TryGetValue(texId, out Texture texture);
                        allTexturesReady &= textureReady;

                        if (textureReady && mat.GetTexture(sbName) != texture)
                        {
                            mat.SetTexture(sbName, texture);
                        }
                    }
                }

                if (allTexturesReady)
                {
                    finishedMaterials.Add(gameObject);
                }
            }

            foreach (GameObject gameObject in finishedMaterials)
            {
                _objectsAwaitingMaterials.RemoveAll(x => x.Item1 == gameObject);
            }
        }

        private void ProcessUnityQueues()
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 16 && _meshCreationQueue.TryDequeue(out (SceneBuilderMesh, int) work))
            {
                SceneBuilderMesh mesh = work.Item1;

                try
                {
                    _meshes[work.Item2] = CreateMesh(mesh);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create mesh {mesh.Name} because {e}");
                    _meshes[work.Item2] = MeshHelper.GetPrimitiveMesh(PrimitiveType.Cube);
                }
                finally
                {
                    mesh.SubMeshDescriptors.Dispose();
                    mesh.IndexData16.Dispose();
                    mesh.IndexData32.Dispose();
                    mesh.VertexDescriptors.Dispose();
                    mesh.VertexStream.Dispose();
                }
            }

            while (sw.ElapsedMilliseconds < 16 && _materialCreationQueue.TryDequeue(out (SceneBuilderMaterial, int) work))
            {
                SceneBuilderMaterial mat = work.Item1;

                try
                {
                    _materials[work.Item2] = (CreateMaterial(mat), mat);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create material {mat.Name} because {e}");
                    _materials[work.Item2] = (new(Shader.Find("Standard")), mat);
                }
            }

            while (sw.ElapsedMilliseconds < 16 && _textureCreationQueue.TryDequeue(out (SceneBuilderTexture, int) work))
            {
                SceneBuilderTexture tex = work.Item1;

                try
                {
                    _textures[work.Item2] = CreateTexture(tex);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create texture {tex.Name} because {e}");
                    _textures[work.Item2] = Texture2D.redTexture;
                }
                finally
                {
                    tex.Data.Dispose();
                }
            }
        }

        private static Mesh CreateMesh(SceneBuilderMesh sbMesh)
        {
            Mesh mesh = new();

            mesh.SetVertexBufferParams(sbMesh.VertexCount, sbMesh.VertexDescriptors);
            MeshHelper.SetVertexBufferData(mesh, sbMesh.VertexStream, sbMesh.VertexCount, sbMesh.VertexSize);

            mesh.SetIndexBufferParams(sbMesh.IndexCount, sbMesh.IndexFormat);

            if (sbMesh.IndexFormat == IndexFormat.UInt16)
            {
                mesh.SetIndexBufferData(sbMesh.IndexData16, 0, 0, sbMesh.IndexCount);
            }
            else
            {
                mesh.SetIndexBufferData(sbMesh.IndexData32, 0, 0, sbMesh.IndexCount);
            }

            mesh.SetSubMeshes(sbMesh.SubMeshDescriptors);

            return mesh;
        }

        private static Material CreateMaterial(SceneBuilderMaterial sbMat)
        {
            Shader shader = Shader.Find(sbMat.ShaderName);

            if (shader == null)
            {
                throw new($"Couldn't resolve shader {sbMat.ShaderName} on material {sbMat.Name}");
            }

            Material mat = new(shader)
            {
                renderQueue = sbMat.CustomRenderQueue,
                doubleSidedGI = sbMat.DoubleSidedGI,
                enableInstancing = sbMat.EnableInstancingVariants
            };

            foreach ((string name, Color color) in sbMat.Colors)
            {
                mat.SetColor(name, color);
            }

            foreach ((string name, float flt) in sbMat.Floats)
            {
                mat.SetFloat(name, flt);
            }

            foreach ((string name, int integer) in sbMat.Ints)
            {
                mat.SetInteger(name, integer);
            }

            // textures set later

            foreach (string pass in sbMat.DisabledShaderPasses)
            {
                mat.SetShaderPassEnabled(pass, false);
            }

            foreach (string kw in sbMat.InvalidKeywords)
            {
                mat.DisableKeyword(kw);
            }

            foreach (string kw in sbMat.ValidKeywords)
            {
                mat.EnableKeyword(kw);
            }

            foreach ((string name, string tag) in sbMat.StringTagMap)
            {
                mat.SetOverrideTag(name, tag);
            }

            return mat;
        }

        private static Texture2D CreateTexture(SceneBuilderTexture sbTex)
        {
            Texture2D tex = new(sbTex.Width, sbTex.Height, sbTex.Format, true)
            {
                filterMode = (FilterMode)sbTex.FilterMode,
                anisoLevel = sbTex.Aniso,
                mipMapBias = sbTex.MipBias,
                wrapModeU = (TextureWrapMode)sbTex.WrapU,
                wrapModeV = (TextureWrapMode)sbTex.WrapV
            };

            tex.LoadRawTextureData(sbTex.Data);
            tex.Apply(true, true);
            return tex;
        }
    }
}
