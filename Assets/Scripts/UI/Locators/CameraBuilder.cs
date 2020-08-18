using UnityEngine;

namespace VRtist
{
    public class CameraBuilder : GameObjectBuilder
    {
        public override GameObject CreateInstance(GameObject source, Transform parent = null, bool isPrefab = false)
        {
            GameObject newCamera = GameObject.Instantiate(source, parent);
            RenderTexture renderTexture = new RenderTexture(1920 / 2, 1080 / 2, 24, RenderTextureFormat.Default);
            if (null == renderTexture)
                Debug.LogError("CAMERA FAILED");
            renderTexture.name = "Camera RT";

            newCamera.GetComponentInChildren<Camera>(true).targetTexture = renderTexture;
            newCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", renderTexture);

            VRInput.DeepSetLayer(newCamera, 5);

            newCamera.GetComponentInChildren<CameraController>(true).CopyParameters(source.GetComponentInChildren<CameraController>(true));

            return newCamera;
        }
    }
}
