using System;
using TMPro;
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

        public static readonly string default_widget_name = "Spinner";
        public static readonly float default_width = 0.13f;
        public static readonly float default_height = 0.03f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly float default_spinner_separation = 0.65f;
        public static readonly UISpinner.TextAndValueVisibilityType default_visibility_type = UISpinner.TextAndValueVisibilityType.ShowTextAndValue;
        public static readonly UISpinner.SpinnerValueType default_value_type = UISpinner.SpinnerValueType.Float;
        public static readonly float default_min_value = 0.0f;
        public static readonly float default_max_value = 1.0f;
        public static readonly float default_current_value = 0.5f;
        public static readonly float default_value_rate = 0.01f;
        public static readonly float default_value_rate_ray = 1f; // per second per meter.
        public static readonly string default_background_material_name = "UIBase";
        public static readonly string default_text = "Spinner";

        [SpaceHeader("Spinner Base Shape Parmeters", 6, 0.3f, 0.3f, 0.3f)]
        [CentimeterFloat] public float margin = 0.005f;
        [CentimeterFloat] public float thickness = 0.001f;
        [Percentage] public float separationPositionPct = 0.3f;
        public TextAndValueVisibilityType textAndValueVisibilityType = TextAndValueVisibilityType.ShowTextAndValue;
        public SpinnerValueType spinnerValueType = SpinnerValueType.Float;
        public Material sourceMaterial = null;
        [TextArea] public string textContent = "";

        [SpaceHeader("Subdivision Parameters", 6, 0.3f, 0.3f, 0.3f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Spinner Float Values", 6, 0.3f, 0.3f, 0.3f)]
        public float minValue = default_min_value;
        public float maxValue = default_max_value;
        public float currentValue = default_current_value;
        [Tooltip("Amount of change /m")] public float valueRate = default_value_rate; // increment per meter
        [Tooltip("Amount of change /s/m")] public float valueRateRay = default_value_rate_ray; // increment per second per meter

        [SpaceHeader("Callbacks", 6, 0.3f, 0.3f, 0.3f)]
        public FloatChangedEvent onSpinEvent = new FloatChangedEvent();
        public IntChangedEvent onSpinEventInt = new IntChangedEvent();
        public UnityEvent onEnterEvent = new UnityEvent();
        public UnityEvent onExitEvent = new UnityEvent();
        public UnityEvent onPressTriggerEvent = new UnityEvent();
        public FloatChangedEvent onReleaseTriggerEvent = new FloatChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private Vector3 localProjectedWidgetInitialPosition = Vector3.zero;
        private float initialFloatValue = 0.0f;

        private bool cursorExitedWidget = true;

        public string Text { get { return textContent; } set { SetText(value); } }
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
                //onEnterEvent.AddListener(OnEnterSpinner);
                //onExitEvent.AddListener(OnExitSpinner);
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
                TextMeshPro text = textTransform.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.text = Text;
                    text.color = TextColor;

                    RectTransform rectText = textTransform.GetComponent<RectTransform>();
                    rectText.sizeDelta = new Vector2((width - 2 * margin) * separationPositionPct * 100.0f, (height - 2.0f * margin) * 100.0f);
                    rectText.localPosition = new Vector3(textPosLeft, -margin, -0.002f);
                    textTransform.gameObject.SetActive(hasText); // hide if ValueOnly
                }

                Transform textValueTransform = canvas.transform.Find("TextValue");
                TextMeshPro textValue = textValueTransform.GetComponent<TextMeshPro>();
                if (textValue != null)
                {
                    textValue.color = TextColor;
                    textValue.alignment = hasText ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                    RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();
                    rectTextValue.sizeDelta = hasText ?
                          new Vector2((width - 2 * margin) * (1 - separationPositionPct) * 100.0f, (height - 2.0f * margin) * 100.0f)
                        : new Vector2((width - 2 * margin) * 100.0f, (height - 2.0f * margin) * 100.0f);
                    float textPos = hasText ?
                          width - margin // right
                        : width - margin; // or middle
                    rectTextValue.localPosition = new Vector3(textPos, -margin, -0.002f);
                }
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

                Material materialInstance = Instantiate(sourceMaterial);

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
            if (currentValue < minValue)
                currentValue = minValue;
            if (currentValue > maxValue)
                currentValue = maxValue;

            // Realign button to parent anchor if we change the thickness.
            if (-thickness != relativeLocation.z)
                relativeLocation.z = -thickness;

            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                try
                {
                    RebuildMesh();
                    UpdateLocalPosition();
                    UpdateAnchor();
                    UpdateChildren();
                    UpdateValueText();
                    ResetColor();
                }
                catch(Exception e)
                {
                    Debug.Log("Exception: " + e);
                }

                NeedsRebuild = false;
            }
        }

        public override void ResetColor()
        {
            base.ResetColor();

            // Make the canvas pop front if Hovered.
            Canvas c = GetComponentInChildren<Canvas>();
            if (c != null)
            {
                RectTransform rt = c.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localPosition = Hovered ? new Vector3(0, 0, -0.003f) : Vector3.zero;
                }
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
                TextMeshPro txt = textValueTransform.gameObject.GetComponent<TextMeshPro>();
                if (txt != null)
                {
                    txt.text = spinnerValueType == SpinnerValueType.Float
                        ? currentValue.ToString("#0.00")
                        : Mathf.RoundToInt(currentValue).ToString();
                }
            }
        }

        private void SetText(string textValue)
        {
            textContent = textValue;

            Transform t = transform.Find("Canvas/Text");
            TextMeshPro text = t.GetComponent<TextMeshPro>();
            if (text != null)
            {
                text.text = textValue;
            }
        }

        private float GetFloatValue()
        {
            return currentValue;
        }

        private void SetFloatValue(float floatValue)
        {
            currentValue = floatValue;
        }

        private int GetIntValue()
        {
            return Mathf.RoundToInt(currentValue);
        }

        private void SetIntValue(int intValue)
        {
            currentValue = (float)intValue;
        }





        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
            {
                return;
            }

            if (otherCollider.gameObject.name == "Cursor")
            {
                cursorExitedWidget = false;
                onEnterEvent.Invoke();
                onClickEvent.Invoke();
                OnEnterSpinner();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {            
            if (NeedToIgnoreCollisionExit())
            {          
                if(otherCollider.gameObject.name == "Cursor")
                    cursorExitedWidget = true;
                return;
            }

            if (otherCollider.gameObject.name == "Cursor")
            {                
                onExitEvent.Invoke();
                onReleaseEvent.Invoke();
                OnExitSpinner();
            }
        }

        public void OnEnterSpinner()
        {
            Pushed = true;
            ResetColor();
        }

        public void OnExitSpinner()
        {
            Pushed = false;
            ResetColor();
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
            VRInput.GetInstantButtonEvent(VRInput.rightController, CommonUsages.triggerButton, ref justPushedTriggered, ref justReleasedTriggered);

            if (justPushedTriggered)
            {
                onPressTriggerEvent.Invoke();

                GlobalState.Instance.cursor.LockOnWidget(true);
                localProjectedWidgetInitialPosition = localProjectedWidgetPosition;
                initialFloatValue = FloatValue;
            }

            float distanceAlongAxis = localProjectedWidgetPosition.x - localProjectedWidgetInitialPosition.x;
            float floatChange = valueRate * distanceAlongAxis;

            // Actually move the spinner ONLY if RIGHT_TRIGGER is pressed.
            bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
            if (triggerState)
            {
                FloatValue = Mathf.Clamp(initialFloatValue + floatChange, minValue, maxValue);
                if (spinnerValueType == SpinnerValueType.Float)
                {
                    onSpinEvent.Invoke(FloatValue);
                }
                else // SpinnerValueType.Int
                {
                    onSpinEventInt.Invoke(IntValue);
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
                onReleaseTriggerEvent.Invoke(spinnerValueType == SpinnerValueType.Float ? FloatValue : (float)IntValue);
                if (cursorExitedWidget)
                    onExitEvent.Invoke();
            }
        }

        // --- RAY API ----------------------------------------------------

        public override void OnRayEnter()
        {
            Hovered = true;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayEnterClicked()
        {
            Hovered = true;
            Pushed = true;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayHover()
        {
            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayHoverClicked()
        {
            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayExit()
        {
            Hovered = false;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayExitClicked()
        {
            Hovered = true; // exiting while clicking shows a hovered button.
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayClick()
        {
            onClickEvent.Invoke();

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayRelease()
        {
            onReleaseEvent.Invoke();

            Hovered = true;
            Pushed = false;
            ResetColor();
        }


        public override bool OverridesRayEndPoint() { return true; }
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            float enter;
            widgetPlane.Raycast(ray, out enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            Vector2 localCenter = new Vector2(width / 2.0f, -height / 2.0f);
            
            float currentDistX = localProjectedWidgetPosition.x - localCenter.x;
            FloatValue += Time.unscaledDeltaTime * valueRateRay * currentDistX;

            localProjectedWidgetPosition.x = localCenter.x;
            localProjectedWidgetPosition.y = localCenter.y;

            if (spinnerValueType == SpinnerValueType.Float) {
                onSpinEvent.Invoke(FloatValue);
            } else {
                onSpinEventInt.Invoke(IntValue);
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        // --- / RAY API ----------------------------------------------------

        //
        // CREATE
        //

        public class CreateArgs
        {
            public Transform parent = null;
            public string widgetName = UISpinner.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UISpinner.default_thickness);
            public float width = UISpinner.default_width;
            public float height = UISpinner.default_height;
            public float margin = UISpinner.default_margin;
            public float thickness = UISpinner.default_thickness;
            public float spinner_separation_pct = UISpinner.default_spinner_separation;
            public TextAndValueVisibilityType visibility_type = UISpinner.default_visibility_type;
            public SpinnerValueType value_type = UISpinner.default_value_type;
            public float min_spinner_value = UISpinner.default_min_value;
            public float max_spinner_value = UISpinner.default_max_value;
            public float cur_spinner_value = UISpinner.default_current_value;
            public float spinner_value_rate = UISpinner.default_value_rate;
            public float spinner_value_rate_ray = UISpinner.default_value_rate_ray;
            public Material background_material = UIUtils.LoadMaterial(UISpinner.default_background_material_name);
            public ColorVar background_color = UIOptions.BackgroundColorVar;
            public ColorVar textColor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public string caption = UISpinner.default_text;
        }

        public static UISpinner Create(CreateArgs input)
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

            UISpinner uiSpinner = go.AddComponent<UISpinner>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiSpinner.relativeLocation = input.relativeLocation;
            uiSpinner.transform.parent = input.parent;
            uiSpinner.transform.localPosition = parentAnchor + input.relativeLocation;
            uiSpinner.transform.localRotation = Quaternion.identity;
            uiSpinner.transform.localScale = Vector3.one;
            uiSpinner.width = input.width;
            uiSpinner.height = input.height;
            uiSpinner.margin = input.margin;
            uiSpinner.thickness = input.thickness;
            uiSpinner.separationPositionPct = input.spinner_separation_pct;
            uiSpinner.textAndValueVisibilityType = input.visibility_type;
            uiSpinner.spinnerValueType = input.value_type;
            uiSpinner.minValue = input.min_spinner_value;
            uiSpinner.maxValue = input.max_spinner_value;
            uiSpinner.currentValue = input.cur_spinner_value;
            uiSpinner.valueRate = input.spinner_value_rate;
            uiSpinner.valueRateRay = input.spinner_value_rate_ray;
            uiSpinner.textContent = input.caption;
            uiSpinner.baseColor.useConstant = false;
            uiSpinner.baseColor.reference = input.background_color;
            uiSpinner.textColor.useConstant = false;
            uiSpinner.textColor.reference = input.textColor;
            uiSpinner.pushedColor.useConstant = false;
            uiSpinner.pushedColor.reference = input.pushedColor;
            uiSpinner.selectedColor.useConstant = false;
            uiSpinner.selectedColor.reference = input.selectedColor;
            uiSpinner.sourceMaterial = input.background_material;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
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
            if (meshRenderer != null && input.background_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(input.background_material);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
                
                uiSpinner.SetColor(input.background_color.value);
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

            bool hasText = (input.visibility_type == TextAndValueVisibilityType.ShowTextAndValue);

            // Add a Text under the Canvas
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                TextMeshPro t = text.AddComponent<TextMeshPro>();
                t.text = input.caption;
                t.enableAutoSizing = true;
                t.fontSizeMin = 1;
                t.fontSizeMax = 500;
                t.fontStyle = FontStyles.Normal;
                t.alignment = TextAlignmentOptions.Left;
                t.color = input.textColor.value;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.sizeDelta = new Vector2(
                    (uiSpinner.width-2*uiSpinner.margin) * uiSpinner.separationPositionPct * 100.0f, 
                    (uiSpinner.height - 2 * uiSpinner.margin) * 100.0f);
                float textPosLeft = uiSpinner.margin;
                trt.localPosition = new Vector3(textPosLeft, -uiSpinner.margin, -0.002f);

                // hide if ValueOnly
                text.SetActive(hasText);
            }

            // Text VALUE
            {
                GameObject text = new GameObject("TextValue");
                text.transform.parent = canvas.transform;

                TextMeshPro t = text.AddComponent<TextMeshPro>();
                t.text = (input.value_type == SpinnerValueType.Float)
                    ? input.cur_spinner_value.ToString("#0.00")
                    : Mathf.RoundToInt(input.cur_spinner_value).ToString();
                t.enableAutoSizing = true;
                t.fontSizeMin = 1;
                t.fontSizeMax = 500;
                t.fontStyle = FontStyles.Normal;
                t.alignment = hasText ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                t.color = input.textColor.value;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(1, 1); // top right?
                trt.sizeDelta = hasText ? 
                    new Vector2(
                        (uiSpinner.width - 2 * uiSpinner.margin) * (1-uiSpinner.separationPositionPct) * 100.0f,
                        (uiSpinner.height - 2 * uiSpinner.margin) * 100.0f)
                    : new Vector2(
                        (uiSpinner.width - 2 * uiSpinner.margin) * 100.0f,
                        (uiSpinner.height - 2 * uiSpinner.margin) * 100.0f);
                float textPos = hasText ?
                      uiSpinner.width - uiSpinner.margin // right
                    : uiSpinner.width - uiSpinner.margin ; // or middle
                trt.localPosition = new Vector3(textPos, -uiSpinner.margin, -0.002f);
            }

            return uiSpinner;
        }
    }
}
