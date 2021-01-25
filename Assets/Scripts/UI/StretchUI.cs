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
        [CentimeterVector3] public Vector3 localOffsetTwoHands = new Vector3(-0.008f, -0.005f, 0.0f);
        [CentimeterVector3] public Vector3 localOffsetOneHand = new Vector3(-0.0086f, -0.0344f, -0.0253f);
        public Vector3 localRotation = new Vector3(45.0f, 0.0f, 0.0f);

        public enum LineMode { SINGLE, DOUBLE };
        [HideInInspector]
        public LineMode lineMode = LineMode.SINGLE;

        LineRenderer line;
        Canvas canvas;

        void Start()
        {
            line = GetComponent<LineRenderer>();
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            canvas = GetComponent<Canvas>();
        }

        public void Show(bool doShow, LineMode mode = LineMode.SINGLE)
        {
            lineMode = mode;
            // NOTE: Show() can be called before Start(), and line may not has been GetComponent<>'d yet.
            if (line != null)
            {
                line.enabled = (mode == LineMode.DOUBLE); // hide line when doing one hand manipulation
            }

            gameObject.SetActive(doShow);
        }

        public void UpdateLineUI(Vector3 left, Vector3 right, Quaternion rotation, float scale)
        {
            //
            // Stretch bar
            //
            if (line != null)
            {
                if (lineMode == LineMode.SINGLE)
                {
                    line.SetPosition(0, left);
                    line.SetPosition(1, right);
                }
                else
                {
                    float m = borderMarginsPct; // margin left and right
                    float w = 1.0f - 2.0f * m;
                    line.SetPosition(0, Vector3.Lerp(left, right, m));
                    line.SetPosition(1, Vector3.Lerp(left, right, 1.0f - m));
                }

                line.startWidth = lineWidth * scale;
                line.endWidth = lineWidth * scale;
            }

            //
            // Text
            //
            float fullBarWidth = Vector3.Magnitude(right - left);

            if (canvas != null)
            {
                RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
                canvasRT.localScale = Vector3.one * scale;
                Vector3 middlePoint = Vector3.Lerp(left, right, 0.5f);
                canvasRT.localPosition = middlePoint;
                canvasRT.rotation = rotation * Quaternion.Euler(localRotation);
                canvasRT.sizeDelta =
                    (lineMode == LineMode.SINGLE)
                    ? new Vector2(fullBarWidth, 1.0f)
                    : new Vector2(3.0f, 1.0f);

                Transform textTransform = canvas.transform.Find("Text");
                RectTransform rectText = textTransform.GetComponent<RectTransform>();

                rectText.sizeDelta = canvasRT.sizeDelta;
                rectText.localPosition = (lineMode == LineMode.SINGLE) ? localOffsetOneHand : localOffsetTwoHands; // local delta relative to the middle point

                Text txt = textTransform.gameObject.GetComponent<Text>();
                if (txt != null)
                {
                    // TODO: add an icon, change its colors.
                    if (scale < 1.0f)
                    {
                        float invertedScale = 1.0f / scale;
                        txt.text = "x " + invertedScale.ToString("#0.00");
                    }
                    else
                    {
                        txt.text = "x " + scale.ToString("#0.00");
                    }
                }
            }
        }
    }
}
