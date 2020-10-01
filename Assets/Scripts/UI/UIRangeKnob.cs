using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIRangeKnob : MonoBehaviour
    {
        public float width;
        public float radius;
        public float depth;

        public ColorReference baseColor = new ColorReference();
        public ColorReference pushedColor = new ColorReference();
        public ColorReference hoveredColor = new ColorReference();
        public ColorReference textColor = new ColorReference();

        public enum TextBehavior { Hidden, Center, Top, Bottom };
        
        private bool isPushed = false;
        private bool isHovered = false;

        public Color BaseColor { get { return baseColor.Value; } }
        public Color PushedColor { get { return pushedColor.Value; } }
        public Color HoveredColor { get { return hoveredColor.Value; } }
        public Color TextColor { get { return textColor.Value; } }

        public bool Pushed { get { return isPushed; } set { isPushed = value; ResetColor(); } }
        public bool Hovered { get { return isHovered; } set { isHovered = value; ResetColor(); } }

        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        public void RebuildMesh(float newWidth, float newKnobRadius, float newKnobDepth, int knobNbSubdivCornerFixed, int knobNbSubdivCornerPerUnit)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            // Make a cylinder using RoundedBox
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(newWidth, 2.0f * newKnobRadius, newKnobRadius, newKnobDepth, knobNbSubdivCornerFixed, knobNbSubdivCornerPerUnit);
            theNewMesh.name = "UIRangeKnob_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            radius = newKnobRadius;
            depth = newKnobDepth;
            nbSubdivCornerFixed = knobNbSubdivCornerFixed;
            nbSubdivCornerPerUnit = knobNbSubdivCornerPerUnit;
        }

        public void ResetColor()
        {
            SetColor( isPushed ? PushedColor
                  : ( isHovered ? HoveredColor
                  : BaseColor));

            // Make the canvas pop front if Hovered.
            Canvas c = transform.Find("Canvas").GetComponent<Canvas>();
            if (c != null)
            {
                RectTransform rt = c.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localPosition = Hovered ? new Vector3(0, 0, -0.003f) : Vector3.zero;
                }
            }
        }

        public void UpdateText(float value, TextBehavior textBehavior)
        {
            // FLOATING TEXT
            Transform textValueTransform = transform.Find("Canvas/TextValue");
            if (textValueTransform != null)
            {
                TextMeshProUGUI t = textValueTransform.GetComponent<TextMeshProUGUI>();
                if (t != null)
                {
                    t.text = value.ToString("#0");

                    RectTransform trt = textValueTransform.GetComponent<RectTransform>();
                    trt.sizeDelta = new Vector2(radius * 1.6f * 100.0f, radius * 1.6f * 100.0f);

                    if (textBehavior == TextBehavior.Hidden)
                    {
                        trt.localPosition = new Vector3(0, 0, -0.002f);
                        t.gameObject.SetActive(false);
                    }
                    else
                    {
                        if (textBehavior == TextBehavior.Center)
                        {
                            trt.localPosition = new Vector3(0.2f * radius, -0.2f * radius, -0.002f);
                        }
                        else if (textBehavior == TextBehavior.Bottom)
                        {
                            trt.localPosition = new Vector3(0.2f * radius, -2.2f * radius, -0.002f);
                        }
                        else if (textBehavior == TextBehavior.Top)
                        {
                            trt.localPosition = new Vector3(0.2f * radius, 1.8f * radius, -0.002f);
                        }
                        t.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void SetColor(Color c)
        {
            GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", c);
        }

        public class CreateArgs
        {
            public Transform parent;
            public string widgetName;
            public Vector3 relativeLocation;
            public float width = 0.0f;
            public float radius;
            public float depth;
            public int nbSubdivCornerFixed = 3;
            public int nbSubdivCornerPerUnit = 3;
            public float initialValue = 0;
            public TextBehavior textBehavior = TextBehavior.Center;
            public Material material;
            public ColorVar baseColor = UIOptions.SliderKnobColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar hoveredColor = UIOptions.SelectedColorVar;
            public ColorVar textColor = UIOptions.ForegroundColorVar;
        }

        public static UIRangeKnob Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";
            go.layer = LayerMask.NameToLayer("UI");

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (input.parent)
            {
                UIElement elem = input.parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIRangeKnob uiRangeKnob = go.AddComponent<UIRangeKnob>();
            uiRangeKnob.transform.parent = input.parent;
            uiRangeKnob.transform.localPosition = parentAnchor + input.relativeLocation;
            uiRangeKnob.transform.localRotation = Quaternion.identity;
            uiRangeKnob.transform.localScale = Vector3.one;
            uiRangeKnob.width = input.width;
            uiRangeKnob.radius = input.radius;
            uiRangeKnob.depth = input.depth;
            uiRangeKnob.nbSubdivCornerFixed = input.nbSubdivCornerFixed;
            uiRangeKnob.nbSubdivCornerPerUnit = input.nbSubdivCornerPerUnit;
            uiRangeKnob.baseColor.useConstant = false;
            uiRangeKnob.baseColor.reference = input.baseColor;
            uiRangeKnob.pushedColor.useConstant = false;
            uiRangeKnob.pushedColor.reference = input.pushedColor;
            uiRangeKnob.hoveredColor.useConstant = false;
            uiRangeKnob.hoveredColor.reference = input.hoveredColor;
            uiRangeKnob.textColor.useConstant = false;
            uiRangeKnob.textColor.reference = input.textColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBoxEx(input.width, 2.0f * input.radius, input.radius, input.depth, input.nbSubdivCornerFixed, input.nbSubdivCornerPerUnit);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(input.material);
                meshRenderer.sharedMaterial.SetColor("_BaseColor", uiRangeKnob.BaseColor);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            //
            // CANVAS (to hold the text)
            //

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiRangeKnob.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiRangeKnob.width, 2.0f * uiRangeKnob.radius);
            rt.localPosition = Vector3.zero; // top left

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            // Text VALUE
            {
                GameObject text = new GameObject("TextValue");
                text.transform.parent = canvas.transform;

                TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
                t.text = input.initialValue.ToString("#0");
                t.enableAutoSizing = true;
                t.fontSizeMin = 1;
                t.fontSizeMax = 500;
                t.characterWidthAdjustment = 50.0f;
                t.fontStyle = FontStyles.Normal;
                t.alignment = TextAlignmentOptions.Center;
                t.enableWordWrapping = false;
                t.overflowMode = TextOverflowModes.Truncate;
                t.color = input.textColor.value;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.sizeDelta = new Vector2(uiRangeKnob.radius * 2.0f * 100.0f, uiRangeKnob.radius * 2.0f * 100.0f);
                if (input.textBehavior == TextBehavior.Hidden)
                {
                    trt.localPosition = new Vector3(0, 0, -0.002f);
                    text.SetActive(false);
                }
                else if (input.textBehavior == TextBehavior.Center)
                {
                    trt.localPosition = new Vector3(0.0f, -uiRangeKnob.radius, -0.002f);
                }
                else if (input.textBehavior == TextBehavior.Bottom)
                {
                    trt.localPosition = new Vector3(0.0f, -2.0f * uiRangeKnob.radius, -0.002f);
                }
                else if (input.textBehavior == TextBehavior.Top)
                {
                    trt.localPosition = new Vector3(0.0f, uiRangeKnob.radius, -0.002f);
                }
            }

            return uiRangeKnob;
        }
    }
}
