using System;
using System.Collections.Generic;

using UnityEngine;

namespace VRtist.Mixer
{
    public class GreasePencilBuilder : GameObjectBuilder
    {
        public override GameObject CreateInstance(GameObject source, Transform parent = null, bool isPrefab = false)
        {
            GameObject newGreasePencil = GameObject.Instantiate(source, parent);
            newGreasePencil.GetComponent<GreasePencil>().data = source.GetComponent<GreasePencil>().data;

            return newGreasePencil;
        }
    }


    public class GreasePencil : MonoBehaviour
    {
        public GreasePencilData data;
        private int frame = -1;

        private Tuple<Mesh, List<MaterialParameters>> findMesh(int frame)
        {
            int curFrame = -1;
            int firstFrame = -1;

            foreach (int f in data.meshes.Keys)
            {
                if (firstFrame == -1)
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
            int mappedFrame = (int)(frame * data.frameScale) + data.frameOffset;
            if (data.hasCustomRange)
            {
                if (mappedFrame >= data.rangeStartFrame)
                    mappedFrame = ((mappedFrame - data.rangeStartFrame) % (data.rangeEndFrame - data.rangeStartFrame + 1)) + data.rangeStartFrame;
                else
                    mappedFrame = data.rangeEndFrame - ((data.rangeStartFrame - mappedFrame - 1) % (data.rangeEndFrame - data.rangeStartFrame + 1));
            }


            Tuple<Mesh, List<MaterialParameters>> meshData = findMesh(mappedFrame);
            if (null == meshData)
                return;

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (null == meshFilter)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = meshData.Item1;
            meshFilter.mesh.name = gameObject.name;

            MeshCollider collider = gameObject.GetComponent<MeshCollider>();
            if (null != collider)
                GameObject.Destroy(collider);
            gameObject.AddComponent<MeshCollider>();

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (null == meshRenderer)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            MixerUtils.ApplyMaterialParameters(meshRenderer, meshData.Item2);
        }

        // Update is called once per frame
        void Update()
        {
            if (GlobalState.Animation.CurrentFrame == frame)
                return;
            frame = GlobalState.Animation.CurrentFrame;

            ForceUpdate();
        }

    }
}