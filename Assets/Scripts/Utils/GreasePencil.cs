using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GreasePencilBuilder : GameObjectBuilder
    {
        public override GameObject CreateInstance(GameObject source, Transform parent = null)
        {
            GameObject newGreasePencil = GameObject.Instantiate(source, parent);
            newGreasePencil.GetComponent<GreasePencil>().data = source.GetComponent<GreasePencil>().data;
            return newGreasePencil;
        }
    }

    public class GreasePencilData
    {
        public Dictionary<int, Tuple<Mesh, Material[]>> meshes = new Dictionary<int, Tuple<Mesh, Material[]>>();

        public void AddMesh(int frame, Tuple<Mesh, Material[]> mesh)
        {
            meshes[frame] = mesh;
        }
    }

    public class GreasePencil : MonoBehaviour
    {
        public static int currentFrame = 1;

        public GreasePencilData data;
        private int frame = -1;
       
        private Tuple<Mesh, Material[]> findMesh(int frame)
        {
            int curFrame = -1;
            int firstFrame = -1;

            foreach(int f in data.meshes.Keys)
            {
                if(firstFrame == -1)
                    firstFrame = f;
                if (f > frame)
                    break;
                curFrame = f;
            }

            if (firstFrame == -1)
                return null;

            if (curFrame == -1)
                curFrame = firstFrame;

            return data.meshes[curFrame];
        }

        public void ForceUpdate()
        {
            Tuple<Mesh, Material[]> meshData = findMesh(frame);
            if (null == meshData)
                return;

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (null == meshFilter)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = meshData.Item1;

            MeshCollider collider = gameObject.GetComponent<MeshCollider>();
            if (null != collider)
                GameObject.Destroy(collider);
            gameObject.AddComponent<MeshCollider>();

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (null == meshRenderer)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.materials = meshData.Item2;
        }

        // Update is called once per frame
        void Update()
        {
            if (currentFrame == frame)
                return;
            frame = currentFrame;

            ForceUpdate();
        }

    }
}