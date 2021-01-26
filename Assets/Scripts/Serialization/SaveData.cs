using System.Collections.Generic;

using UnityEngine;

namespace VRtist.Serialization
{
    [System.Serializable]
    public enum ObjectType
    {
        Object,
        Empty,
        Light,
        Camera
    }


    [System.Serializable]
    public class MaterialData
    {
        string path;

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

            path = materialInfo.materialPath;

            useColorMap = materialInfo.material.GetInt("_UseColorMap") == 1f;
            baseColor = materialInfo.material.GetColor("_BaseColor");
            if (useColorMap) { colorMapPath = materialInfo.materialPath + "color.tex"; }

            useNormalMap = materialInfo.material.GetInt("_UseNormalMap") == 1f;
            if (useNormalMap) { normalMapPath = materialInfo.materialPath + "normal.tex"; }

            useMetallicMap = materialInfo.material.GetInt("_UseMetallicMap") == 1f;
            metallic = materialInfo.material.GetFloat("_Metallic");
            if (useMetallicMap) { metallicMapPath = materialInfo.materialPath + "metallic.tex"; }

            useRoughnessMap = materialInfo.material.GetInt("_UseRoughnessMap") == 1f;
            roughness = materialInfo.material.GetFloat("_Roughness");
            if (useRoughnessMap) { roughnessMapPath = materialInfo.materialPath + "roughness.tex"; }

            useEmissiveMap = materialInfo.material.GetInt("_UseEmissiveMap") == 1f;
            emissive = materialInfo.material.GetColor("_Emissive");
            if (useEmissiveMap) { metallicMapPath = materialInfo.materialPath + "emissive.tex"; }

            useAoMap = materialInfo.material.GetInt("_UseAoMap") == 1f;
            if (useAoMap) { aoMapPath = materialInfo.materialPath + "ao.tex"; }

            useOpacityMap = materialInfo.material.GetInt("_UseOpacityMap") == 1f;
            opacity = materialInfo.material.GetFloat("_Opacity");
            if (useOpacityMap) { opacityMapPath = materialInfo.materialPath + "opacity.tex"; }

            uvOffset = materialInfo.material.GetVector("_UvOffset");
            uvScale = materialInfo.material.GetVector("_UvScale");
        }

        public Material CreateMaterial()
        {
            Material material = new Material(SaveManager.GetMaterial(opacity == 1f));

            material.SetFloat("_UseColorMap", useColorMap ? 1f : 0f);
            material.SetColor("_BaseColor", baseColor);
            if (useColorMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "color.tex");
                if (null != texture) { material.SetTexture("_ColorMap", texture); }
            }

            material.SetFloat("_UseNormalMap", useNormalMap ? 1f : 0f);
            if (useNormalMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "normal.tex");
                if (null != texture) { material.SetTexture("_NormalMap", texture); }
            }

            material.SetFloat("_UseMetallicMap", useMetallicMap ? 1f : 0f);
            material.SetFloat("_Metallic", metallic);
            if (useMetallicMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "metallic.tex");
                if (null != texture) { material.SetTexture("_MetallicMap", texture); }
            }

            material.SetFloat("_UseRoughnessMap", useRoughnessMap ? 1f : 0f);
            material.SetFloat("_Roughness", roughness);
            if (useRoughnessMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "roughness.tex");
                if (null != texture) { material.SetTexture("_RoughnessMap", texture); }
            }

            material.SetFloat("_UseEmissiveMap", useEmissiveMap ? 1f : 0f);
            material.SetColor("_Emissive", emissive);
            if (useEmissiveMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "emissive.tex");
                if (null != texture) { material.SetTexture("_EmissiveMap", texture); }
            }

            material.SetFloat("_UseAoMap", useAoMap ? 1f : 0f);
            if (useAoMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "ao.tex");
                if (null != texture) { material.SetTexture("_AoMap", texture); }
            }

            material.SetFloat("_UseOpacityMap", useOpacityMap ? 1f : 0f);
            material.SetFloat("_Opacity", opacity);
            if (useOpacityMap)
            {
                Texture2D texture = Utils.LoadTexture(path + "opacity.tex");
                if (null != texture) { material.SetTexture("_OpacityMap", texture); }
            }

            material.SetVector("_UvOffset", uvOffset);
            material.SetVector("_UvScale", uvScale);

            return material;
        }
    }


    [System.Serializable]
    public class SubMesh
    {
        public MeshTopology topology;
        public int[] indices;
        public MaterialData materialData;
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
                    indices = meshInfo.mesh.GetIndices(i),
                    materialData = new MaterialData(meshInfo.materialsInfo[i])
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
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.subMeshCount = subMeshes.Length;
            for (int i = 0; i < subMeshes.Length; ++i)
            {
                mesh.SetIndices(subMeshes[i].indices, subMeshes[i].topology, i);
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        public Material[] GetMaterials()
        {
            Material[] materials = new Material[subMeshes.Length];
            for (int i = 0; i < subMeshes.Length; ++i)
            {
                materials[i] = subMeshes[i].materialData.CreateMaterial();
            }
            return materials;
        }
    }


    [System.Serializable]
    public class ObjectData
    {
        public string path;
        public string tag;

        // Transform
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        // Mesh
        public string meshPath;
        public bool isImported;

        // Parameters
        public bool lockPosition;
        public bool lockRotation;
        public bool lockScale;

        // Constraints

    }


    [System.Serializable]
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
    }


    [System.Serializable]
    public class CameraData : ObjectData
    {
        public float focal;
        public float focus;
        public float aperture;
        public bool enableDOF;
        public float near;
        public float far;
        public float filmHeight;
    }


    [System.Serializable]
    public class ShotData
    {
        public string name;
        public int index;
        public int start;
        public int end;
        public string cameraName;
    }


    [System.Serializable]
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

        public void Clear()
        {
            objects.Clear();
            lights.Clear();
            cameras.Clear();
            shots.Clear();
        }
    }
}
