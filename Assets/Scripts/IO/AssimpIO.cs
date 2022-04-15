/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace VRtist
{
    public class AssimpIO : MonoBehaviour
    {
        bool blocking = false;

        private Assimp.Scene scene;
        private string directoryName;
        private List<Material> materials = new List<Material>();
        private List<SubMeshComponent> meshes = new List<SubMeshComponent>();
        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        private Dictionary<Assimp.Node, GameObject> delayedMesh = new Dictionary<Assimp.Node, GameObject>();


        private Transform rootBone;
        private bool isHuman;
        private Vector3 meshCenter;
        private Vector3 meshSize;
        private SkinnedMeshRenderer bodyMesh;
        private int importCount;

        public RigConfiguration rigConfiguration;

        // We consider that half of the total time is spent inside the assimp engine
        // A quarter of the total time is necessary to create meshes
        // The remaining quarter is for hierarchy creation
        private float progress = 1f;
        public float Progress
        {
            get { return progress; }
        }

        private class SubMeshComponent
        {
            public Mesh mesh;
            public string name;
            public int materialIndex;
        }
        private struct ImportTaskData
        {
            public string fileName;
            public Transform root;
        }

        List<ImportTaskData> taskData = new List<ImportTaskData>();
        Task<Assimp.Scene> currentTask = null;
        bool unityDataInCoroutineCreated = false;

        public class ImportTaskEventArgs : EventArgs
        {
            public ImportTaskEventArgs(Transform r, string fn, bool e)
            {
                data.root = r;
                data.fileName = fn;
                error = e;
            }
            public bool Error
            {
                get { return error; }
            }

            public string Filename
            {
                get { return data.fileName; }
            }
            public Transform Root
            {
                get { return data.root; }
            }
            ImportTaskData data = new ImportTaskData();
            bool error = false;
        }

        public event EventHandler<ImportTaskEventArgs> importEventTask;

        enum ImporterState
        {
            Ready,
            Initialized,
            Processing,
            Error,
        };

        ImporterState importerState = ImporterState.Ready;

        public void Import(string fileName, Transform root, bool synchronous = false)
        {
            blocking = synchronous;
            if (synchronous)
            {
                Assimp.AssimpContext ctx = new Assimp.AssimpContext();
                var aScene = ctx.ImportFile(fileName,
                    Assimp.PostProcessSteps.Triangulate |
                    Assimp.PostProcessSteps.GenerateNormals |
                    Assimp.PostProcessSteps.GenerateUVCoords);
                CreateUnityDataFromAssimp(fileName, aScene, root).MoveNext();
                Clear();
                progress = 1.0f;
            }
            else
            {
                unityDataInCoroutineCreated = false;
                ImportTaskData d = new ImportTaskData();
                d.fileName = fileName;
                d.root = root;
                taskData.Add(d);
            }
        }

        void Update()
        {
            switch (importerState)
            {
                case ImporterState.Ready:
                    if (taskData.Count > 0)
                    {
                        // Assimp loading
                        ImportTaskData d = taskData[0];
                        currentTask = Task.Run(async () => await ImportAssimpFile(d.fileName));
                        importerState = ImporterState.Initialized;
                        progress = 0f;
                    }
                    break;

                case ImporterState.Initialized:
                    if (currentTask.IsCompleted)
                    {
                        // Convert assimp structures into unity
                        if (!currentTask.IsFaulted)
                        {
                            var scene = currentTask.Result;
                            if (scene == null)
                            {
                                importerState = ImporterState.Error;
                                break;
                            }
                            ImportTaskData d = taskData[0];
                            StartCoroutine(CreateUnityDataFromAssimp(d.fileName, scene, d.root.transform));
                            importerState = ImporterState.Processing;
                            progress = 0.5f;
                        }
                        else
                        {
                            importerState = ImporterState.Error;
                        }
                    }
                    break;

                case ImporterState.Error:
                    {
                        var tdata = taskData[0];
                        taskData.RemoveAt(0);
                        currentTask = null;
                        Clear();
                        importerState = ImporterState.Ready;
                        ImportTaskEventArgs args = new ImportTaskEventArgs(null, tdata.fileName, true);
                        progress = 1f;
                        importEventTask.Invoke(this, args);
                    }
                    break;

                case ImporterState.Processing:
                    if (unityDataInCoroutineCreated)
                    {
                        // task done
                        var tdata = taskData[0];
                        taskData.RemoveAt(0);
                        currentTask = null;
                        unityDataInCoroutineCreated = false;
                        Clear();
                        importerState = ImporterState.Ready;

                        Transform root = tdata.root.transform.GetChild(tdata.root.transform.childCount - 1);
                        ImportTaskEventArgs args = new ImportTaskEventArgs(root, tdata.fileName, false);
                        progress = 1f;
                        importEventTask.Invoke(this, args);
                    }
                    break;
            }
        }

        private void Clear()
        {
            scene = null;
            materials = new List<Material>();
            meshes = new List<SubMeshComponent>();
            bones = new Dictionary<string, Transform>();
            delayedMesh = new Dictionary<Assimp.Node, GameObject>();
            meshCenter = Vector3.zero;
            meshSize = Vector3.zero;

            //textures = new Dictionary<string, Texture2D>();   
        }

        private SubMeshComponent ImportMesh(Assimp.Mesh assimpMesh)
        {
            int i;

            Vector3[] vertices = new Vector3[assimpMesh.VertexCount];
            Vector2[][] uv = new Vector2[assimpMesh.TextureCoordinateChannelCount][];
            for (i = 0; i < assimpMesh.TextureCoordinateChannelCount; i++)
            {
                uv[i] = new Vector2[assimpMesh.VertexCount];
            }
            Vector3[] normals = new Vector3[assimpMesh.VertexCount];
            int[] triangles = new int[assimpMesh.FaceCount * 3];
            Vector4[] tangents = null;
            Color[] vertexColors = null;

            i = 0;
            foreach (Assimp.Vector3D v in assimpMesh.Vertices)
            {
                vertices[i].x = v.X;
                vertices[i].y = v.Y;
                vertices[i].z = v.Z;
                i++;
            }

            for (int UVlayer = 0; UVlayer < assimpMesh.TextureCoordinateChannelCount; UVlayer++)
            {
                i = 0;
                foreach (Assimp.Vector3D UV in assimpMesh.TextureCoordinateChannels[UVlayer])
                {
                    uv[UVlayer][i].x = UV.X;
                    uv[UVlayer][i].y = UV.Y;
                    i++;
                }
            }

            i = 0;
            foreach (Assimp.Vector3D n in assimpMesh.Normals)
            {
                normals[i].x = n.X;
                normals[i].y = n.Y;
                normals[i].z = n.Z;
                i++;
            }

            if (assimpMesh.HasTangentBasis)
            {
                i = 0;
                tangents = new Vector4[assimpMesh.VertexCount];
                foreach (Assimp.Vector3D t in assimpMesh.Tangents)
                {
                    tangents[i].x = t.X;
                    tangents[i].y = t.Y;
                    tangents[i].z = t.Z;
                    tangents[i].w = 1f;
                    i++;
                }
            }

            if (assimpMesh.VertexColorChannelCount >= 1)
            {
                i = 0;
                vertexColors = new Color[assimpMesh.VertexCount];
                foreach (Assimp.Color4D c in assimpMesh.VertexColorChannels[0])
                {
                    vertexColors[i].r = c.R;
                    vertexColors[i].g = c.G;
                    vertexColors[i].b = c.B;
                    vertexColors[i].a = c.A;
                    i++;
                }
            }

            i = 0;
            foreach (Assimp.Face face in assimpMesh.Faces)
            {
                triangles[i + 0] = face.Indices[0];
                triangles[i + 1] = face.Indices[1];
                triangles[i + 2] = face.Indices[2];
                i += 3;
            }

            SubMeshComponent subMeshComponent = new SubMeshComponent();
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            if (assimpMesh.TextureCoordinateChannelCount > 0)
                mesh.uv = uv[0];
            if (assimpMesh.TextureCoordinateChannelCount > 1)
                mesh.uv2 = uv[1];
            if (assimpMesh.TextureCoordinateChannelCount > 2)
                mesh.uv3 = uv[2];
            if (assimpMesh.TextureCoordinateChannelCount > 3)
                mesh.uv4 = uv[3];
            if (assimpMesh.TextureCoordinateChannelCount > 4)
                mesh.uv5 = uv[4];
            if (assimpMesh.TextureCoordinateChannelCount > 5)
                mesh.uv6 = uv[5];
            if (assimpMesh.TextureCoordinateChannelCount > 6)
                mesh.uv7 = uv[6];
            if (assimpMesh.TextureCoordinateChannelCount > 7)
                mesh.uv8 = uv[7];
            mesh.normals = normals;
            if (tangents != null)
                mesh.tangents = tangents;
            if (vertexColors != null)
                mesh.colors = vertexColors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            subMeshComponent.mesh = mesh;
            subMeshComponent.name = Utils.CreateUniqueName(assimpMesh.Name);
            subMeshComponent.materialIndex = assimpMesh.MaterialIndex;

            return subMeshComponent;
        }

        private IEnumerator ImportMeshes()
        {
            int i = 0;
            foreach (Assimp.Mesh assimpMesh in scene.Meshes)
            {
                GlobalState.Instance.messageBox.ShowMessage("Importing Meshes : " + i + " / " + scene.MeshCount);
                if (assimpMesh.HasBones) isHuman = true;

                SubMeshComponent subMeshComponent = ImportMesh(assimpMesh);
                meshes.Add(subMeshComponent);
                i++;

                progress += 0.25f / scene.MeshCount;

                if (isHuman)
                {
                    foreach (Assimp.Bone bone in assimpMesh.Bones)
                    {
                        if (!bones.ContainsKey(bone.Name)) bones.Add(bone.Name, null);
                    }
                }

                if (!blocking)
                    yield return null;
            }

        }

        private Texture2D GetOrCreateTextureFromFile(string filename)
        {
            CultureInfo ci = new CultureInfo("en-US");
            if (!filename.EndsWith(".jpg", false, ci) &&
                !filename.EndsWith(".png", false, ci) &&
                !filename.EndsWith(".exr", false, ci) &&
                !filename.EndsWith(".tga", false, ci))
                return null;

            Texture2D texture;
            if (textures.TryGetValue(filename, out texture))
                return texture;

            byte[] bytes = System.IO.File.ReadAllBytes(filename);
            texture = new Texture2D(1, 1);
            texture.LoadImage(bytes);
            textures[filename] = texture;
            return texture;
        }

        private IEnumerator ImportMaterials()
        {
            int i = 0;
            Material opaqueMat = Resources.Load<Material>("Materials/ObjectOpaque");
            Material transpMat = Resources.Load<Material>("Materials/ObjectTransparent");
            foreach (Assimp.Material assimpMaterial in scene.Materials)
            {
                GlobalState.Instance.messageBox.ShowMessage("Importing Materials : " + i + " / " + scene.MaterialCount);
                if (assimpMaterial.HasOpacity && !assimpMaterial.HasColorTransparent)
                {
                    materials.Add(new Material(transpMat));
                }
                else
                {
                    materials.Add(new Material(opaqueMat));
                }

                var material = materials[i];
                material.enableInstancing = true;

                material.SetFloat("_Metallic", 0.0f);
                material.SetFloat("_Roughness", 0.8f);

                if (assimpMaterial.IsTwoSided)
                {
                    // does not work...
                    material.SetInt("_DoubleSidedEnable", 1);
                    material.EnableKeyword("_DOUBLESIDED_ON");
                }

                material.name = assimpMaterial.Name;
                if (assimpMaterial.HasColorDiffuse)
                {
                    Color baseColor = new Color(assimpMaterial.ColorDiffuse.R, assimpMaterial.ColorDiffuse.G, assimpMaterial.ColorDiffuse.B, assimpMaterial.ColorDiffuse.A);
                    material.SetColor("_BaseColor", baseColor);
                }
                if (assimpMaterial.HasTextureOpacity)
                {
                    Assimp.TextureSlot tslot = assimpMaterial.TextureOpacity;
                    GetTexture(tslot, out Texture2D texture, out float useMap);
                    material.SetFloat("_UseOpacityMap", useMap);
                    material.SetTexture("_OpacityMap", texture);
                }
                if (assimpMaterial.HasOpacity && assimpMaterial.Opacity < 1.0f)
                {
                    material.SetFloat("_Opacity", assimpMaterial.Opacity);
                }
                if (assimpMaterial.HasTextureDiffuse)
                {
                    //Assimp.TextureSlot[] slots = assimpMaterial.GetAllMaterialTextures();
                    Assimp.TextureSlot tslot = assimpMaterial.TextureDiffuse;
                    GetTexture(tslot, out Texture2D texture, out float useMap);
                    material.SetFloat("_UseColorMap", useMap);
                    material.SetTexture("_ColorMap", texture);
                }
                if (assimpMaterial.HasTextureNormal)
                {
                    Assimp.TextureSlot tslot = assimpMaterial.TextureNormal;
                    GetTexture(tslot, out Texture2D texture, out float useMap);
                    material.SetFloat("_UseNormalMap", useMap);
                    material.SetTexture("_NormalMap", texture);
                }
                if (assimpMaterial.HasTextureEmissive)
                {
                    Assimp.TextureSlot tslot = assimpMaterial.TextureEmissive;
                    GetTexture(tslot, out Texture2D texture, out float useMap);
                    material.SetFloat("_UseEmissiveMap", useMap);
                    material.SetTexture("_EmissiveMap", texture);
                }
                i++;

                if (!blocking)
                    yield return null;
            }
        }

        private void GetTexture(Assimp.TextureSlot tslot, out Texture2D texture, out float useColorMap)
        {
            string fullpath = Path.IsPathRooted(tslot.FilePath) ? tslot.FilePath : directoryName + "\\" + tslot.FilePath;
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, 0, true);
            useColorMap = 0f;
            if (File.Exists(fullpath))
            {
                texture = GetOrCreateTextureFromFile(fullpath);
                useColorMap = 1f;
            }
            else
            {
                List<Assimp.EmbeddedTexture> texts = scene.Textures;
                Assimp.EmbeddedTexture diffuseTexture = texts.Find(x => x.Filename == tslot.FilePath);
                if (diffuseTexture != null)
                {
                    byte[] data = diffuseTexture.CompressedData;
                    texture.LoadImage(data);
                    useColorMap = 1f;
                }
            }
        }

        private void AssignMeshes(Assimp.Node node, GameObject parent)
        {
            if (node.MeshIndices.Count == 0)
                return;

            if (scene.Meshes[node.MeshIndices[0]].HasBones)
            {
                delayedMesh.Add(node, parent);
                return;
            }

            Material[] mats = new Material[node.MeshIndices.Count];
            CombineInstance[] combine = new CombineInstance[node.MeshIndices.Count];

            int i = 0;
            foreach (int indice in node.MeshIndices)
            {
                combine[i].mesh = meshes[indice].mesh;
                combine[i].transform = Matrix4x4.identity;
                mats[i] = materials[meshes[indice].materialIndex];
                i++;
            }
            AddSimpleMesh(node, parent, mats, combine);

            progress += (0.25f * node.MeshIndices.Count) / scene.MeshCount;
        }

        private void AssignSkinnedMeshes(Assimp.Node node, GameObject parent)
        {
            Material[] mats = new Material[node.MeshIndices.Count];
            CombineInstance[] combine = new CombineInstance[node.MeshIndices.Count];

            int i = 0;
            foreach (int indice in node.MeshIndices)
            {
                combine[i].mesh = meshes[indice].mesh;
                combine[i].transform = parent.transform.worldToLocalMatrix;
                mats[i] = materials[meshes[indice].materialIndex];
                i++;
            }
            AddSkinnedMesh(node, parent, mats, combine);
            progress += (0.25f * node.MeshIndices.Count) / scene.MeshCount;
        }

        private void AddSkinnedMesh(Assimp.Node node, GameObject parent, Material[] mats, CombineInstance[] combine)
        {
            SkinnedMeshRenderer meshRenderer = parent.AddComponent<SkinnedMeshRenderer>();
            List<List<Assimp.Bone>> meshBones = new List<List<Assimp.Bone>>();
            Dictionary<int, List<BoneWeight1>> VertexBonesWeights = new Dictionary<int, List<BoneWeight1>>();
            List<Transform[]> bonesArray = new List<Transform[]>();
            List<Matrix4x4[]> bindPoses = new List<Matrix4x4[]>();
            //Debug.Log(node.Name + " / " + parent.name);

            int previousVertexCount = 0;
            for (int iMesh = 0; iMesh < node.MeshIndices.Count; iMesh++)
            {
                Assimp.Mesh currentMesh = scene.Meshes[node.MeshIndices[iMesh]];
                meshBones.Add(currentMesh.Bones);
                bonesArray.Add(new Transform[currentMesh.BoneCount]);
                bindPoses.Add(new Matrix4x4[currentMesh.BoneCount]);

                for (int iBone = 0; iBone < bonesArray[iMesh].Length; iBone++)
                {
                    Assimp.Bone currentBones = meshBones[iMesh][iBone];
                    if (!bones.ContainsKey(currentBones.Name))
                    {
                        Debug.Log("missing bone " + currentBones.Name);
                        continue;
                    }
                    bonesArray[iMesh][iBone] = bones[currentBones.Name];
                    bindPoses[iMesh][iBone] = bonesArray[iMesh][iBone].worldToLocalMatrix;

                    for (int iVertex = 0; iVertex < currentBones.VertexWeightCount; iVertex++)
                    {
                        int vertexIndex = currentBones.VertexWeights[iVertex].VertexID + previousVertexCount;
                        float weight = currentBones.VertexWeights[iVertex].Weight;
                        BoneWeight1 boneWeight = new BoneWeight1() { boneIndex = iBone, weight = weight };
                        if (!VertexBonesWeights.ContainsKey(vertexIndex)) VertexBonesWeights.Add(vertexIndex, new List<BoneWeight1>());
                        VertexBonesWeights[vertexIndex].Add(boneWeight);
                    }
                }
                previousVertexCount += currentMesh.VertexCount;
            }

            List<Assimp.Bone> meshBonesFlat = new List<Assimp.Bone>();
            meshBones.ForEach(x => meshBonesFlat.AddRange(x));
            List<Transform> bonesArrayFlat = new List<Transform>();
            bonesArray.ForEach(x => bonesArrayFlat.AddRange(x));
            List<Matrix4x4> bindPosesFlat = new List<Matrix4x4>();
            bindPoses.ForEach(x => bindPosesFlat.AddRange(x));
            List<BoneWeight> bonesWeightsFlat = new List<BoneWeight>();
            byte[] bonesPerVertes = new byte[VertexBonesWeights.Count];
            List<BoneWeight1> bw = new List<BoneWeight1>();
            for (int i = 0; i < VertexBonesWeights.Count; i++)
            {
                bonesPerVertes[i] = (byte)VertexBonesWeights[i].Count;
                VertexBonesWeights[i].Sort((x, y) =>
                {
                    if (x == null)
                    {
                        if (y == null) return 0;
                        else return -1;
                    }
                    else
                    {
                        if (y == null) return 1;
                        else return -x.weight.CompareTo(y.weight);
                    }
                });
                bw.AddRange(VertexBonesWeights[i]);
            }

            meshRenderer.bones = bonesArrayFlat.ToArray();
            meshRenderer.sharedMesh = new Mesh();
            meshRenderer.sharedMesh.bindposes = bindPosesFlat.ToArray();
            meshRenderer.sharedMesh.CombineMeshes(combine, false);
            meshRenderer.sharedMesh.SetBoneWeights(new NativeArray<byte>(bonesPerVertes, Allocator.Temp), new NativeArray<BoneWeight1>(bw.ToArray(), Allocator.Temp));
            meshRenderer.sharedMesh.name = meshes[node.MeshIndices[0]].name;
            meshRenderer.sharedMaterials = mats;
            meshRenderer.rootBone = rootBone;
            if (meshRenderer.sharedMesh.bounds.size.magnitude > meshSize.magnitude)
            {
                meshCenter = meshRenderer.bounds.center;
                meshSize = meshRenderer.bounds.size;
                bodyMesh = meshRenderer;
            }
        }

        private void AddSimpleMesh(Assimp.Node node, GameObject parent, Material[] mats, CombineInstance[] combine)
        {
            MeshFilter meshFilter = parent.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = parent.AddComponent<MeshRenderer>();

            meshFilter.mesh = new Mesh();
            meshFilter.mesh.CombineMeshes(combine, false);
            meshFilter.name = meshes[node.MeshIndices[0]].name;
            meshFilter.mesh.name = meshFilter.name;
            meshRenderer.sharedMaterials = mats;
            MeshCollider collider = parent.AddComponent<MeshCollider>();
        }


        private IEnumerator ImportHierarchy(Assimp.Node node, Transform parent, GameObject go, Matrix4x4 cumulMatrix, Quaternion preRotation)
        {
            if (parent != null && parent != go.transform)
                go.transform.parent = parent;

            GlobalState.Instance.messageBox.ShowMessage("Importing Hierarchy : " + importCount);
            // Do not use Assimp Decompose function, it does not work properly
            // use unity decomposition instead
            Matrix4x4 nodeMatrix = new Matrix4x4(
                new Vector4(node.Transform.A1, node.Transform.B1, node.Transform.C1, node.Transform.D1),
                new Vector4(node.Transform.A2, node.Transform.B2, node.Transform.C2, node.Transform.D2),
                new Vector4(node.Transform.A3, node.Transform.B3, node.Transform.C3, node.Transform.D3),
                new Vector4(node.Transform.A4, node.Transform.B4, node.Transform.C4, node.Transform.D4)
                );

            Maths.DecomposeMatrix(nodeMatrix, out Vector3 nodePosition, out Quaternion nodeRotation, out Vector3 nodeScale);
            Maths.DecomposeMatrix(cumulMatrix, out Vector3 cumulPosition, out Quaternion cumulRotation, out Vector3 cumulScale);

            if (node.Name.Contains("$AssimpFbx$") && node.HasChildren)
            {
                if (node.Name.Contains("Translation"))
                {
                    nodePosition = cumulRotation * nodePosition;
                }
                else
                {
                    nodePosition += cumulPosition;
                }
                if (node.Name.Contains("PreRotation"))
                {
                    preRotation = nodeRotation;
                    nodeRotation = cumulRotation * nodeRotation;
                }
                else
                {
                    nodeRotation = cumulRotation;
                }
                cumulMatrix = Matrix4x4.TRS(nodePosition, nodeRotation, nodeScale);
                if (blocking)
                    ImportHierarchy(node.Children[0], parent, go, cumulMatrix, preRotation).MoveNext();
                else
                    yield return StartCoroutine(ImportHierarchy(node.Children[0], parent, go, cumulMatrix, preRotation));
            }
            else
            {
                nodePosition = (cumulRotation * nodePosition) + cumulPosition;
                node.Transform.Decompose(out Assimp.Vector3D scale1, out Assimp.Quaternion rot, out Assimp.Vector3D trans);
                AssignMeshes(node, go);

                if (node.Parent != null)
                {
                    go.transform.localPosition = nodePosition;
                    go.transform.localRotation = nodeRotation;
                    go.transform.localScale = nodeScale;
                    go.name = isHuman ? node.Name : Utils.CreateUniqueName(node.Name);
                    if (isHuman)
                    {
                        if (bones.ContainsKey(node.Name))
                        {
                            bones[node.Name] = go.transform;
                        }
                        if (node.Name.Contains("Hips")) rootBone = go.transform;
                    }
                }


                if (scene.HasAnimations)
                {
                    ImportAnimation(node, go, cumulRotation);
                }

                nodeMatrix = Matrix4x4.TRS(Vector3.zero, cumulRotation * nodeRotation, nodeScale);
                importCount++;
                foreach (Assimp.Node assimpChild in node.Children)
                {
                    GameObject child = new GameObject();
                    if (blocking)
                        ImportHierarchy(assimpChild, go.transform, child, nodeMatrix, Quaternion.identity).MoveNext();
                    else
                        yield return StartCoroutine(ImportHierarchy(assimpChild, go.transform, child, nodeMatrix, Quaternion.identity));
                }
            }
        }

        private IEnumerator ImportHierarchy(Assimp.Node node, Transform parent, GameObject go)
        {
            if (parent != null && parent != go.transform)
                go.transform.parent = parent;

            GlobalState.Instance.messageBox.ShowMessage("Importing Hierarchy : " + importCount);
            // Do not use Assimp Decompose function, it does not work properly
            // use unity decomposition instead
            Matrix4x4 mat = new Matrix4x4(
                new Vector4(node.Transform.A1, node.Transform.B1, node.Transform.C1, node.Transform.D1),
                new Vector4(node.Transform.A2, node.Transform.B2, node.Transform.C2, node.Transform.D2),
                new Vector4(node.Transform.A3, node.Transform.B3, node.Transform.C3, node.Transform.D3),
                new Vector4(node.Transform.A4, node.Transform.B4, node.Transform.C4, node.Transform.D4)
                );
            Vector3 position, scale;
            Quaternion rotation;
            Maths.DecomposeMatrix(mat, out position, out rotation, out scale);

            node.Transform.Decompose(out Assimp.Vector3D scale1, out Assimp.Quaternion rot, out Assimp.Vector3D trans);

            AssignMeshes(node, go);

            if (node.Parent != null)
            {
                go.transform.localPosition = position;
                go.transform.localRotation = rotation;
                go.transform.localScale = scale;
                go.name = isHuman ? node.Name : Utils.CreateUniqueName(node.Name);
                if (isHuman)
                {
                    if (bones.ContainsKey(node.Name))
                    {
                        bones[node.Name] = go.transform;
                    }
                    if (node.Name.Contains("Hips")) rootBone = go.transform;
                }
            }


            if (scene.HasAnimations)
            {
                ImportAnimation(node, go, Quaternion.identity);
            }

            importCount++;
            foreach (Assimp.Node assimpChild in node.Children)
            {
                GameObject child = new GameObject();
                if (blocking)
                    ImportHierarchy(assimpChild, go.transform, child).MoveNext();
                else
                    yield return StartCoroutine(ImportHierarchy(assimpChild, go.transform, child));
            }
        }


        private void ImportAnimation(Assimp.Node node, GameObject go, Quaternion cumulRotation)
        {
            Assimp.Animation animation = scene.Animations[0];
            if ((GlobalState.Animation.fps / (float)animation.TicksPerSecond) * animation.DurationInTicks > GlobalState.Animation.EndFrame)
            {
                GlobalState.Animation.EndFrame = Mathf.CeilToInt(GlobalState.Animation.fps / (float)animation.TicksPerSecond * (float)animation.DurationInTicks) + 1;
            }
            Assimp.NodeAnimationChannel nodeChannel = animation.NodeAnimationChannels.Find(x => x.NodeName == node.Name);
            if (nodeChannel == null) nodeChannel = animation.NodeAnimationChannels.Find(x => x.NodeName.Split('_')[0] == node.Name);
            if (null != nodeChannel)
            {
                if (nodeChannel.PositionKeyCount < 2 && nodeChannel.RotationKeyCount < 2) return;
                AnimationSet animationSet = new AnimationSet(go);
                animationSet.ComputeCache();
                if (nodeChannel.PositionKeyCount > 1 || nodeChannel.RotationKeyCount == nodeChannel.PositionKeyCount)
                {
                    foreach (Assimp.VectorKey vectorKey in nodeChannel.PositionKeys)
                    {
                        int frame = Mathf.CeilToInt((float)vectorKey.Time * GlobalState.Animation.fps / (float)animation.TicksPerSecond) + 1;
                        animationSet.curves[AnimatableProperty.PositionX].AddKey(new AnimationKey(frame, vectorKey.Value.X, Interpolation.Bezier));
                        animationSet.curves[AnimatableProperty.PositionY].AddKey(new AnimationKey(frame, vectorKey.Value.Y, Interpolation.Bezier));
                        animationSet.curves[AnimatableProperty.PositionZ].AddKey(new AnimationKey(frame, vectorKey.Value.Z, Interpolation.Bezier));
                    }
                }
                foreach (Assimp.VectorKey vectorKey in nodeChannel.ScalingKeys)
                {
                    int frame = Mathf.CeilToInt((float)vectorKey.Time * GlobalState.Animation.fps / (float)animation.TicksPerSecond) + 1;
                    animationSet.curves[AnimatableProperty.ScaleX].AddKey(new AnimationKey(frame, vectorKey.Value.X, Interpolation.Bezier));
                    animationSet.curves[AnimatableProperty.ScaleY].AddKey(new AnimationKey(frame, vectorKey.Value.Y, Interpolation.Bezier));
                    animationSet.curves[AnimatableProperty.ScaleZ].AddKey(new AnimationKey(frame, vectorKey.Value.Z, Interpolation.Bezier));
                }
                Vector3 previousRotation = Vector3.zero;
                foreach (Assimp.QuaternionKey quaternionKey in nodeChannel.RotationKeys)
                {
                    int frame = Mathf.RoundToInt((float)quaternionKey.Time * GlobalState.Animation.fps / (float)animation.TicksPerSecond) + 1;
                    Quaternion uQuaternion = new Quaternion(quaternionKey.Value.X, quaternionKey.Value.Y, quaternionKey.Value.Z, quaternionKey.Value.W);
                    uQuaternion = cumulRotation * uQuaternion * Quaternion.Inverse(cumulRotation);

                    Vector3 eulerValue = uQuaternion.eulerAngles;
                    eulerValue.x = previousRotation.x + Mathf.DeltaAngle(previousRotation.x, eulerValue.x);
                    eulerValue.y = previousRotation.y + Mathf.DeltaAngle(previousRotation.y, eulerValue.y);
                    eulerValue.z = previousRotation.z + Mathf.DeltaAngle(previousRotation.z, eulerValue.z);
                    animationSet.curves[AnimatableProperty.RotationX].AddKey(new AnimationKey(frame, eulerValue.x, Interpolation.Bezier));
                    animationSet.curves[AnimatableProperty.RotationY].AddKey(new AnimationKey(frame, eulerValue.y, Interpolation.Bezier));
                    animationSet.curves[AnimatableProperty.RotationZ].AddKey(new AnimationKey(frame, eulerValue.z, Interpolation.Bezier));
                    previousRotation = eulerValue;
                }
                GlobalState.Animation.SetObjectAnimations(go, animationSet);
            }
        }


        private IEnumerator ImportScene(string fileName, Transform root = null)
        {

            if (blocking)
                ImportMaterials().MoveNext();
            else
                yield return StartCoroutine(ImportMaterials());

            if (blocking)
                ImportMeshes().MoveNext();
            else
                yield return StartCoroutine(ImportMeshes());

            GameObject objectRoot = root.gameObject;

            objectRoot = new GameObject();
            // Right handed to Left Handed
            objectRoot.name = Utils.CreateUniqueName(Path.GetFileNameWithoutExtension(fileName));
            objectRoot.transform.parent = root;
            objectRoot.transform.localPosition = Vector3.zero;
            if (!isHuman)
            {
                objectRoot.transform.localScale = new Vector3(-1, 1, 1);
                objectRoot.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }

            importCount = 1;
            if (blocking)
                ImportHierarchy(scene.RootNode, root, objectRoot, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), Quaternion.identity).MoveNext();
            else
                yield return StartCoroutine(ImportHierarchy(scene.RootNode, root, objectRoot, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), Quaternion.identity));

            //if (blocking)
            //    ImportHierarchy(scene.RootNode, root, objectRoot).MoveNext();
            //else
            //    yield return StartCoroutine(ImportHierarchy(scene.RootNode, root, objectRoot));

            if (null == rootBone)
            {
                foreach (Transform child in objectRoot.transform)
                {
                    if (child.childCount > 0) rootBone = child;
                }
            }

            foreach (KeyValuePair<Assimp.Node, GameObject> pair in delayedMesh)
            {
                AssignSkinnedMeshes(pair.Key, pair.Value);
            }

            if (isHuman)
            {
                objectRoot.tag = "PhysicObject";
                BoxCollider objectCollider = objectRoot.AddComponent<BoxCollider>();
                objectCollider.center = meshCenter;
                objectCollider.size = meshSize;
                bodyMesh.updateWhenOffscreen = true;

                RigController skinMesh = objectRoot.AddComponent<RigController>();
                skinMesh.SkinMesh = bodyMesh;
                skinMesh.Collider = objectCollider;
                skinMesh.RootObject = rootBone;

                GenerateSkeleton(rootBone, skinMesh);
            }

        }

        public void GenerateSkeleton(Transform root, RigController rootController)
        {
            rigConfiguration.GenerateGoalController(rootController, root, new List<Transform>());
        }


        private async Task<Assimp.Scene> ImportAssimpFile(string fileName)
        {
            Assimp.Scene aScene = null;
            await Task<Assimp.Scene>.Run(() =>
            {
                try
                {
                    Assimp.AssimpContext ctx = new Assimp.AssimpContext();
                    aScene = ctx.ImportFile(fileName,
                        Assimp.PostProcessSteps.Triangulate |
                        Assimp.PostProcessSteps.GenerateNormals |
                        Assimp.PostProcessSteps.GenerateUVCoords);
                }
                catch (Assimp.AssimpException e)
                {
                    Debug.LogError(e.Message);
                    aScene = null;
                }
            });
            return aScene;
        }

        private IEnumerator CreateUnityDataFromAssimp(string fileName, Assimp.Scene aScene, Transform root)
        {
            scene = aScene;
            directoryName = Path.GetDirectoryName(fileName);

            if (blocking)
            {
                ImportScene(fileName, root).MoveNext();
            }
            else
            {
                yield return StartCoroutine(ImportScene(fileName, root));
                unityDataInCoroutineCreated = true;
            }
        }
    }
}