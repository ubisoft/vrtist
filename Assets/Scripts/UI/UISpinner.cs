using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UISpinner : UIElement
    {
        public enum TextAndValueVisibilityType { ShowTextAndValue, ShowValueOnly };
        public enum SpinnerValueType { Float, Int };

        [SpaceHeader("Spinner Base Shape Parmeters", 6, 0.3f, 0.3f, 0.3f)]
        [CentimeterFloat] public float margin = 0.005f;
        [CentimeterFloat] public float thickness = 0.001f;
        [Percentage] public float separationPositionPct = 0.3f;
        public TextAndValueVisibilityType textAndValueVisibilityType = TextAndValueVisibilityType.ShowTextAndValue;
        public SpinnerValueType spinnerValueType = SpinnerValueType.Float;
        public Color pushedColor = new Color(0f, 0.6549f, 1f);
        
        [SpaceHeader("Subdivision Parameters", 6, 0.3f, 0.3f, 0.3f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Spinner Float Values", 6, 0.3f, 0.3f, 0.3f)]
        public float minFloatValue = 0.0f;
        public float maxFloatValue = 1.0f;
        public float currentFloatValue = 0.5f;
        [Tooltip("Amount of change /m")] public float valueRateFloat = 0.01f; // increment per meter

        [SpaceHeader("Spinner Int Values", 6, 0.3f, 0.3f, 0.3f)]
        public int minIntValue = 0;
        public int maxIntValue = 10;
        public int currentIntValue = 5;
        [Tooltip("Amount of change /m")] public float valueRateInt = 0.01f; // increment per meter

        [SpaceHeader("Callbacks", 6, 0.3f, 0.3f, 0.3f)]
        public FloatChangedEvent onSpinEvent = new FloatChangedEvent();
        public IntChangedEvent onSpinEventInt = new IntChangedEvent();
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        private Vector3 localProjectedWidgetInitialPosition = Vector3.zero;
        private float initialFloatValue = 0.0f;
        private int initialIntValue = 0;

        private bool needRebuild = false;

        public string Text { get { return GetText(); } set { SetText(value); } }
        public float FloatValue { get { return GetFloatValue(); } set { SetFloatValue(value); UpdateValueText(); } }
        public int IntValue { get { return GetIntValue(); } set { SetIntValue(value); UpdateValueText(); } }

        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                onSpinEvent.AddListener(OnSpin);
                onClickEvent.AddListener(OnClickSpinner);
                onReleaseEvent.AddListener(OnReleaseSpinner);
            }
        }

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UISpinner_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
        }

        private void UpdateColliderDimensions()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            if (meshFilter != null && coll != null)
            {
                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                {
                    coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                    coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                }
                else
                {
                    coll.center = initColliderCenter;
                    coll.size = initColliderSize;
                }
            }
        }

        private void UpdateCanvasDimensions()
        {
            Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(width, height);

                bool hasText = (textAndValueVisibilityType == TextAndValueVisibilityType.ShowTextAndValue);

                float textPosLeft = margin;

                Transform textTransform = canvas.transform.Find("Text");
                RectTransform rectText = textTransform.GetComponent<RectTransform>();
                rectText.sizeDelta = new Vector2((width - 2 * margin) * separationPositionPct, height);
                rectText.localPosition = new Vector3(textPosLeft, -height / 2.0f, -0.002f);
                textTransform.gameObject.SetActive(hasText); // hide if ValueOnly


                Transform textValueTransform = canvas.transform.Find("TextValue");
                Text textValue = textValueTransform.GetComponent<Text>();
                textValue.alignment = hasText ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
                RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();
                rectTextValue.sizeDelta = hasText ?
                      new Vector2((width - 2 * margin) * (1 - separationPositionPct), height)
                    : new Vector2(width - 2 * margin, height);
                float textPos = hasText ?
                      width - margin // right
                    : 0.5f * width; // or middle
                rectTextValue.localPosition = new Vector3(textPos, -height / 2.0f, -0.002f);
            }
        }

        public override void ResetMaterial()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material material = UIUtils.LoadMaterial("UIPanel");
                Material materialInstance = Instantiate(material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UISpinner_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_thickness = 0.001f;
            const int min_nbSubdivCornerFixed = 1;
            const int min_nbSubdivCornerPerUnit = 1;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (thickness < min_thickness)
                thickness = min_thickness;
            if (margin > width / 2.0f || margin > height / 2.0f)
                margin = Mathf.Min(width / 2.0f, height / 2.0f);
            if (nbSubdivCornerFixed < min_nbSubdivCornerFixed)
                nbSubdivCornerFixed = min_nbSubdivCornerFixed;
            if (nbSubdivCornerPerUnit < min_nbSubdivCornerPerUnit)
                nbSubdivCornerPerUnit = min_nbSubdivCornerPerUnit;
            if (currentFloatValue < minFloatValue)
                currentFloatValue = minFloatValue;
            if (currentFloatValue > maxFloatValue)
                currentFloatValue = maxFloatValue;
            if (currentIntValue < minIntValue)
                currentIntValue = minIntValue;
            if (currentIntValue > maxIntValue)
                currentIntValue = maxIntValue;

            needRebuild = true;
        }

        private void Update()
        {
            if (needRebuild)
            {
                try
                {
                    RebuildMesh();
                    UpdateLocalPosition();
                    UpdateAnchor();
                    UpdateChildren();
                    UpdateValueText();
                    SetColor(Disabled ? disabledColor : baseColor);
                }
                catch(Exception e)
                {
                    Debug.Log("Exception: " + e);
                }

                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            float widthWithoutMargins = width - 2.0f * margin;

            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));
            Vector3 posTopMiddle = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * separationPositionPct, -margin, -0.001f));
            Vector3 posBottomMiddle = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * separationPositionPct, -height + margin, -0.001f));

            Vector3 eps = new Vector3(0.001f, 0, 0);

            // Full Rect
            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

            // Text
            if (textAndValueVisibilityType == TextAndValueVisibilityType.ShowTextAndValue)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(posTopLeft, posTopMiddle);
                Gizmos.DrawLine(posTopMiddle, posBottomMiddle);
                Gizmos.DrawLine(posBottomMiddle, posBottomLeft);
                Gizmos.DrawLine(posBottomLeft, posTopLeft);
            }

            // Value
            Gizmos.color = Color.red;
            Gizmos.DrawLine(posTopMiddle + eps, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomMiddle + eps);
            Gizmos.DrawLine(posBottomMiddle + eps, posTopMiddle + eps);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        private void UpdateValueText()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                Transform textValueTransform = canvas.transform.Find("TextValue");
                Text txt = textValueTransform.gameObject.GetComponent<Text>();
                if (txt != null)
                {
                    txt.text = spinnerValueType == SpinnerValueType.Float 
                        ? currentFloatValue.ToString("#0.00")
                        : currentIntValue.ToString();
                }
            }
        }

        private string GetText()
        {
            Text text = GetComponentInChildren<Text>();
            if (text != null)
            {
                return text.text;
            }

            return null;
        }

        private void SetText(string textValue)
        {
            Text text = GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = textValue;
            }
        }

        private float GetFloatValue()
        {
            return currentFloatValue;
        }

        private void SetFloatValue(float floatValue)
        {
            currentFloatValue = floatValue;
        }

        private int GetIntValue()
        {
            return currentIntValue;
        }

        private void SetIntValue(int intValue)
        {
            currentIntValue = intValue;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
            }
        }

        public void OnClickSpinner()
        {
            SetColor(Disabled ? disabledColor : pushedColor);
        }

        public void OnReleaseSpinner()
        {
            SetColor(Disabled ? disabledColor : baseColor);
        }

        public void OnSpin(float f)
        {
            //Value = f; // Value already set in HandleCursorBehavior
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCursorColliderCenter);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            // SNAP Y to middle
            localProjectedWidgetPosition.y = -height / 2.0f;

            bool justPushedTriggered = false;
            bool justReleasedTriggered = false;
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.triggerButton,
                () => {
                    justPushedTriggered = true;
                },
                () => {
                    justReleasedTriggered = true;
                }
            );

            if (justPushedTriggered)
            {
                GlobalState.Instance.cursor.LockOnWidget(true);
                localProjectedWidgetInitialPosition = localProjectedWidgetPosition;
                initialFloatValue = FloatValue;
                initialIntValue = IntValue;
            }

            float distanceAlongAxis = localProjectedWidgetPosition.x - localProjectedWidgetInitialPosition.x;
            float floatChange = valueRateFloat * distanceAlongAxis;
            float intChange = valueRateInt * distanceAlongAxis;

            // Actually move the spinner ONLY if RIGHT_TRIGGER is pressed.
            bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
            if (triggerState)
            {
                if (spinnerValueType == SpinnerValueType.Float)
                {
                    FloatValue = initialFloatValue + floatChange;
                    onSpinEvent.Invoke(currentFloatValue);
                }
                else // SpinnerValueType.Int
                {
                    IntValue = initialIntValue + Mathf.FloorToInt(intChange);
                    onSpinEventInt.Invoke(currentIntValue);
                }
            }

            // Haptic intensity as we go deeper into the widget.
            float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            intensity *= intensity; // ease-in

            VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            // Actually MOVE the cursor.
            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;

            if (justReleasedTriggered)
            {
                GlobalState.Instance.cursor.LockOnWidget(false);
            }
        }

        public static UISpinner CreateUISpinner(
            string spinnerName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            float thickness,
            float spinner_separation_pct,
            TextAndValueVisibilityType visibility_type,
            SpinnerValueType value_type,
            float min_spinner_value_float,
            float max_spinner_value_float,
            float cur_spinner_value_float,
            float spinner_value_rate_float,
            int min_spinner_value_int,
            int max_spinner_value_int,
            int cur_spinner_value_int,
            float spinner_value_rate_int,
            Material background_material,
            Color background_color,
            string caption)
        {
            GameObject go = new GameObject(spinnerName);
            go.tag = "UICollider";

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UISpinner uiSpinner = go.AddComponent<UISpinner>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiSpinner.relativeLocation = relativeLocation;
            uiSpinner.transform.parent = parent;
            uiSpinner.transform.localPosition = parentAnchor + relativeLocation;
            uiSpinner.transform.localRotation = Quaternion.identity;
            uiSpinner.transform.localScale = Vector3.one;
            uiSpinner.width = width;
            uiSpinner.height = height;
            uiSpinner.margin = margin;
            uiSpinner.thickness = thickness;
            uiSpinner.separationPositionPct = spinner_separation_pct;
            uiSpinner.textAndValueVisibilityType = visibility_type;
            uiSpinner.spinnerValueType = value_type;
            uiSpinner.minFloatValue = min_spinner_value_float;
            uiSpinner.maxFloatValue = max_spinner_value_float;
            uiSpinner.currentFloatValue = cur_spinner_value_float;
            uiSpinner.valueRateFloat = spinner_value_rate_float;
            uiSpinner.minIntValue = min_spinner_value_int;
            uiSpinner.maxIntValue = max_spinner_value_int;
            uiSpinner.currentIntValue = cur_spinner_value_int;
            uiSpinner.valueRateInt = spinner_value_rate_int;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
                uiSpinner.Anchor = Vector3.zero;
                BoxCollider coll = go.GetComponent<BoxCollider>();
                if (coll != null)
                {
                    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                    if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                    {
                        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                    }
                    else
                    {
                        coll.center = initColliderCenter;
                        coll.size = initColliderSize;
                    }
                    coll.isTrigger = true;
                }
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && background_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(background_material);
                uiSpinner.BaseColor = background_color;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            //
            // CANVAS (to hold the 2 texts)
            //

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiSpinner.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiSpinner.width, uiSpinner.height);
            rt.localPosition = Vector3.zero;

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            bool hasText = (visibility_type == TextAndValueVisibilityType.ShowTextAndValue);

            // Add a Text under the Canvas
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                //t.font = (Font)Resources.Load("MyLocalFont"); // TODO: get modifs from other widgets
                t.text = caption;
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.sizeDelta = new Vector2((uiSpinner.width-2*uiSpinner.margin) * uiSpinner.separationPositionPct, uiSpinner.height);
                float textPosLeft = uiSpinner.margin;
                trt.localPosition = new Vector3(textPosLeft, -uiSpinner.height / 2.0f, -0.002f);

                // hide if ValueOnly
                text.SetActive(hasText);
            }

            // Text VALUE
            {
                GameObject text = new GameObject("TextValue");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                //t.font = (Font)Resources.Load("MyLocalFont");
                t.text = (value_type == SpinnerValueType.Float) ? cur_spinner_value_float.ToString("#0.00") : cur_spinner_value_int.ToString();
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.alignment = hasText ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(1, 1); // top right?
                trt.sizeDelta = hasText ? 
                      new Vector2((uiSpinner.width - 2 * uiSpinner.margin) * (1-uiSpinner.separationPositionPct), uiSpinner.height)
                    : new Vector2(uiSpinner.width - 2 * uiSpinner.margin, uiSpinner.height);
                float textPos = hasText ? 
                      uiSpinner.width - uiSpinner.margin // right
                    : 0.5f * uiSpinner.width; // or middle
                trt.localPosition = new Vector3(textPos, -uiSpinner.height / 2.0f, -0.002f);
            }

            return uiSpinner;
        }
    }
}
