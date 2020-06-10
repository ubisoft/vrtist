using System;
using UnityEngine;

namespace VRtist
{
    public class CameraBuilder : GameObjectBuilder
    {
        public RenderTexture renderTexture = null;

        public override GameObject CreateInstance(GameObject source, Transform parent = null, bool isPrefab = false)
        {
            GameObject newCamera = GameObject.Instantiate(source, parent);
            //RenderTexture newRenderTexture = new RenderTexture(renderTexture);
            //RenderTexture newRenderTexture = new RenderTexture(1920,1080,24,RenderTextureFormat.RGB565);
            //RenderTexture newRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.RGB111110Float);
            //RenderTexture newRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.Default);
            RenderTexture newRenderTexture = new RenderTexture(1920/2, 1080/2, 24, RenderTextureFormat.Default);
            if (null == newRenderTexture)
                Debug.LogError("CAMERA FAILED");

            newCamera.GetComponentInChildren<Camera>(true).targetTexture = newRenderTexture;
            newCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", newRenderTexture);

            VRInput.DeepSetLayer(newCamera, 5);

            newCamera.GetComponentInChildren<CameraController>(true).CopyParameters(source.GetComponentInChildren<CameraController>(true));

            return newCamera;
        }
    }

}