using System.Collections.Generic;
using UnityEngine;

namespace VRtist
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
        public float emissive;
        public string emissiveMapPath;

        public bool useAoMap;
        public string aoMapPath;

        public Vector4 uvOffset;
        public Vector4 uvScale;

        public bool useOpacityMap;
        public float opacity;
        public float opacityMapPath;
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

        public MeshData(SaveManager.MeshInfo meshInfo)
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

        // Material
        //public string materialPath;

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

        private List<ObjectData> objects = new List<ObjectData>();
        private List<LightData> lights = new List<LightData>();
        private List<CameraData> cameras = new List<CameraData>();

        private List<ShotData> shots = new List<ShotData>();

        public void AddObject(ObjectData data)
        {
            objects.Add(data);
        }

        public List<ObjectData> GetObjects()
        {
            return objects;
        }

        public void AddLight(LightData data)
        {
            lights.Add(data);
        }

        public List<LightData> GetLights()
        {
            return lights;
        }

        public void AddCamera(CameraData data)
        {
            cameras.Add(data);
        }

        public List<CameraData> GetCameras()
        {
            return cameras;
        }

        public void AddShot(ShotData data)
        {
            shots.Add(data);
        }

        public List<ShotData> GetShots()
        {
            return shots;
        }
    }
}
