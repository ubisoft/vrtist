using UnityEngine;

namespace VRtist
{
    public class UIRay : MonoBehaviour
    {
        [GradientUsage(true)]
        public Gradient volumeColor = default;
        [GradientUsage(true)]
        public Gradient widgetColor = default;
        [GradientUsage(true)]
        public Gradient panelColor = default;
        [GradientUsage(true)]
        public Gradient handleColor = default;

        //[ColorUsage(true, true)]
        //public Color volumeColor = new Color(0, 38, 64);
        //[ColorUsage(true, true)]
        //public Color widgetColor = new Color(64, 12, 0);
        //[ColorUsage(true, true)]
        //public Color panelColor = new Color(0, 64, 12);
        //[ColorUsage(true, true)]
        //public Color handleColor = new Color(12, 64, 0);

        private LineRenderer line = null;
        private Transform endPoint = null;
        private Material rayMat = null;
        private Material endMat = null;
        
        //[Range(0,1)]
        //public float pct_1 = 0.10f;

        //[Range(0, 1)]
        //public float pct_2 = 0.90f;

        [Range(1, 100)]
        public float startWidth = 100;

        [Range(1, 100)]
        public float endWidth = 1;

        private float f = 10000f;

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

            endPoint.gameObject.SetActive(false);

            rayMat = line.material;
            endMat = endPoint.gameObject.GetComponent<MeshRenderer>().material;
        }

        public void SetStartPosition(Vector3 start)
        {
            if (line != null)
            {
                line.SetPosition(0, start);
                Vector3 end = line.GetPosition(10);

                for (int i = 1; i < 10; ++i)
                {
                    float pct = ((float)i / 10.0f);
                    line.SetPosition(i, start + (end - start) * pct);
                }

                line.startWidth = startWidth / f;
                line.endWidth = endWidth / f;
            }
        }

        public void SetEndPosition(Vector3 end)
        {
            if (line != null)
            {
                line.SetPosition(10, end);
                Vector3 start = line.GetPosition(0);

                for (int i = 1; i < 10; ++i)
                {
                    float pct = ((float)i / 10.0f);
                    line.SetPosition(i, start + (end - start) * pct);
                }

                line.startWidth = startWidth / f;
                line.endWidth = endWidth / f;
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

        public void SetColor(Gradient color)
        {
            line.colorGradient = color;

            //rayMat.SetColor("_EmissiveColor", Color.black);

            //if (rayMat != null)
            //{
            //    rayMat.SetColor("_EmissiveColor", color);
            //}

            //if (endMat != null)
            //{
            //    endMat.SetColor("_EmissiveColor", color);
            //}
        }
    }
}
