using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CameraBuilder : GameObjectBuilder
    {
        public RenderTexture renderTexture = null;

        public override GameObject CreateInstance(GameObject source, Transform parent = null)
        {
            GameObject newCamera = GameObject.Instantiate(source, parent);

            RenderTexture newRenderTexture = new RenderTexture(renderTexture);
            newCamera.transform.Find("Camera").GetComponent<Camera>().targetTexture = newRenderTexture;
            newCamera.transform.Find("CameraPreview").gameObject.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", newRenderTexture);

            VRInput.DeepSetLayer(newCamera, 5);
            return newCamera;
        }
    }

}