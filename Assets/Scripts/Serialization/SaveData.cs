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
using System.Collections.Generic;

using UnityEngine;

namespace VRtist.Serialization
{
    public interface IBlob
    {
        byte[] ToBytes();
        void FromBytes(byte[] bytes, ref int index);
    }

    public class MaterialData : IBlob
    {
        string name;
        string path;  // relative path

        bool unlit;

        public bool useColorMap;
        public Color baseColor;
        public string colorMapPath;

        public bool useNormalMap;
        public string normalMapPath;

        public bool useMetallicMap;
        public float metallic;
        public string metallicMapPath;

        public bool useRoughnessMap;
        public float roughness;
        public string roughnessMapPath;

        public bool useEmissiveMap;
        public Color emissive;
        public string emissiveMapPath;

        public bool useAoMap;
        public string aoMapPath;

        public bool useOpacityMap;
        public float opacity;
        public string opacityMapPath;

        public Vector4 uvOffset;
        public Vector4 uvScale;

        public MaterialData() { }

        public MaterialData(MaterialInfo materialInfo)
        {
            string shaderName = materialInfo.material.shader.name;
            if (shaderName != "VRtist/ObjectOpaque" &&
                shaderName != "VRtist/ObjectTransparent" &&
                shaderName != "VRtist/ObjectOpaqueUnlit" &&
                shaderName != "VRtist/ObjectTransparentUnlit")
            {
                Debug.LogWarning($"Unsupported material {shaderName}. Expected VRtist/Object*.");
                return;
            }

            name = materialInfo.material.name;
            path = materialInfo.relativePath;

            unlit = shaderName.Contains("Unlit");

            useColorMap = materialInfo.material.GetInt("_UseColorMap") == 1f;
            baseColor = materialInfo.material.GetColor("_BaseColor");
            if (useColorMap) { colorMapPath = materialInfo.relativePath + "color.tex"; }

            useNormalMap = materialInfo.material.GetInt("_UseNormalMap") == 1f;
            if (useNormalMap) { normalMapPath = materialInfo.relativePath + "normal.tex"; }

            useMetallicMap = materialInfo.material.GetInt("_UseMetallicMap") == 1f;
            metallic = materialInfo.material.GetFloat("_Metallic");
            if (useMetallicMap) { metallicMapPath = materialInfo.relativePath + "metallic.tex"; }

            useRoughnessMap = materialInfo.material.GetInt("_UseRoughnessMap") == 1f;
            roughness = materialInfo.material.GetFloat("_Roughness");
            if (useRoughnessMap) { roughnessMapPath = materialInfo.relativePath + "roughness.tex"; }

            useEmissiveMap = materialInfo.material.GetInt("_UseEmissiveMap") == 1f;
            emissive = materialInfo.material.GetColor("_Emissive");
            if (useEmissiveMap) { metallicMapPath = materialInfo.relativePath + "emissive.tex"; }

            useAoMap = materialInfo.material.GetInt("_UseAoMap") == 1f;
            if (useAoMap) { aoMapPath = materialInfo.relativePath + "ao.tex"; }

            useOpacityMap = materialInfo.material.GetInt("_UseOpacityMap") == 1f;
            opacity = materialInfo.material.GetFloat("_Opacity");
            if (useOpacityMap) { opacityMapPath = materialInfo.relativePath + "opacity.tex"; }

            uvOffset = materialInfo.material.GetVector("_UvOffset");
            uvScale = materialInfo.material.GetVector("_UvScale");
        }

        public Material CreateMaterial(string rootPath)
        {
            Material material;
            if (!unlit)
            {
                material = new Material(
                opacity == 1f && !useOpacityMap ?
                ResourceManager.GetMaterial(MaterialID.ObjectOpaque) :
                ResourceManager.GetMaterial(MaterialID.ObjectTransparent));
            }
            else
            {
                material = new Material(
                opacity == 1f && !useOpacityMap ?
                ResourceManager.GetMaterial(MaterialID.ObjectOpaqueUnlit) :
                ResourceManager.GetMaterial(MaterialID.ObjectTransparentUnlit));
            }

            string fullPath = rootPath + path;

            const string instanceString = " (Instance)";
            while (name.EndsWith(instanceString))
                name = name.Substring(0, name.Length - instanceString.Length);
            material.name = name;
            material.SetFloat("_UseColorMap", useColorMap ? 1f : 0f);
            material.SetColor("_BaseColor", baseColor);
            if (useColorMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "color.tex", false);
                if (null != texture) { material.SetTexture("_ColorMap", texture); }
            }

            material.SetFloat("_UseNormalMap", useNormalMap ? 1f : 0f);
            if (useNormalMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "normal.tex", true);
                if (null != texture) { material.SetTexture("_NormalMap", texture); }
            }

            material.SetFloat("_UseMetallicMap", useMetallicMap ? 1f : 0f);
            material.SetFloat("_Metallic", metallic);
            if (useMetallicMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "metallic.tex", true);
                if (null != texture) { material.SetTexture("_MetallicMap", texture); }
            }

            material.SetFloat("_UseRoughnessMap", useRoughnessMap ? 1f : 0f);
            material.SetFloat("_Roughness", roughness);
            if (useRoughnessMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "roughness.tex", true);
                if (null != texture) { material.SetTexture("_RoughnessMap", texture); }
            }

            material.SetFloat("_UseEmissiveMap", useEmissiveMap ? 1f : 0f);
            material.SetColor("_Emissive", emissive);
            if (useEmissiveMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "emissive.tex", true);
                if (null != texture) { material.SetTexture("_EmissiveMap", texture); }
            }

            material.SetFloat("_UseAoMap", useAoMap ? 1f : 0f);
            if (useAoMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "ao.tex", true);
                if (null != texture) { material.SetTexture("_AoMap", texture); }
            }

            material.SetFloat("_UseOpacityMap", useOpacityMap ? 1f : 0f);
            material.SetFloat("_Opacity", opacity);
            if (useOpacityMap)
            {
                Texture2D texture = TextureUtils.LoadRawTexture(fullPath + "opacity.tex", true);
                if (null != texture) { material.SetTexture("_OpacityMap", texture); }
            }

            material.SetVector("_UvOffset", uvOffset);
            material.SetVector("_UvScale", uvScale);

            return material;
        }

        public void FromBytes(byte[] bytes, ref int index)
        {
            name = Converter.GetString(bytes, ref index);
            path = Converter.GetString(bytes, ref index);

            unlit = Converter.GetBool(bytes, ref index);

            useColorMap = Converter.GetBool(bytes, ref index);
            baseColor = Converter.GetColor(bytes, ref index);
            colorMapPath = Converter.GetString(bytes, ref index);

            useNormalMap = Converter.GetBool(bytes, ref index);
            normalMapPath = Converter.GetString(bytes, ref index);

            useMetallicMap = Converter.GetBool(bytes, ref index);
            metallic = Converter.GetFloat(bytes, ref index);
            metallicMapPath = Converter.GetString(bytes, ref index);

            useRoughnessMap = Converter.GetBool(bytes, ref index);
            roughness = Converter.GetFloat(bytes, ref index);
            roughnessMapPath = Converter.GetString(bytes, ref index);

            useEmissiveMap = Converter.GetBool(bytes, ref index);
            emissive = Converter.GetColor(bytes, ref index);
            emissiveMapPath = Converter.GetString(bytes, ref index);

            useAoMap = Converter.GetBool(bytes, ref index);
            aoMapPath = Converter.GetString(bytes, ref index);

            useOpacityMap = Converter.GetBool(bytes, ref index);
            opacity = Converter.GetFloat(bytes, ref index);
            opacityMapPath = Converter.GetString(bytes, ref index);

            uvOffset = Converter.GetVector4(bytes, ref index);
            uvScale = Converter.GetVector4(bytes, ref index);
        }

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] pathBuffer = Converter.StringToBytes(path);

            byte[] unlitBuffer = Converter.BoolToBytes(unlit);

            byte[] useColorMapBuffer = Converter.BoolToBytes(useColorMap);
            byte[] baseColorBuffer = Converter.ColorToBytes(baseColor);
            byte[] colorMapPathBuffer = Converter.StringToBytes(colorMapPath);

            byte[] useNormalMapBuffer = Converter.BoolToBytes(useNormalMap);
            byte[] normalMapPathBuffer = Converter.StringToBytes(normalMapPath);

            byte[] useMetallicMapBuffer = Converter.BoolToBytes(useMetallicMap);
            byte[] metallicBuffer = Converter.FloatToBytes(metallic);
            byte[] metallicMapPathBuffer = Converter.StringToBytes(metallicMapPath);

            byte[] useRoughnessMapBuffer = Converter.BoolToBytes(useRoughnessMap);
            byte[] roughnessBuffer = Converter.FloatToBytes(roughness);
            byte[] roughnessMapPathBuffer = Converter.StringToBytes(roughnessMapPath);

            byte[] useEmissiveMapBuffer = Converter.BoolToBytes(useEmissiveMap);
            byte[] emissiveBuffer = Converter.ColorToBytes(emissive);
            byte[] emissiveMapPathBuffer = Converter.StringToBytes(emissiveMapPath);

            byte[] useAoMapBuffer = Converter.BoolToBytes(useAoMap);
            byte[] aoMapPathBuffer = Converter.StringToBytes(aoMapPath);

            byte[] useOpacityMapBuffer = Converter.BoolToBytes(useOpacityMap);
            byte[] opacityBuffer = Converter.FloatToBytes(opacity);
            byte[] opacityMapPathBuffer = Converter.StringToBytes(opacityMapPath);

            byte[] uvOffsetBuffer = Converter.Vector4ToBytes(uvOffset);
            byte[] uvScaleBuffer = Converter.Vector4ToBytes(uvScale);

            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                nameBuffer,
                pathBuffer,

                unlitBuffer,

                useColorMapBuffer,
                baseColorBuffer,
                colorMapPathBuffer,

                useNormalMapBuffer,
                normalMapPathBuffer,

                useMetallicMapBuffer,
                metallicBuffer,
                metallicMapPathBuffer,

                useRoughnessMapBuffer,
                roughnessBuffer,
                roughnessMapPathBuffer,

                useEmissiveMapBuffer,
                emissiveBuffer,
                emissiveMapPathBuffer,

                useAoMapBuffer,
                aoMapPathBuffer,

                useOpacityMapBuffer,
                opacityBuffer,
                opacityMapPathBuffer,

                uvOffsetBuffer,
                uvScaleBuffer
            });
            return bytes;
        }
    }


    public class SubMesh : IBlob
    {
        public MeshTopology topology;
        public int[] indices;

        public SubMesh() { }

        public void FromBytes(byte[] bytes, ref int index)
        {
            topology = (MeshTopology)Converter.GetInt(bytes, ref index);
            indices = Converter.GetInts(bytes, ref index);
        }

        public byte[] ToBytes()
        {
            byte[] topologyBuffer = Converter.IntToBytes((int)topology);
            byte[] indicesBuffer = Converter.IntsToBytes(indices);

            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                topologyBuffer,
                indicesBuffer
            });
            return bytes;
        }
    }


    public class MeshData : IBlob
    {
        public MeshData() { }

        public MeshData(Serialization.MeshInfo meshInfo)
        {
            name = meshInfo.mesh.name;
            vertices = meshInfo.mesh.vertices;
            normals = meshInfo.mesh.normals;
            uvs = meshInfo.mesh.uv;
            subMeshes = new SubMesh[meshInfo.mesh.subMeshCount];
            for (int i = 0; i < meshInfo.mesh.subMeshCount; ++i)
            {
                subMeshes[i] = new SubMesh
                {
                    topology = meshInfo.mesh.GetSubMesh(i).topology,
                    indices = meshInfo.mesh.GetIndices(i)
                };
            }
        }

        private string name;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector2[] uvs;
        private SubMesh[] subMeshes;

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                subMeshCount = subMeshes.Length
            };
            for (int i = 0; i < subMeshes.Length; ++i)
            {
                mesh.SetIndices(subMeshes[i].indices, subMeshes[i].topology, i);
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        public void FromBytes(byte[] bytes, ref int index)
        {
            name = Converter.GetString(bytes, ref index);
            vertices = Converter.GetVectors3(bytes, ref index);
            normals = Converter.GetVectors3(bytes, ref index);
            uvs = Converter.GetVectors2(bytes, ref index);

            int subMeshesCount = Converter.GetInt(bytes, ref index);
            subMeshes = new SubMesh[subMeshesCount];
            for (int i = 0; i < subMeshesCount; i++)
            {
                subMeshes[i] = new SubMesh();
                subMeshes[i].FromBytes(bytes, ref index);
            }
        }

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] verticesBuffer = Converter.Vectors3ToBytes(vertices);
            byte[] normalsBuffer = Converter.Vectors3ToBytes(normals);
            byte[] uvsBuffer = Converter.Vectors2ToBytes(uvs);

            int subMeshesCount = subMeshes.Length;
            byte[] subMeshesCountBuffer = Converter.IntToBytes(subMeshesCount);
            List<byte[]> subMeshesBufferList = new List<byte[]>();
            for (int i = 0; i < subMeshesCount; i++)
            {
                subMeshesBufferList.Add(subMeshes[i].ToBytes());
            }
            byte[] subMeshesBuffer = Converter.ConcatenateBuffers(subMeshesBufferList);

            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]> {
                nameBuffer,
                verticesBuffer,
                normalsBuffer,
                uvsBuffer,
                subMeshesCountBuffer,
                subMeshesBuffer
            });
            return bytes;
        }
    }

    public class SkinMeshData : IBlob
    {
        private string name;
        private string[] bones;
        private MeshData mesh;
        private int poseCount;
        private Matrix4x4[] bindposes;
        private string rootBone;
        private int vertexCount;
        private byte[] bonesPerVert;
        private int[] boneIndexes;
        private float[] boneWeigts;
        private Vector3 boundsCenter;
        private Vector3 boundsExtend;

        public SkinMeshData() { }

        public SkinMeshData(SkinMeshInfo skinMesh)
        {
            name = skinMesh.skinMesh.name;
            mesh = new MeshData(skinMesh.mesh);
            bones = new string[skinMesh.skinMesh.bones.Length];
            for (int i = 0; i < skinMesh.skinMesh.bones.Length; i++)
            {
                bones[i] = GetPathToRoot(skinMesh.skinMesh.transform.parent, skinMesh.skinMesh.bones[i]);
            }
            poseCount = skinMesh.skinMesh.sharedMesh.bindposes.Length;
            bindposes = new Matrix4x4[poseCount];
            for (int j = 0; j < poseCount; j++)
            {
                bindposes[j] = skinMesh.skinMesh.sharedMesh.bindposes[j];
            }
            rootBone = skinMesh.skinMesh.rootBone.name;
            bonesPerVert = skinMesh.mesh.mesh.GetBonesPerVertex().ToArray();
            vertexCount = bonesPerVert.Length;
            BoneWeight1[] weigts = skinMesh.mesh.mesh.GetAllBoneWeights().ToArray();
            boneIndexes = new int[weigts.Length];
            boneWeigts = new float[weigts.Length];
            for (int k = 0; k < weigts.Length; k++)
            {
                boneIndexes[k] = weigts[k].boneIndex;
                boneWeigts[k] = weigts[k].weight;
            }
            boundsCenter = skinMesh.skinMesh.localBounds.center;
            boundsExtend = skinMesh.skinMesh.localBounds.extents;
        }

        public void SetSkinnedMeshRenderer(SkinnedMeshRenderer renderer, Transform parent)
        {
            Transform[] bonesTransform = new Transform[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bonesTransform[i] = parent.Find(bones[i]);
            }
            renderer.bones = bonesTransform;
            renderer.sharedMesh = mesh.CreateMesh();
            renderer.sharedMesh.bindposes = bindposes;
            Unity.Collections.NativeArray<BoneWeight1> bw1 = new Unity.Collections.NativeArray<BoneWeight1>(boneIndexes.Length, Unity.Collections.Allocator.Temp);
            for (int j = 0; j < boneIndexes.Length; j++)
            {
                bw1[j] = new BoneWeight1 { boneIndex = boneIndexes[j], weight = boneWeigts[j] };
            }
            renderer.sharedMesh.SetBoneWeights(new Unity.Collections.NativeArray<byte>(bonesPerVert, Unity.Collections.Allocator.Temp), bw1);
            renderer.rootBone = parent.Find(rootBone);
            renderer.localBounds = new Bounds(boundsCenter, boundsExtend);
            renderer.sharedMesh.RecalculateBounds();
        }

        public void FromBytes(byte[] bytes, ref int index)
        {
            name = Converter.GetString(bytes, ref index);
            bones = Converter.GetStrings(bytes, ref index).ToArray();
            mesh = new MeshData();
            mesh.FromBytes(bytes, ref index);
            poseCount = Converter.GetInt(bytes, ref index);
            bindposes = new Matrix4x4[poseCount];
            for (int i = 0; i < poseCount; i++)
            {
                bindposes[i] = Converter.GetMatrix(bytes, ref index);
            }
            rootBone = Converter.GetString(bytes, ref index);
            vertexCount = Converter.GetInt(bytes, ref index);
            bonesPerVert = new byte[vertexCount];
            Buffer.BlockCopy(bytes, index, bonesPerVert, 0, vertexCount);
            index += vertexCount;
            boneIndexes = Converter.GetInts(bytes, ref index);
            boneWeigts = Converter.GetFloats(bytes, ref index);
            boundsCenter = Converter.GetVector3(bytes, ref index);
            boundsExtend = Converter.GetVector3(bytes, ref index);

        }

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] bonesBuffer = Converter.StringsToBytes(bones);
            byte[] meshBuffer = mesh.ToBytes();
            byte[] countBuffer = Converter.IntToBytes(poseCount);
            List<Byte[]> subBindBufferList = new List<byte[]>();
            for (int i = 0; i < poseCount; i++)
            {
                subBindBufferList.Add(Converter.MatrixToBytes(bindposes[i]));
            }
            byte[] bindBuffer = Converter.ConcatenateBuffers(subBindBufferList);
            byte[] rootBoneBuffer = Converter.StringToBytes(rootBone);
            byte[] vertexCountBuffer = Converter.IntToBytes(vertexCount);
            byte[] boneIndexesBuffer = Converter.IntsToBytes(boneIndexes);
            byte[] boneWeigtsBuffer = Converter.FloatsToBytes(boneWeigts);
            byte[] centerBuffer = Converter.Vector3ToBytes(boundsCenter);
            byte[] extendBuffer = Converter.Vector3ToBytes(boundsExtend);
            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                nameBuffer, bonesBuffer, meshBuffer, countBuffer, bindBuffer, rootBoneBuffer, vertexCountBuffer, bonesPerVert, boneIndexesBuffer, boneWeigtsBuffer, centerBuffer, extendBuffer
            });
            return bytes;
        }

        private string GetPathToRoot(Transform root, Transform target)
        {
            if (null != root && null != target && root != target.parent)
            {
                return GetPathToRoot(root, target.parent) + "/" + target.name;
            }
            else
            {
                return target.name;
            }
        }
    }

    public class ObjectData : IBlob
    {
        public string name;
        public string parentPath;
        public string path;  // relative path
        public string tag;

        public bool visible;

        // Transform
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        // Mesh
        public bool isSkinMesh;
        public string meshPath;
        public bool isImported;

        // Materials
        public List<MaterialData> materialsData = new List<MaterialData>();

        // Parameters
        public bool lockPosition;
        public bool lockRotation;
        public bool lockScale;

        public virtual void FromBytes(byte[] bytes, ref int index)
        {
            name = Converter.GetString(bytes, ref index);
            parentPath = Converter.GetString(bytes, ref index);
            path = Converter.GetString(bytes, ref index);
            tag = Converter.GetString(bytes, ref index);

            visible = Converter.GetBool(bytes, ref index);

            position = Converter.GetVector3(bytes, ref index);
            rotation = Converter.GetQuaternion(bytes, ref index);
            scale = Converter.GetVector3(bytes, ref index);

            isSkinMesh = Converter.GetBool(bytes, ref index);
            meshPath = Converter.GetString(bytes, ref index);
            isImported = Converter.GetBool(bytes, ref index);

            int materialCount = Converter.GetInt(bytes, ref index);
            for (int i = 0; i < materialCount; i++)
            {
                MaterialData matData = new MaterialData();
                matData.FromBytes(bytes, ref index);
                materialsData.Add(matData);
            }

            lockPosition = Converter.GetBool(bytes, ref index);
            lockRotation = Converter.GetBool(bytes, ref index);
            lockScale = Converter.GetBool(bytes, ref index);
        }

        public virtual byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] parentBuffer = Converter.StringToBytes(parentPath);
            byte[] pathBuffer = Converter.StringToBytes(path);
            byte[] tagBuffer = Converter.StringToBytes(tag);

            byte[] visibleBuffer = Converter.BoolToBytes(visible);

            byte[] positionBuffer = Converter.Vector3ToBytes(position);
            byte[] rotationBuffer = Converter.QuaternionToBytes(rotation);
            byte[] scaleBuffer = Converter.Vector3ToBytes(scale);

            byte[] skinMeshBuffer = Converter.BoolToBytes(isSkinMesh);
            byte[] meshPathBuffer = Converter.StringToBytes(meshPath);
            byte[] isImportedBuffer = Converter.BoolToBytes(isImported);

            byte[] materialCountBuffer = Converter.IntToBytes(materialsData.Count);
            List<byte[]> matBuffers = new List<byte[]>();
            foreach (MaterialData matData in materialsData)
            {
                matBuffers.Add(matData.ToBytes());
            }
            byte[] materialsBuffer = Converter.ConcatenateBuffers(matBuffers);

            byte[] lockPositionBuffer = Converter.BoolToBytes(lockPosition);
            byte[] lockRotationBuffer = Converter.BoolToBytes(lockRotation);
            byte[] lockScaleBuffer = Converter.BoolToBytes(lockScale);

            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]> {
                nameBuffer,
                parentBuffer,
                pathBuffer,
                tagBuffer,

                visibleBuffer,

                positionBuffer,
                rotationBuffer,
                scaleBuffer,

                skinMeshBuffer,
                meshPathBuffer,
                isImportedBuffer,

                materialCountBuffer,
                materialsBuffer,

                lockPositionBuffer,
                lockRotationBuffer,
                lockScaleBuffer
            });
            return bytes;
        }
    }

    public class LightData : ObjectData
    {
        public LightType lightType;
        public float intensity;
        public float minIntensity;
        public float maxIntensity;
        public Color color;
        public bool castShadows;
        public float near;
        public float range;
        public float minRange;
        public float maxRange;
        public float outerAngle;
        public float sharpness;

        public override void FromBytes(byte[] buffer, ref int index)
        {
            base.FromBytes(buffer, ref index);
            lightType = (LightType)Converter.GetInt(buffer, ref index);
            intensity = Converter.GetFloat(buffer, ref index);
            minIntensity = Converter.GetFloat(buffer, ref index);
            maxIntensity = Converter.GetFloat(buffer, ref index);
            color = Converter.GetColor(buffer, ref index);
            castShadows = Converter.GetBool(buffer, ref index);
            near = Converter.GetFloat(buffer, ref index);
            range = Converter.GetFloat(buffer, ref index);
            minRange = Converter.GetFloat(buffer, ref index);
            maxRange = Converter.GetFloat(buffer, ref index);
            outerAngle = Converter.GetFloat(buffer, ref index);
            sharpness = Converter.GetFloat(buffer, ref index);
        }

        public override byte[] ToBytes()
        {
            byte[] baseBuffer = base.ToBytes();
            byte[] lightTypeBuffer = Converter.IntToBytes((int)lightType);
            byte[] intensityBuffer = Converter.FloatToBytes(intensity);
            byte[] minIntensityBuffer = Converter.FloatToBytes(minIntensity);
            byte[] maxIntensityBuffer = Converter.FloatToBytes(maxIntensity);
            byte[] colorBuffer = Converter.ColorToBytes(color);
            byte[] castShadowsBuffer = Converter.BoolToBytes(castShadows);
            byte[] nearBuffer = Converter.FloatToBytes(near);
            byte[] rangeBuffer = Converter.FloatToBytes(range);
            byte[] minRangeBuffer = Converter.FloatToBytes(minRange);
            byte[] maxRangeBuffer = Converter.FloatToBytes(maxRange);
            byte[] outerAngleBuffer = Converter.FloatToBytes(outerAngle);
            byte[] sharpnessBuffer = Converter.FloatToBytes(sharpness);

            return Converter.ConcatenateBuffers(new List<byte[]>()
            {
                baseBuffer,
                lightTypeBuffer,
                intensityBuffer,
                minIntensityBuffer,
                maxIntensityBuffer,
                colorBuffer,
                castShadowsBuffer,
                nearBuffer,
                rangeBuffer,
                minRangeBuffer,
                maxRangeBuffer,
                outerAngleBuffer,
                sharpnessBuffer}
            );
        }

    }
    public class CameraData : ObjectData
    {
        public float focal;
        public float focus;
        public float aperture;
        public bool enableDOF;
        public float near;
        public float far;
        public float filmHeight;
        public float filmWidth;
        public int gateFit;

        public override void FromBytes(byte[] buffer, ref int index)
        {
            base.FromBytes(buffer, ref index);
            focal = Converter.GetFloat(buffer, ref index);
            focus = Converter.GetFloat(buffer, ref index);
            aperture = Converter.GetFloat(buffer, ref index);
            enableDOF = Converter.GetBool(buffer, ref index);
            near = Converter.GetFloat(buffer, ref index);
            far = Converter.GetFloat(buffer, ref index);
            filmHeight = Converter.GetFloat(buffer, ref index);

            if (SceneData.fileVersion >= 1)
            {
                filmWidth = Converter.GetFloat(buffer, ref index);
                gateFit = Converter.GetInt(buffer, ref index);
            }
            else
            {
                filmWidth = 36f;
                gateFit = 3;  // Fill
            }
        }

        public override byte[] ToBytes()
        {
            byte[] baseBuffer = base.ToBytes();
            byte[] focalBuffer = Converter.FloatToBytes(focal);
            byte[] focusBuffer = Converter.FloatToBytes(focus);
            byte[] apertureBuffer = Converter.FloatToBytes(aperture);
            byte[] enableDOFBuffer = Converter.BoolToBytes(enableDOF);
            byte[] nearBuffer = Converter.FloatToBytes(near);
            byte[] farBuffer = Converter.FloatToBytes(far);
            byte[] filmHeightBuffer = Converter.FloatToBytes(filmHeight);
            byte[] filmWidthBuffer = Converter.FloatToBytes(filmWidth);
            byte[] gateFitBuffer = Converter.IntToBytes(gateFit);

            return Converter.ConcatenateBuffers(new List<byte[]>()
            {
                baseBuffer,
                focalBuffer,
                focusBuffer,
                apertureBuffer,
                enableDOFBuffer,
                nearBuffer,
                farBuffer,
                filmHeightBuffer,
                filmWidthBuffer,
                gateFitBuffer
            });
        }
    }


    public class ShotData : IBlob
    {
        public string name;
        public int start;
        public int end;
        public string cameraName;
        public bool enabled;

        public void FromBytes(byte[] buffer, ref int index)
        {
            name = Converter.GetString(buffer, ref index);
            start = Converter.GetInt(buffer, ref index);
            end = Converter.GetInt(buffer, ref index);
            cameraName = Converter.GetString(buffer, ref index);
            enabled = Converter.GetBool(buffer, ref index);
        }

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] startBuffer = Converter.IntToBytes(start);
            byte[] endBuffer = Converter.IntToBytes(end);
            byte[] cameraNameBuffer = Converter.StringToBytes(cameraName);
            byte[] enabledBuffer = Converter.BoolToBytes(enabled);

            return Converter.ConcatenateBuffers(new List<byte[]>()
            {
                nameBuffer,
                startBuffer,
                endBuffer,
                cameraNameBuffer,
                enabledBuffer
            });
        }
    }


    public class KeyframeData : IBlob
    {
        public int frame;
        public float value;
        public Interpolation interpolation;
        public Vector2 inTangent;
        public Vector2 outTangent;

        public byte[] ToBytes()
        {
            byte[] frameBuffer = Converter.IntToBytes(frame);
            byte[] valueBuffer = Converter.FloatToBytes(value);
            byte[] interpolationBuffer = Converter.IntToBytes((int)interpolation);
            byte[] inTanBuffer = Converter.Vector2ToBytes(inTangent);
            byte[] outTanBuffer = Converter.Vector2ToBytes(outTangent);
            return Converter.ConcatenateBuffers(new List<byte[]> {
                frameBuffer,
                valueBuffer,
                interpolationBuffer,
                inTanBuffer,
                outTanBuffer
            });
        }

        public void FromBytes(byte[] buffer, ref int index)
        {
            frame = Converter.GetInt(buffer, ref index);
            value = Converter.GetFloat(buffer, ref index);
            interpolation = (Interpolation)Converter.GetInt(buffer, ref index);
            inTangent = Converter.GetVector2(buffer, ref index);
            outTangent = Converter.GetVector2(buffer, ref index);
        }
    }


    public class CurveData : IBlob
    {
        public AnimatableProperty property;
        public List<KeyframeData> keyframes = new List<KeyframeData>();

        public byte[] ToBytes()
        {
            byte[] propertyBuffer = Converter.IntToBytes((int)property);
            byte[] keyCountBuffer = Converter.IntToBytes(keyframes.Count);
            List<byte[]> keysBufferList = new List<byte[]>();
            foreach (KeyframeData keyframe in keyframes)
            {
                keysBufferList.Add(keyframe.ToBytes());
            }
            byte[] keyframesBuffer = Converter.ConcatenateBuffers(keysBufferList);
            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                propertyBuffer,
                keyCountBuffer,
                keyframesBuffer
            });
            return bytes;
        }

        public void FromBytes(byte[] buffer, ref int index)
        {
            property = (AnimatableProperty)Converter.GetInt(buffer, ref index);
            int count = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < count; ++i)
            {
                KeyframeData keyframe = new KeyframeData();
                keyframe.FromBytes(buffer, ref index);
                keyframes.Add(keyframe);
            }
        }
    }


    public class AnimationData : IBlob
    {
        public string objectPath;
        public List<CurveData> curves = new List<CurveData>();

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(objectPath);
            byte[] curveCountBuffer = Converter.IntToBytes(curves.Count);
            List<byte[]> curvesBufferList = new List<byte[]>();
            foreach (CurveData curve in curves)
            {
                curvesBufferList.Add(curve.ToBytes());
            }
            byte[] curvesBuffer = Converter.ConcatenateBuffers(curvesBufferList);
            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                nameBuffer,
                curveCountBuffer,
                curvesBuffer
            });
            return bytes;
        }

        public void FromBytes(byte[] buffer, ref int index)
        {
            objectPath = Converter.GetString(buffer, ref index);
            int curvesCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < curvesCount; ++i)
            {
                CurveData curveData = new CurveData();
                curveData.FromBytes(buffer, ref index);
                curves.Add(curveData);
            }
        }
    }


    public class ConstraintData : IBlob
    {
        public string source;
        public string target;
        public ConstraintType type;

        public byte[] ToBytes()
        {
            byte[] sourceBuffer = Converter.StringToBytes(source);
            byte[] targetBuffer = Converter.StringToBytes(target);
            byte[] typeBuffer = Converter.IntToBytes((int)type);
            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                sourceBuffer,
                targetBuffer,
                typeBuffer
            });
            return bytes;
        }

        public void FromBytes(byte[] buffer, ref int index)
        {
            source = Converter.GetString(buffer, ref index);
            target = Converter.GetString(buffer, ref index);
            type = (ConstraintType)Converter.GetInt(buffer, ref index);
        }
    }

    public class RigData : IBlob
    {
        public string ObjectName;
        public string meshPath;

        public RigData() { }

        public RigData(RigController controller)
        {
            ObjectName = controller.name;
            meshPath = GetPathToRoot(controller.transform, controller.SkinMesh.transform);
        }

        public void FromBytes(byte[] bytes, ref int index)
        {
            ObjectName = Converter.GetString(bytes, ref index);
            meshPath = Converter.GetString(bytes, ref index);
        }

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(ObjectName);
            byte[] pathBuffer = Converter.StringToBytes(meshPath);
            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>() { nameBuffer, pathBuffer });
            return bytes;
        }

        private string GetPathToRoot(Transform root, Transform target)
        {
            if (root != target.parent && null != root && null != target)
            {
                return GetPathToRoot(root, target.parent) + "/" + target.name;
            }
            else
            {
                return target.name;
            }
        }
    }

    public class PlayerData : IBlob
    {
        public Vector3 position;
        public Quaternion rotation;
        public float scale;

        public byte[] ToBytes()
        {
            byte[] positionBuffer = Converter.Vector3ToBytes(position);
            byte[] rotationBuffer = Converter.QuaternionToBytes(rotation);
            byte[] scaleBuffer = Converter.FloatToBytes(scale);
            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]>
            {
                positionBuffer,
                rotationBuffer,
                scaleBuffer
            });
            return bytes;
        }

        public void FromBytes(byte[] buffer, ref int index)
        {
            position = Converter.GetVector3(buffer, ref index);
            rotation = Converter.GetQuaternion(buffer, ref index);
            scale = Converter.GetFloat(buffer, ref index);
        }
    }

    // Version 1:
    //  - add: CameraData.filmWidth
    //  - add: CameraData.gateFit
    public class SceneData : IBlob
    {
        private static SceneData current;
        public static SceneData Current
        {
            get
            {
                if (null == current) { current = new SceneData(); }
                return current;
            }
        }

        public List<ObjectData> objects = new List<ObjectData>();
        public List<LightData> lights = new List<LightData>();
        public List<CameraData> cameras = new List<CameraData>();

        public List<ShotData> shots = new List<ShotData>();
        public List<AnimationData> animations = new List<AnimationData>();

        public List<RigData> rigs = new List<RigData>();

        private readonly byte[] headerBuffer = new byte[6] { (byte)'V', (byte)'R', (byte)'t', (byte)'i', (byte)'s', (byte)'t' };
        public static int version = 1;
        public static int fileVersion;

        public float fps;
        public int startFrame;
        public int endFrame;
        public int currentFrame;

        public List<ConstraintData> constraints = new List<ConstraintData>();

        public SkySettings skyData;

        public PlayerData playerData;

        public void Clear()
        {
            objects.Clear();
            lights.Clear();
            cameras.Clear();
            shots.Clear();
            rigs.Clear();
            animations.Clear();
        }

        public void FromBytes(byte[] buffer, ref int index)
        {
            for (int i = 0; i < 6; i++)
            {
                if (buffer[i] != headerBuffer[i])
                {
                    throw new Exception("Invalid VRtist file Header");
                }
            }
            index += 6;

            fileVersion = Converter.GetInt(buffer, ref index);
            if (fileVersion > version)
            {
                throw new Exception("File version mismatch, please update VRtist");
            }


            int objectsCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < objectsCount; i++)
            {
                ObjectData data = new ObjectData();
                data.FromBytes(buffer, ref index);
                objects.Add(data);
            }

            int lightsCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < lightsCount; i++)
            {
                LightData data = new LightData();
                data.FromBytes(buffer, ref index);
                lights.Add(data);
            }

            int camerasCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < camerasCount; i++)
            {
                CameraData data = new CameraData();
                data.FromBytes(buffer, ref index);
                cameras.Add(data);
            }

            skyData = new SkySettings
            {
                topColor = Converter.GetColor(buffer, ref index),
                middleColor = Converter.GetColor(buffer, ref index),
                bottomColor = Converter.GetColor(buffer, ref index)
            };

            int animationsCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < animationsCount; i++)
            {
                AnimationData data = new AnimationData();
                data.FromBytes(buffer, ref index);
                animations.Add(data);
            }

            int rigCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < rigCount; i++)
            {
                RigData data = new RigData();
                data.FromBytes(buffer, ref index);
                rigs.Add(data);
            }

            fps = Converter.GetFloat(buffer, ref index);
            startFrame = Converter.GetInt(buffer, ref index);
            endFrame = Converter.GetInt(buffer, ref index);
            currentFrame = Converter.GetInt(buffer, ref index);

            int constraintsCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < constraintsCount; i++)
            {
                ConstraintData data = new ConstraintData();
                data.FromBytes(buffer, ref index);
                constraints.Add(data);
            }

            int shotsCount = Converter.GetInt(buffer, ref index);
            for (int i = 0; i < shotsCount; i++)
            {
                ShotData data = new ShotData();
                data.FromBytes(buffer, ref index);
                shots.Add(data);
            }

            playerData = new PlayerData();
            playerData.FromBytes(buffer, ref index);
        }

        public byte[] ToBytes()
        {
            byte[] versionBuffer = Converter.IntToBytes(version);

            byte[] objectsCountBuffer = Converter.IntToBytes(objects.Count);
            List<byte[]> objectsBufferList = new List<byte[]>();
            foreach (ObjectData data in objects)
            {
                objectsBufferList.Add(data.ToBytes());
            }
            byte[] objectsBuffer = Converter.ConcatenateBuffers(objectsBufferList);

            byte[] lightsCountBuffer = Converter.IntToBytes(lights.Count);
            List<byte[]> lightsBufferList = new List<byte[]>();
            foreach (LightData data in lights)
            {
                lightsBufferList.Add(data.ToBytes());
            }
            byte[] lightsBuffer = Converter.ConcatenateBuffers(lightsBufferList);

            byte[] camerasCountBuffer = Converter.IntToBytes(cameras.Count);
            List<byte[]> camerasBufferList = new List<byte[]>();
            foreach (CameraData data in cameras)
            {
                camerasBufferList.Add(data.ToBytes());
            }
            byte[] camerasBuffer = Converter.ConcatenateBuffers(camerasBufferList);

            byte[] skyTopColorBuffer = Converter.ColorToBytes(skyData.topColor);
            byte[] skyMiddleColorBuffer = Converter.ColorToBytes(skyData.middleColor);
            byte[] skyBottomColorBuffer = Converter.ColorToBytes(skyData.bottomColor);

            byte[] animationsCountBuffer = Converter.IntToBytes(animations.Count);
            List<byte[]> animationsBufferList = new List<byte[]>();
            foreach (AnimationData data in animations)
            {
                animationsBufferList.Add(data.ToBytes());
            }
            byte[] animationsBuffer = Converter.ConcatenateBuffers(animationsBufferList);

            byte[] rigCountBuffer = Converter.IntToBytes(rigs.Count);
            List<byte[]> rigBufferList = new List<byte[]>();
            foreach (RigData data in rigs)
            {
                rigBufferList.Add(data.ToBytes());
            }
            byte[] rigsBuffer = Converter.ConcatenateBuffers(rigBufferList);

            byte[] fpsBuffer = Converter.FloatToBytes(fps);
            byte[] startFrameBuffer = Converter.IntToBytes(startFrame);
            byte[] endFrameBuffer = Converter.IntToBytes(endFrame);
            byte[] currentFrameBuffer = Converter.IntToBytes(currentFrame);

            byte[] constraintsCountBuffer = Converter.IntToBytes(constraints.Count);
            List<byte[]> constraintsBufferList = new List<byte[]>();
            foreach (ConstraintData data in constraints)
            {
                constraintsBufferList.Add(data.ToBytes());
            }
            byte[] constraintsBuffer = Converter.ConcatenateBuffers(constraintsBufferList);

            byte[] shotsCountBuffer = Converter.IntToBytes(shots.Count);
            List<byte[]> shotsBufferList = new List<byte[]>();
            foreach (ShotData data in shots)
            {
                shotsBufferList.Add(data.ToBytes());
            }
            byte[] shotsBuffer = Converter.ConcatenateBuffers(shotsBufferList);

            byte[] playerBuffer = playerData.ToBytes();

            byte[] bytes = Converter.ConcatenateBuffers(new List<byte[]> {
                headerBuffer,
                versionBuffer,

                objectsCountBuffer,
                objectsBuffer,

                lightsCountBuffer,
                lightsBuffer,

                camerasCountBuffer,
                camerasBuffer,

                skyTopColorBuffer,
                skyMiddleColorBuffer,
                skyBottomColorBuffer,

                animationsCountBuffer,
                animationsBuffer,

                rigCountBuffer,
                rigsBuffer,

                fpsBuffer,
                startFrameBuffer,
                endFrameBuffer,
                currentFrameBuffer,

                constraintsCountBuffer,
                constraintsBuffer,

                shotsCountBuffer,
                shotsBuffer,

                playerBuffer
            });
            return bytes;
        }
    }
}

