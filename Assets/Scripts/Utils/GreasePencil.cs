using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GreasePencil : MonoBehaviour
    {
        public static int currentFrame = 1;

        private Dictionary<int, Mesh> meshes;
        private int frame = -1;

        private Mesh findMesh(int frame)
        {
            int curFrame = -1;
            foreach(int f in meshes.Keys)
            {
                if (f > frame)
                    break;
                curFrame = f;
            }

            return meshes[curFrame];
        }

        // Update is called once per frame
        void Update()
        {
            if (currentFrame == frame)
                return;

            frame = currentFrame;
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = findMesh(frame);
        }

        public void AddMesh(int frame, Mesh mesh)
        {
            meshes[frame] = mesh;
        }
    }
}