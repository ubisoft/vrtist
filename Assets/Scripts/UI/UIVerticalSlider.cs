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
    public class UIVerticalSlider : UIElement
    {
        public enum SliderTextValueAlign { Left, Right };

        private static readonly string default_widget_name = "New VerticalSlider";
        private static readonly float default_width = 0.025f;
        private static readonly float default_height = 0.15f;
        private static readonly float default_margin = 0.002f;
        private static readonly float default_thickness = 0.001f;
        private static readonly float default_slider_begin = 0.0f;
        private static readonly float default_slider_end = 0.85f;
        private static readonly float default_rail_margin = 0.002f;
        private static readonly float default_rail_thickness = 0.0005f;
        private static readonly float default_knob_radius = 0.0065f;
        private static readonly float default_knob_depth = 0.0025f;
        private static readonly float default_min_value = 0.0f;
        private static readonly float default_max_value = 1.0f;
        private static readonly float default_current_value = 0.5f;
        private static readonly string default_material_name = "UIBase";
        private static readonly string default_rail_material_name = "UISliderRail";
        private static readonly string default_knob_material_name = "UISliderKnob";
        private static readonly Color default_color = UIElement.default_background_color;
        private static readonly Color default_rail_color = UIElement.default_slider_rail_color;
        private static readonly Color default_knob_color = UIElement.default_slider_knob_color;
        private static readonly string default_text = "Slider";
        private static readonly string default_icon_name = "paint";
        private static readonly SliderTextValueAlign default_text_value_align = SliderTextValueAlign.Left;

        [SpaceHeader("Slider Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public float sliderPositionBegin = default_slider_begin;
        public float sliderPositionEnd = default_slider_end;
        public Color pushedColor = UIElement.default_pushed_color;
        public Material sourceMaterial = null;
        public Material sourceRailMaterial = null;
        public Material sourceKnobMaterial = null;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Slider SubComponents Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float railMargin = default_rail_margin;
        [CentimeterFloat] public float railThickness = default_rail_thickness;

        [CentimeterFloat] public float knobRadius = default_knob_radius;
        [CentimeterFloat] public float knobDepth = default_knob_depth;

        [SpaceHeader("Slider Values", 6, 0.8f, 0.8f, 0.8f)]
        public float minValue = default_min_value;
        public float maxValue = default_max_value;
        public float currentValue = default_current_value;

        public SliderTextValueAlign textValueAlign = default_text_value_align;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public FloatChangedEvent onSlideEvent = new FloatChangedEvent();
        public IntChangedEvent onSlideEventInt = new IntChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        [SerializeField] private UIVerticalSliderRail rail = null;
        [SerializeField] private UIVerticalSliderKnob knob = null;

        private bool needRebuild = false;

        public float SliderPositionBegin { get { return sliderPositionBegin; } set { sliderPositionBegin = value; RebuildMesh(); } }
        public float SliderPositionEnd { get { return sliderPositionEnd; } set { sliderPositionEnd = value; RebuildMesh(); } }
        public string Text { get { return GetText(); } set { SetText(value); } }
        public float Value { get { return GetValue(); } set { SetValue(value); UpdateValueText(); UpdateSliderPosition(); } }

        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                onSlideEvent.AddListener(OnSlide);
                onClickEvent.AddListener(OnClickSlider);
                onReleaseEvent.AddListener(OnReleaseSlider);
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

            needRebuild = true;
        }

        private void Update()
        {
            // NOTE: rebuild when touching a property in the inspector.
            // Boolean needRebuild is set in OnValidate();
            // The rebuild method called when using the gizmos is: Width and Height
            // properties in UIElement.
            // This comment is probably already obsolete.
            if (needRebuild)
            {
                // NOTE: I do all these things because properties can't be called from the inspector.
                try
                {
                    RebuildMesh();
                    UpdateLocalPosition();
                    UpdateAnchor();
                    UpdateChildren();
                    UpdateValueText();
                    UpdateSliderPosition();
                    SetColor(Disabled ? disabledColor : baseColor);
                }
                catch (Exception e)
                {
                    Debug.Log("Exception: " + e);
                }

                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            float heightWithoutMargins = height - 2.0f * margin;

            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));
            Vector3 posSliderTopLeft = transform.TransformPoint(new Vector3(margin, -height + margin + heightWithoutMargins * sliderPositionEnd, -0.001f));
            Vector3 posSliderTopRight = transform.TransformPoint(new Vector3(width - margin, -height + margin + heightWithoutMargins * sliderPositionEnd, -0.001f));
            Vector3 posSliderBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin + heightWithoutMargins * sliderPositionBegin, -0.001f));
            Vector3 posSliderBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin + heightWithoutMargins * sliderPositionBegin, -0.001f));

            Vector3 eps = new Vector3(0, 0.001f, 0);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posSliderTopRight);
            Gizmos.DrawLine(posSliderTopRight, posSliderTopLeft);
            Gizmos.DrawLine(posSliderTopLeft, posTopLeft);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(posSliderTopLeft - eps, posSliderTopRight - eps);
            Gizmos.DrawLine(posSliderTopRight - eps, posSliderBottomRight);
            Gizmos.DrawLine(posSliderBottomRight, posSliderBottomLeft);
            Gizmos.DrawLine(posSliderBottomLeft, posSliderTopLeft - eps);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(posSliderBottomLeft - eps, posSliderBottomRight - eps);
            Gizmos.DrawLine(posSliderBottomRight - eps, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posSliderBottomLeft - eps);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        public override void RebuildMesh()
        {
            // RAIL - pivot is at top-left
            Vector3 railPosition = new Vector3(width / 2.0f - railMargin, -height + margin + (height - 2 * margin) * sliderPositionEnd, -railThickness);
            float railWidth = 2 * railMargin; // no inner rectangle, only margin driven rounded borders.
            float railHeight = (height - 2 * margin) * (sliderPositionEnd - sliderPositionBegin);

            rail.RebuildMesh(railWidth, railHeight, railThickness, railMargin);
            rail.transform.localPosition = railPosition;

            // KNOB
            float newKnobRadius = knobRadius;
            float newKnobDepth = knobDepth;

            knob.RebuildMesh(newKnobRadius, newKnobDepth); // TODO: + text
            
            // BASE
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UISlider_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
            UpdateSliderPosition();
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

                float minSide = Mathf.Min(width, height);

                // IMAGE
                Image image = canvas.GetComponentInChildren<Image>();
                if (image != null)
                {
                    RectTransform rt = image.gameObject.GetComponent<RectTransform>();
                    if (rt)
                    {
                        rt.sizeDelta = new Vector2(minSide - 2.0f * margin, minSide - 2.0f * margin);
                        rt.localPosition = new Vector3(margin, -margin, -0.001f);
                    }
                }

                // FLOATING TEXT
                Transform textValueTransform = canvas.transform.Find("TextValue");
                Text text = textValueTransform.GetComponent<Text>();
                RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();
                //rectTextValue.sizeDelta = new Vector2((width - 2 * margin) * (1 - sliderPositionEnd), height); // changing canvas size does not change the floating text dimensions.
                if (textValueAlign == SliderTextValueAlign.Left)
                {
                    text.alignment = TextAnchor.MiddleRight;
                    rectTextValue.pivot = new Vector2(1, 1); // top right
                    rectTextValue.localPosition = new Vector3(-2.0f * margin, height / 2.0f, -0.002f);
                }
                else // Right (pour le moment)
                {
                    text.alignment = TextAnchor.MiddleLeft;
                    rectTextValue.pivot = new Vector2(0, 1); // top left
                    rectTextValue.localPosition = new Vector3(width + 2.0f * margin, height / 2.0f, -0.002f);
                }
                
            }
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
                    txt.text = currentValue.ToString("#0.00");
                }
            }
        }

        private void UpdateSliderPosition()
        {
            float pct = (currentValue - minValue) / (maxValue - minValue);

            float heightWithoutMargins = height - 2.0f * margin;
            float startY = -height + margin + heightWithoutMargins * sliderPositionBegin + railMargin;
            float endY = -height + margin + heightWithoutMargins * sliderPositionEnd - railMargin;
            float posY = startY + pct * (endY - startY);

            Vector3 knobPosition = new Vector3((width / 2.0f) - knobRadius, posY + knobRadius, -knobDepth);

            knob.transform.localPosition = knobPosition;

            // FLOATING TEXT
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                Transform textValueTransform = canvas.transform.Find("TextValue");
                Text text = textValueTransform.GetComponent<Text>();
                RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();
                //trt.sizeDelta = new Vector2(1, uiSlider.knobRadius * 2.0f); // TODO: add a variable for the floating text dimensions.
                if (textValueAlign == SliderTextValueAlign.Left)
                {
                    text.alignment = TextAnchor.MiddleRight;
                    rectTextValue.pivot = new Vector2(1, 1); // top right
                    rectTextValue.localPosition = new Vector3(-2.0f * margin, knobPosition.y - knobRadius, -0.002f);
                }
                else // Right (pour le moment)
                {
                    text.alignment = TextAnchor.MiddleLeft;
                    rectTextValue.pivot = new Vector2(0, 1); // top left
                    rectTextValue.localPosition = new Vector3(width + 2.0f * margin, knobPosition.y - knobRadius, -0.002f);
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
                sharedMaterialInstance.name = "UIBase_Instance_for_UIVerticalSlider";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }

            meshRenderer = rail.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = rail.Color;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material materialInstance = Instantiate(sourceRailMaterial);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UISliderRail_Instance_for_UIVerticalSliderRail";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }

            meshRenderer = knob.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = knob.Color;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material materialInstance = Instantiate(sourceKnobMaterial);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UISliderKnob_Instance_for_UIVerticalSliderKnob";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
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

        private float GetValue()
        {
            return currentValue;
        }

        private void SetValue(float floatValue)
        {
            currentValue = floatValue;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                // HIDE cursor

                onClickEvent.Invoke();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                // SHOW cursor

                onReleaseEvent.Invoke();
            }
        }

        public void OnClickSlider()
        {
            SetColor(Disabled ? disabledColor : pushedColor);
        }

        public void OnReleaseSlider()
        {
            SetColor(Disabled ? disabledColor : baseColor);
        }

        public void OnSlide(float f)
        {
            //Value = f; // Value already set in HandleCursorBehavior
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCursorColliderCenter);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            float heightWithoutMargins = height - 2.0f * margin;
            float startY = -height + margin + heightWithoutMargins * sliderPositionBegin + railMargin;
            float endY   = -height + margin + heightWithoutMargins * sliderPositionEnd   - railMargin;

            float snapYDistance = 0.002f;

            if (localProjectedWidgetPosition.y > startY - snapYDistance && localProjectedWidgetPosition.y < endY + snapYDistance)
            {
                // SNAP Y top
                if (localProjectedWidgetPosition.y < startY)
                    localProjectedWidgetPosition.y = startY;

                // SNAP Y bottom
                if (localProjectedWidgetPosition.y > endY)
                    localProjectedWidgetPosition.y = endY;

                // SNAP X to middle
                localProjectedWidgetPosition.x = width / 2.0f;

                float pct = (localProjectedWidgetPosition.y - startY) / (endY - startY);

                // Actually move the slider ONLY if RIGHT_TRIGGER is pressed.
                bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
                if (triggerState)
                {
                    Value = minValue + pct * (maxValue - minValue); // will replace the slider cursor.
                    onSlideEvent.Invoke(Value);
                    int intValue = Mathf.RoundToInt(Value);
                    onSlideEventInt.Invoke(intValue);
                }

                // Haptic intensity as we go deeper into the widget.
                float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                intensity *= intensity; // ease-in

                VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;
        }

        public class CreateArgs
        {
            public Transform parent = null;
            public string widgetName = UIVerticalSlider.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIVerticalSlider.default_thickness);
            public float width = UIVerticalSlider.default_width;
            public float height = UIVerticalSlider.default_height;
            public float margin = UIVerticalSlider.default_margin;
            public float thickness = UIVerticalSlider.default_thickness;

            public float sliderBegin = UIVerticalSlider.default_slider_begin;
            public float sliderEnd = UIVerticalSlider.default_slider_end;
            public float railMargin = UIVerticalSlider.default_rail_margin;
            public float railThickness = UIVerticalSlider.default_rail_thickness;
            public float knobRadius = UIVerticalSlider.default_knob_radius;
            public float knobDepth = UIVerticalSlider.default_knob_depth;
            public float minValue = UIVerticalSlider.default_min_value;
            public float maxValue = UIVerticalSlider.default_max_value;
            public float currentValue = UIVerticalSlider.default_current_value;

            public Material material = UIUtils.LoadMaterial(UIVerticalSlider.default_material_name);
            public Material railMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_rail_material_name);
            public Material knobMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_knob_material_name);

            public Color color = UIVerticalSlider.default_color;
            public Color railColor = UIVerticalSlider.default_rail_color;
            public Color knobColor = UIVerticalSlider.default_knob_color;

            public string caption = UIVerticalSlider.default_text;

            public  SliderTextValueAlign textValueAlign = UIVerticalSlider.default_text_value_align;
            public Sprite icon = UIUtils.LoadIcon(UIVerticalSlider.default_icon_name);
        }

        public static void Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";

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

            UIVerticalSlider uiSlider = go.AddComponent<UIVerticalSlider>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiSlider.relativeLocation = input.relativeLocation;
            uiSlider.transform.parent = input.parent;
            uiSlider.transform.localPosition = parentAnchor + input.relativeLocation;
            uiSlider.transform.localRotation = Quaternion.identity;
            uiSlider.transform.localScale = Vector3.one;
            uiSlider.width = input.width;
            uiSlider.height = input.height;
            uiSlider.margin = input.margin;
            uiSlider.thickness = input.thickness;
            uiSlider.sliderPositionBegin = input.sliderBegin;
            uiSlider.sliderPositionEnd = input.sliderEnd;
            uiSlider.railMargin = input.railMargin;
            uiSlider.railThickness = input.railThickness;
            uiSlider.knobRadius = input.knobRadius;
            uiSlider.knobDepth = input.knobDepth;
            uiSlider.minValue = input.minValue;
            uiSlider.maxValue = input.maxValue;
            uiSlider.currentValue = input.currentValue;
            uiSlider.sourceMaterial = input.material;
            uiSlider.sourceRailMaterial = input.railMaterial;
            uiSlider.sourceKnobMaterial = input.knobMaterial;
            uiSlider.textValueAlign = input.textValueAlign;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiSlider.Anchor = Vector3.zero;
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
            if (meshRenderer != null && input.material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(input.material);
                uiSlider.BaseColor = input.color;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            //
            // RAIL
            //

            float railWidth = 2 * uiSlider.railMargin;
            float railHeight = (input.height - 2 * input.margin) * (input.sliderEnd - input.sliderBegin);
            float railThickness = uiSlider.railThickness;
            float railMargin = uiSlider.railMargin;
            Vector3 railPosition = new Vector3(input.width / 2.0f - railMargin, -input.height + input.margin + (input.height - 2 * input.margin) * uiSlider.sliderPositionEnd, -railThickness);

            uiSlider.rail = UIVerticalSliderRail.Create("Rail", go.transform, railPosition, railWidth, railHeight, railThickness, railMargin, input.railMaterial, input.railColor);

            //
            // KNOB
            //

            float newKnobRadius = uiSlider.knobRadius;
            float newKnobDepth = uiSlider.knobDepth;

            float pct = (uiSlider.currentValue - uiSlider.minValue) / (uiSlider.maxValue - uiSlider.minValue);
            float heightWithoutMargins = input.height - 2.0f * input.margin;
            float startY = -input.height + input.margin + heightWithoutMargins * uiSlider.sliderPositionBegin + railMargin;
            float endY = -input.height + input.margin + heightWithoutMargins * uiSlider.sliderPositionEnd - railMargin;
            float posY = startY + pct * (endY - startY);

            Vector3 knobPosition = new Vector3((input.width / 2.0f) - uiSlider.knobRadius, posY + uiSlider.knobRadius, -uiSlider.knobDepth);

            uiSlider.knob = UIVerticalSliderKnob.Create("Knob", go.transform, knobPosition, newKnobRadius, newKnobDepth, input.knobMaterial, input.knobColor);

            //
            // CANVAS (to hold the image)
            //

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiSlider.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiSlider.width, uiSlider.height);
            rt.localPosition = Vector3.zero; // top left

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            float minSide = Mathf.Min(uiSlider.width, uiSlider.height);

            // Add an Image under the Canvas
            if (input.icon != null)
            {
                GameObject image = new GameObject("Image");
                image.transform.parent = canvas.transform;

                Image img = image.AddComponent<Image>();
                img.sprite = input.icon;

                RectTransform trt = image.GetComponent<RectTransform>();
                trt.localScale = Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                // TODO: non square icons ratio...
                trt.sizeDelta = new Vector2(minSide - 2.0f * input.margin, minSide - 2.0f * input.margin);
                trt.localPosition = new Vector3(input.margin, -input.margin, -0.001f); // top-left minus margins
            }

            // Text VALUE
            {
                GameObject text = new GameObject("TextValue");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                t.text = input.currentValue.ToString("#0.00");
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleRight;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(1, 1); // top right?
                //trt.sizeDelta = new Vector2((uiSlider.width - 2 * uiSlider.margin) * (1-uiSlider.sliderPositionEnd), uiSlider.height);
                trt.sizeDelta = new Vector2(1, uiSlider.knobRadius * 2.0f);
                float textPosRight = - 2.0f * uiSlider.margin; // TMP: au pif pour le moment
                trt.localPosition = new Vector3(textPosRight, knobPosition.y - input.knobRadius, -0.002f);
            }
        }
    }
}
