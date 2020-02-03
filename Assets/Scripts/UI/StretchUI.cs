using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    [ExecuteInEditMode]
    public class StretchUI : MonoBehaviour
    {
        // NOTE: for tweaking, should be hidden after that.
        [SpaceHeader("Debug parameters", 6, 0.8f, 0.8f, 0.8f)]
        public float borderMarginsPct = 0.1f;
        [CentimeterFloat] public float lineWidth = 0.005f;
        [CentimeterVector3] public Vector3 localOffset = new Vector3(0.0f, 0.02f, 0.0f);
        public Vector3 localRotation = new Vector3(45.0f, 0.0f, 0.0f);

#if UNITY_EDITOR
        Vector3 previousLeft = Vector3.zero;
        Vector3 previousRight = Vector3.right;
        Quaternion previousRotation = Quaternion.identity;
        float previousScale = 1.0f;
        bool needsUpdate = false;
#endif
        LineRenderer line;
        Canvas canvas;

        void Start()
        {
            line = GetComponent<LineRenderer>();
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            canvas = GetComponent<Canvas>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            needsUpdate = true;
        }
#endif

        void Update()
        {
#if UNITY_EDITOR
            if(needsUpdate)
            {
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;

                UpdateLineUI(previousLeft, previousRight, previousRotation, previousScale);

                needsUpdate = false;
            }
#endif
        }

        public void Show(bool doShow)
        {
            gameObject.SetActive(doShow);
        }

        public void UpdateLineUI(Vector3 left, Vector3 right, Quaternion rotation, float scale)
        {
#if UNITY_EDITOR
            Vector3 previousLeft = left;
            Vector3 previousRight = right;
            Quaternion previousRotation = rotation;
            float previousScale = scale;
#endif
            //
            // Stretch bar
            //
            float m = borderMarginsPct; // margin left and right
            float w = 1.0f - 2.0f * m;
            line.SetPosition(0, Vector3.Lerp(left, right, m));
            line.SetPosition(1, Vector3.Lerp(left, right, m + 0.450f * w));
            line.SetPosition(2, Vector3.Lerp(left, right, m + 0.451f * w));
            line.SetPosition(3, Vector3.Lerp(left, right, m + 0.549f * w));
            line.SetPosition(4, Vector3.Lerp(left, right, m + 0.550f * w));
            line.SetPosition(5, Vector3.Lerp(left, right, 1.0f - m));

            //
            // Text
            //
            float fullBarWidth = Vector3.Magnitude(right - left);

            if (canvas != null)
            {
                RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
                Vector3 middlePoint = Vector3.Lerp(left, right, 0.5f);
                canvasRT.localPosition = middlePoint;
                canvasRT.rotation = rotation * Quaternion.Euler(localRotation);
                canvasRT.sizeDelta = new Vector2(fullBarWidth, 1.0f);

                Transform textTransform = canvas.transform.Find("Text");
                RectTransform rectText = textTransform.GetComponent<RectTransform>();

                rectText.sizeDelta = canvasRT.sizeDelta;
                rectText.localPosition = localOffset; // local delta relative to the middle point

                Text txt = textTransform.gameObject.GetComponent<Text>();
                if (txt != null)
                {
                    float invertedScale = 1.0f / scale;
                    txt.text = "x " + invertedScale.ToString("#0.00");
                }
            }
        }
    }
}
