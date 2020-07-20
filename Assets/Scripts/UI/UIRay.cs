using UnityEngine;

namespace VRtist
{
    public class UIRay : MonoBehaviour
    {
        [ColorUsage(true, true)]
        public Color volumeColor = new Color(0, 38, 64);
        [ColorUsage(true, true)]
        public Color widgetColor = new Color(64, 12, 0);
        [ColorUsage(true, true)]
        public Color panelColor = new Color(0, 64, 12);
        [ColorUsage(true, true)]
        public Color handleColor = new Color(12, 64, 0);
        
        private LineRenderer line = null;
        private Transform endPoint = null;
        private Material rayMat = null;
        private Material endMat = null;

        private void Start()
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                Debug.LogWarning("Cannot find the LineRenderer component in the UIRay");
            }

            endPoint = transform.Find("RayEnd");
            if (endPoint == null)
            {
                Debug.LogWarning("Cannot find the RayEnd object under the UIRay");
            }

            rayMat = line.material;
            endMat = endPoint.gameObject.GetComponent<MeshRenderer>().material;
        }

        public void SetStartPosition(Vector3 start)
        {
            if (line != null)
            {
                line.SetPosition(0, start);
            }
        }

        public void SetEndPosition(Vector3 end)
        {
            if (line != null)
            {
                line.SetPosition(1, end);
            }

            if (endPoint != null)
            {
                endPoint.position = end;
            }
        }

        public void SetPositions(Vector3 begin, Vector3 end)
        {
            if (line != null)
            {
                line.SetPosition(0, begin);
                line.SetPosition(1, end);
            }

            if (endPoint != null)
            {
                endPoint.position = end;
            }
        }

        public void SetVolumeColor()
        {
            SetColor(volumeColor);
        }

        public void SetWidgetColor()
        {
            SetColor(widgetColor);
        }

        public void SetPanelColor()
        {
            SetColor(panelColor);
        }

        public void SetHandleColor()
        {
            SetColor(handleColor);
        }

        public void SetColor(Color color)
        {
            if (rayMat != null)
            {
                rayMat.SetColor("_EmissiveColor", color);

                // NOTE: only setting the color does not change the shader...
                //rayMat.SetColor("_EmissiveColorLDR", color);
                //rayMat.SetFloat("_EmissiveIntensity", 50.0f); // 50 nits, 6.0 HDR, 8.0 EV100
            }

            if (endMat != null)
            {
                endMat.SetColor("_EmissiveColor", color);

                //endMat.SetColor("_EmissiveColorLDR", color);
                //endMat.SetFloat("_EmissiveIntensity", 50.0f);
            }
        }
    }
}
