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

            if (!GlobalState.Settings.displayGizmos)
                GlobalState.SetGizmoVisible(newCamera, false);

            // Add UI
            if (isPrefab)
            {
                UISlider focalSlider = UISlider.Create(new UISlider.CreateArgs
                {
                    parent = newCamera.transform.Find("Rotate/UI"),
                    widgetName = "Focal",
                    caption = "Focal",
                    currentValue = 35f
                });
                focalSlider.dataCurve = GlobalState.Settings.focalCurve;
                focalSlider.BuildInverseCurve();
                focalSlider.dataSource = UISlider.SliderDataSource.Curve;
                focalSlider.RelativeLocation = new Vector3(-0.30f, -0.0105f, -UISlider.default_thickness);
                focalSlider.Width = 0.3f;
                focalSlider.Height = 0.03f;
                focalSlider.SetLightLayer(3);  // light layer 2, HUD light
            }

            return newCamera;
        }
    }
}
