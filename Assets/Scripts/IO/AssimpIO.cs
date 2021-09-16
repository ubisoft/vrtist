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
                if (assimpMaterial.HasOpacity && assimpMaterial.Opacity < 0.99f)
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
                if (assimpMaterial.HasOpacity && assimpMaterial.Opacity < 1.0f)
                {
                    material.SetFloat("_Opacity", assimpMaterial.Opacity);
                }
                if (assimpMaterial.HasTextureDiffuse)
                {
                    Assimp.TextureSlot tslot = assimpMaterial.TextureDiffuse;
                    string fullpath = Path.IsPathRooted(tslot.FilePath) ? tslot.FilePath : directoryName + "\\" + tslot.FilePath;
                    if (File.Exists(fullpath))
                    {
                        Texture2D texture = GetOrCreateTextureFromFile(fullpath);
                        material.SetFloat("_UseColorMap", 1f);
                        material.SetTexture("_ColorMap", texture);
                    }
                    else
                    {
                        List<Assimp.EmbeddedTexture> texts = scene.Textures;
                        if (texts.Count > 0)
                        {
                            byte[] data = texts[0].CompressedData;
                            Texture2D tex = new Texture2D(1, 1);
                            tex.LoadImage(data);
                            material.SetFloat("_UseColorMap", 1f);
                            material.SetTexture("_ColorMap", tex);
                        }

                    }
                }
                i++;

                if (!blocking)
                    yield return null;
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
            AddNormalMesh(node, parent, mats, combine);

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
                combine[i].transform = Matrix4x4.identity;
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
            List<Transform[]> bonesArray = new List<Transform[]>();
            List<Matrix4x4[]> bindPoses = new List<Matrix4x4[]>();
            List<BoneWeight[]> bonesWeights = new List<BoneWeight[]>();

            for (int iMesh = 0; iMesh < node.MeshIndices.Count; iMesh++)
            {
                Assimp.Mesh currentMesh = scene.Meshes[node.MeshIndices[iMesh]];
                meshBones.Add(currentMesh.Bones);
                bonesArray.Add(new Transform[currentMesh.BoneCount]);
                bindPoses.Add(new Matrix4x4[currentMesh.BoneCount]);
                bonesWeights.Add(new BoneWeight[currentMesh.VertexCount]);

                for (int iBone = 0; iBone < bonesArray[iMesh].Length; iBone++)
                {
                    if (!bones.ContainsKey(meshBones[iMesh][iBone].Name))
                    {
                        Debug.Log("missing bone " + meshBones[iMesh][iBone].Name);
                        continue;
                    }
                    bonesArray[iMesh][iBone] = bones[meshBones[iMesh][iBone].Name];
                    bindPoses[iMesh][iBone] = bonesArray[iMesh][iBone].worldToLocalMatrix;

                    for (int iVertex = 0; iVertex < meshBones[iMesh][iBone].VertexWeightCount; iVertex++)
                    {
                        int vertexIndex = meshBones[iMesh][iBone].VertexWeights[iVertex].VertexID;
                        float weight = meshBones[iMesh][iBone].VertexWeights[iVertex].Weight;
                        if (bonesWeights[iMesh][vertexIndex].weight0 <= weight)
                        {
                            bonesWeights[iMesh][vertexIndex].boneIndex1 = bonesWeights[iMesh][vertexIndex].boneIndex0;
                            bonesWeights[iMesh][vertexIndex].weight1 = bonesWeights[iMesh][vertexIndex].weight0;
                            bonesWeights[iMesh][vertexIndex].boneIndex0 = iBone;
                            bonesWeights[iMesh][vertexIndex].weight0 = weight;
                            continue;
                        }
                        if (bonesWeights[iMesh][vertexIndex].weight1 <= weight)
                        {
                            bonesWeights[iMesh][vertexIndex].boneIndex2 = bonesWeights[iMesh][vertexIndex].boneIndex1;
                            bonesWeights[iMesh][vertexIndex].weight2 = bonesWeights[iMesh][vertexIndex].weight1;
                            bonesWeights[iMesh][vertexIndex].boneIndex1 = iBone;
                            bonesWeights[iMesh][vertexIndex].weight1 = weight;
                            continue;
                        }
                        if (bonesWeights[iMesh][vertexIndex].weight2 <= weight)
                        {
                            bonesWeights[iMesh][vertexIndex].boneIndex3 = bonesWeights[iMesh][vertexIndex].boneIndex2;
                            bonesWeights[iMesh][vertexIndex].weight3 = bonesWeights[iMesh][vertexIndex].weight2;
                            bonesWeights[iMesh][vertexIndex].boneIndex2 = iBone;
                            bonesWeights[iMesh][vertexIndex].weight2 = weight;
                            continue;
                        }
                        if (bonesWeights[iMesh][vertexIndex].weight3 == 0)
                        {
                            bonesWeights[iMesh][vertexIndex].boneIndex3 = iBone;
                            bonesWeights[iMesh][vertexIndex].weight3 = weight;
                            continue;
                        }
                        else
                        {
                            Debug.Log("A vertices has more than 4 bones weight");
                            break;
                        }
                    }
                }
                for (int vert = 0; vert < bonesWeights[iMesh].Length; vert++)
                {
                    float totalWeight = bonesWeights[iMesh][vert].weight0 + bonesWeights[iMesh][vert].weight1 + bonesWeights[iMesh][vert].weight2 + bonesWeights[iMesh][vert].weight3;
                    if (totalWeight != 1)
                    {
                        bonesWeights[iMesh][vert].weight0 += (1 - totalWeight);
                    }
                }
            }

            List<Assimp.Bone> meshBonesFlat = new List<Assimp.Bone>();
            meshBones.ForEach(x => meshBonesFlat.AddRange(x));
            List<Transform> bonesArrayFlat = new List<Transform>();
            bonesArray.ForEach(x => bonesArrayFlat.AddRange(x));
            List<Matrix4x4> bindPosesFlat = new List<Matrix4x4>();
            bindPoses.ForEach(x => bindPosesFlat.AddRange(x));
            List<BoneWeight> bonesWeightsFlat = new List<BoneWeight>();
            bonesWeights.ForEach(x => bonesWeightsFlat.AddRange(x));

            meshRenderer.bones = bonesArrayFlat.ToArray();
            meshRenderer.sharedMesh = new Mesh();
            meshRenderer.sharedMesh.bindposes = bindPosesFlat.ToArray();
            meshRenderer.sharedMesh.CombineMeshes(combine, false);
            meshRenderer.sharedMesh.boneWeights = bonesWeightsFlat.ToArray();
            meshRenderer.sharedMesh.name = meshes[node.MeshIndices[0]].name;
            meshRenderer.sharedMaterials = mats;
            meshRenderer.rootBone = rootBone;
        }

        private void AddNormalMesh(Assimp.Node node, GameObject parent, Material[] mats, CombineInstance[] combine)
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


        private IEnumerator ImportHierarchy(Assimp.Node node, Transform parent, GameObject go)
        {
            if (parent != null && parent != go.transform)
                go.transform.parent = parent;


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
                if (blocking)
                {
                    ImportAnimation(node, go).MoveNext();
                }
                else
                {
                    yield return StartCoroutine(ImportAnimation(node, go));
                }
            }


            foreach (Assimp.Node assimpChild in node.Children)
            {
                GameObject child = new GameObject();
                child.tag = "PhysicObject";
                if (blocking)
                    ImportHierarchy(assimpChild, go.transform, child).MoveNext();
                else
                    yield return StartCoroutine(ImportHierarchy(assimpChild, go.transform, child));
            }

        }

        private IEnumerator ImportAnimation(Assimp.Node node, GameObject go)
        {
            Assimp.Animation animation = scene.Animations[0];
            Assimp.NodeAnimationChannel nodeChannel = animation.NodeAnimationChannels.Find(x => x.NodeName == node.Name);
            if (null != nodeChannel)
            {

                AnimationSet animationSet = new AnimationSet(go);
                animationSet.ComputeCache();
                foreach (Assimp.VectorKey vectorKey in nodeChannel.PositionKeys)
                {
                    int frame = Mathf.CeilToInt((float)vectorKey.Time * GlobalState.Animation.fps / (float)animation.TicksPerSecond) + 1;
                    animationSet.curves[AnimatableProperty.PositionX].AddKey(new AnimationKey(frame, vectorKey.Value.X));
                    animationSet.curves[AnimatableProperty.PositionY].AddKey(new AnimationKey(frame, vectorKey.Value.Y));
                    animationSet.curves[AnimatableProperty.PositionZ].AddKey(new AnimationKey(frame, vectorKey.Value.Z));
                }
                Vector3 previousRotation = Vector3.zero;
                foreach (Assimp.QuaternionKey quaternionKey in nodeChannel.RotationKeys)
                {
                    int frame = Mathf.RoundToInt((float)quaternionKey.Time * GlobalState.Animation.fps / (float)animation.TicksPerSecond) + 1;
                    Quaternion uQuaternion = new Quaternion(quaternionKey.Value.X, quaternionKey.Value.Y, quaternionKey.Value.Z, quaternionKey.Value.W);
                    Vector3 eulerValue = uQuaternion.eulerAngles;
                    eulerValue.x = previousRotation.x + Mathf.DeltaAngle(previousRotation.x, eulerValue.x);
                    eulerValue.y = previousRotation.y + Mathf.DeltaAngle(previousRotation.y, eulerValue.y);
                    eulerValue.z = previousRotation.z + Mathf.DeltaAngle(previousRotation.z, eulerValue.z);
                    animationSet.curves[AnimatableProperty.RotationX].AddKey(new AnimationKey(frame, eulerValue.x));
                    animationSet.curves[AnimatableProperty.RotationY].AddKey(new AnimationKey(frame, eulerValue.y));
                    animationSet.curves[AnimatableProperty.RotationZ].AddKey(new AnimationKey(frame, eulerValue.z));
                    previousRotation = eulerValue;
                }

                GlobalState.Animation.SetObjectAnimations(go, animationSet);
                if (!blocking)
                {
                    yield return null;
                }
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
            objectRoot.transform.localScale = new Vector3(-1, 1, 1);
            objectRoot.transform.localRotation = Quaternion.Euler(0, 180, 0);

            if (blocking)
                ImportHierarchy(scene.RootNode, root, objectRoot).MoveNext();
            else
                yield return StartCoroutine(ImportHierarchy(scene.RootNode, root, objectRoot));
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