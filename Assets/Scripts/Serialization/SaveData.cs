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
    public class SubMesh
    {
        public MeshTopology topology;
        public int[] indices;
    }


    [System.Serializable]
    public class MeshData
    {
        // MeshSurrogate doesn't work :(
        //public Mesh mesh;

        public MeshData() { }

        public MeshData(Mesh mesh)
        {
            name = mesh.name;
            vertices = mesh.vertices;
            normals = mesh.normals;
            uvs = mesh.uv;
            subMeshes = new SubMesh[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                subMeshes[i] = new SubMesh
                {
                    topology = mesh.GetSubMesh(i).topology,
                    indices = mesh.GetIndices(i)
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

    }


    [System.Serializable]
    public class CameraData : ObjectData
    {

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
    }
}
