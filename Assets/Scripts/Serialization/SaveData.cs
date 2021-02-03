using System.Collections.Generic;

using UnityEngine;

namespace VRtist.Serialization
{
    public class MaterialData
    {
        readonly string path;  // relative path

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

        public MaterialData(MaterialInfo materialInfo)
        {
            string shaderName = materialInfo.material.shader.name;
            if (shaderName != "VRtist/ObjectOpaque" && shaderName != "VRtist/ObjectTransparent")
            {
                Debug.LogWarning($"Unsupported material {shaderName}. Expected VRtist/ObjectOpaque or VRtist/ObjectTransparent.");
                return;
            }

            path = materialInfo.relativePath;

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
            Material material = new Material(
                opacity == 1f ?
                ResourceManager.GetMaterial(MaterialID.ObjectOpaque) :
                ResourceManager.GetMaterial(MaterialID.ObjectTransparent)
            );

            string fullPath = rootPath + path;

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

        public MaterialData(byte[] bytes, ref int offset)
        {
            path = Converter.GetString(bytes, ref offset);

            useColorMap = Converter.GetBool(bytes, ref offset);
            baseColor = Converter.GetColor(bytes, ref offset);
            colorMapPath = Converter.GetString(bytes, ref offset);

            useNormalMap = Converter.GetBool(bytes, ref offset);
            normalMapPath = Converter.GetString(bytes, ref offset);

            useMetallicMap = Converter.GetBool(bytes, ref offset);
            metallic = Converter.GetFloat(bytes, ref offset);
            metallicMapPath = Converter.GetString(bytes, ref offset);

            useRoughnessMap = Converter.GetBool(bytes, ref offset);
            roughness = Converter.GetFloat(bytes, ref offset);
            roughnessMapPath = Converter.GetString(bytes, ref offset);

            useEmissiveMap = Converter.GetBool(bytes, ref offset);
            emissive = Converter.GetColor(bytes, ref offset);
            emissiveMapPath = Converter.GetString(bytes, ref offset);

            useAoMap = Converter.GetBool(bytes, ref offset);
            aoMapPath = Converter.GetString(bytes, ref offset);

            useOpacityMap = Converter.GetBool(bytes, ref offset);
            opacity = Converter.GetFloat(bytes, ref offset);
            opacityMapPath = Converter.GetString(bytes, ref offset);

            uvOffset = Converter.GetVector4(bytes, ref offset);
            uvScale = Converter.GetVector4(bytes, ref offset);
        }

        public byte[] ToBytes()
        {
            byte[] pathBuffer = Converter.StringToBytes(path);

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
                pathBuffer,

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


    public class SubMesh
    {
        public MeshTopology topology;
        public int[] indices;

        public SubMesh(byte[] bytes, ref int offset)
        {
            topology = (MeshTopology)Converter.GetInt(bytes, ref offset);
            indices = Converter.GetInts(bytes, ref offset);
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


    [System.Serializable]
    public class MeshData
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
    }


    public class ObjectData
    {
        public string name;
        public string path;  // relative path
        public string tag;

        // Transform
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        // Mesh
        public string meshPath;
        public bool isImported;

        // Materials
        public List<MaterialData> materialsData = new List<MaterialData>();

        // Parameters
        public bool lockPosition;
        public bool lockRotation;
        public bool lockScale;

        // Constraints

        protected int readIndex = 0;
        public ObjectData(byte[] bytes)
        {
            name = Converter.GetString(bytes, ref readIndex);
            path = Converter.GetString(bytes, ref readIndex);
            tag = Converter.GetString(bytes, ref readIndex);

            position = Converter.GetVector3(bytes, ref readIndex);
            rotation = Converter.GetQuaternion(bytes, ref readIndex);
            scale = Converter.GetVector3(bytes, ref readIndex);

            meshPath = Converter.GetString(bytes, ref readIndex);
            isImported = Converter.GetBool(bytes, ref readIndex);

            int materialCount = Converter.GetInt(bytes, ref readIndex);
            for (int i = 0; i < materialCount; i++)
            {
                materialsData.Add(new MaterialData(bytes, ref readIndex));
            }

            lockPosition = Converter.GetBool(bytes, ref readIndex);
            lockRotation = Converter.GetBool(bytes, ref readIndex);
            lockScale = Converter.GetBool(bytes, ref readIndex);
        }

        public virtual byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] pathBuffer = Converter.StringToBytes(path);
            byte[] tagBuffer = Converter.StringToBytes(tag);

            byte[] positionBuffer = Converter.Vector3ToBytes(position);
            byte[] rotationBuffer = Converter.QuaternionToBytes(rotation);
            byte[] scaleBuffer = Converter.Vector3ToBytes(scale);

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
                pathBuffer,
                tagBuffer,
                positionBuffer,
                rotationBuffer,
                scaleBuffer,
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
        public float innerAngle;

        LightData(byte[] buffer, ref int index) : base(buffer)
        {
            lightType = (LightType)Converter.GetInt(buffer, ref readIndex);
            intensity = Converter.GetFloat(buffer, ref readIndex);
            minIntensity = Converter.GetFloat(buffer, ref readIndex);
            maxIntensity = Converter.GetFloat(buffer, ref readIndex);
            color = Converter.GetColor(buffer, ref readIndex);
            castShadows = Converter.GetBool(buffer, ref readIndex);
            near = Converter.GetFloat(buffer, ref readIndex);
            range = Converter.GetFloat(buffer, ref readIndex);
            minRange = Converter.GetFloat(buffer, ref readIndex);
            maxRange = Converter.GetFloat(buffer, ref readIndex);
            outerAngle = Converter.GetFloat(buffer, ref readIndex);
            innerAngle = Converter.GetFloat(buffer, ref readIndex);
        }

        public override byte[] ToBytes()
        {
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
            byte[] innerAngleBuffer = Converter.FloatToBytes(innerAngle);

            return Converter.ConcatenateBuffers(new List<byte[]>()
            {
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
                innerAngleBuffer}
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

        CameraData(byte[] buffer, ref int index) : base(buffer)
        {
            focal = Converter.GetFloat(buffer, ref readIndex);
            focus = Converter.GetFloat(buffer, ref readIndex);
            aperture = Converter.GetFloat(buffer, ref readIndex);
            enableDOF = Converter.GetBool(buffer, ref readIndex);
            near = Converter.GetFloat(buffer, ref readIndex);
            far = Converter.GetFloat(buffer, ref readIndex);
            filmHeight = Converter.GetFloat(buffer, ref readIndex);
        }

        public override byte[] ToBytes()
        {
            byte[] focalBuffer = Converter.FloatToBytes(focal);
            byte[] focusBuffer = Converter.FloatToBytes(focus);
            byte[] apertureBuffer = Converter.FloatToBytes(aperture);
            byte[] enableDOFBuffer = Converter.BoolToBytes(enableDOF);
            byte[] nearBuffer = Converter.FloatToBytes(near);
            byte[] farBuffer = Converter.FloatToBytes(far);
            byte[] filmHeightBuffer = Converter.FloatToBytes(filmHeight);

            return Converter.ConcatenateBuffers(new List<byte[]>()
            {
                focalBuffer,
                focusBuffer,
                apertureBuffer,
                enableDOFBuffer,
                nearBuffer,
                farBuffer,
                filmHeightBuffer
            });
        }
    }

    public class ShotData
    {
        public string name;
        public int index;
        public int start;
        public int end;
        public string cameraName;

        ShotData(byte[] buffer, ref int index)
        {
            name = Converter.GetString(buffer, ref index);
            index = Converter.GetInt(buffer, ref index);
            start = Converter.GetInt(buffer, ref index);
            end = Converter.GetInt(buffer, ref index);
            cameraName = Converter.GetString(buffer, ref index);
        }

        public byte[] ToBytes()
        {
            byte[] nameBuffer = Converter.StringToBytes(name);
            byte[] indexBuffer = Converter.IntToBytes(index);
            byte[] startBuffer = Converter.IntToBytes(start);
            byte[] endBuffer = Converter.IntToBytes(end);
            byte[] cameraNameBuffer = Converter.StringToBytes(cameraName);

            return Converter.ConcatenateBuffers(new List<byte[]>()
            {
                nameBuffer,
                indexBuffer,
                startBuffer,
                endBuffer,
                cameraNameBuffer
            });
        }

        public class SceneData
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

            public SkySettings skyData;

            public void Clear()
            {
                objects.Clear();
                lights.Clear();
                cameras.Clear();
                shots.Clear();
            }

            public byte[] ToBytes()
            {


                byte[] bytes = new byte[456];
                return bytes;
            }

            public void Load()
            {

            }
        }
    }
}

