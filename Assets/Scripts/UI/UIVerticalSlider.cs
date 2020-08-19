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
    public class UIVerticalSlider : UIElement
    {
        public enum SliderDataSource { Curve, MinMax };
        public enum SliderTextValueAlign { Left, Right };

        public static readonly string default_widget_name = "New VerticalSlider";
        public static readonly float default_width = 0.025f;
        public static readonly float default_height = 0.15f;
        public static readonly float default_margin = 0.002f;
        public static readonly float default_thickness = 0.001f;
        public static readonly float default_slider_begin = 0.0f;
        public static readonly float default_slider_end = 0.85f;
        public static readonly float default_rail_margin = 0.002f;
        public static readonly float default_rail_thickness = 0.0005f;
        public static readonly float default_knob_radius = 0.0065f;
        public static readonly float default_knob_depth = 0.0025f;
        public static readonly float default_min_value = 0.0f;
        public static readonly float default_max_value = 1.0f;
        public static readonly float default_current_value = 0.5f;
        public static readonly string default_material_name = "UIBase";
        public static readonly string default_rail_material_name = "UISliderRail";
        public static readonly string default_knob_material_name = "UISliderKnob";
        public static readonly string default_text = "Slider";
        public static readonly string default_icon_name = "paint";
        public static readonly SliderTextValueAlign default_text_value_align = SliderTextValueAlign.Left;
        public static readonly SliderDataSource default_data_source = SliderDataSource.MinMax;

        [SpaceHeader("Slider Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public float sliderPositionBegin = default_slider_begin;
        public float sliderPositionEnd = default_slider_end;
        public Material sourceMaterial = null;
        public Material sourceRailMaterial = null;
        public Material sourceKnobMaterial = null;
        [TextArea] public string textContent = "";

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Slider SubComponents Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float railMargin = default_rail_margin;
        [CentimeterFloat] public float railThickness = default_rail_thickness;

        [CentimeterFloat] public float knobRadius = default_knob_radius;
        [CentimeterFloat] public float knobDepth = default_knob_depth;

        [SpaceHeader("Slider Values", 6, 0.8f, 0.8f, 0.8f)]
        public SliderDataSource dataSource = default_data_source;
        public float minValue = default_min_value;
        public float maxValue = default_max_value;
        public float currentValue = default_current_value;
        public AnimationCurve dataCurve = new AnimationCurve(new Keyframe(0, default_min_value), new Keyframe(1, default_min_value));
        public AnimationCurve invDataCurve = null;

        public SliderTextValueAlign textValueAlign = default_text_value_align;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public FloatChangedEvent onSlideEvent = new FloatChangedEvent();
        public IntChangedEvent onSlideEventInt = new IntChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        public UIVerticalSliderRail rail = null;
        public UIVerticalSliderKnob knob = null;

        public float SliderPositionBegin { get { return sliderPositionBegin; } set { sliderPositionBegin = value; RebuildMesh(); } }
        public float SliderPositionEnd { get { return sliderPositionEnd; } set { sliderPositionEnd = value; RebuildMesh(); } }
        public string Text { get { return textContent; } set { SetText(value); } }
        public float Value { get { return GetValue(); } set { SetValue(value); UpdateValueText(); UpdateSliderPosition(); } }

        public bool HasCurveData()
        {
            return (dataSource == SliderDataSource.Curve && dataCurve != null && dataCurve.keys.Length > 0);
        }

        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                onSlideEvent.AddListener(OnSlide);
                //onClickEvent.AddListener(OnClickSlider);
                //onReleaseEvent.AddListener(OnReleaseSlider);
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
                // NOTE: I do all these things because properties can't be called from the inspector.
                try
                {
                    RebuildMesh();
                    UpdateLocalPosition();
                    UpdateAnchor();
                    UpdateChildren();
                    UpdateValueText();
                    BuildInverseCurve();
                    UpdateSliderPosition();
                    ResetColor();
                }
                catch (Exception e)
                {
                    Debug.Log("Exception: " + e);
                }

                NeedsRebuild = false;
            }
        }

        public override void ResetColor()
        {
            base.ResetColor(); // reset color of base mesh
            rail.ResetColor();
            knob.ResetColor();

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
                    image.color = TextColor;
                    RectTransform rt = image.gameObject.GetComponent<RectTransform>();
                    if (rt)
                    {
                        rt.sizeDelta = new Vector2(minSide - 2.0f * margin, minSide - 2.0f * margin);
                        rt.localPosition = new Vector3(margin, -margin, -0.001f);
                    }
                }

                // FLOATING TEXT
                UpdateTextPosition();
            }
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
                    txt.text = currentValue.ToString("#0.00");
                }
            }
        }

        private void UpdateSliderPosition()
        {
            float pct = HasCurveData() ? invDataCurve.Evaluate(currentValue)
                : (currentValue - minValue) / (maxValue - minValue);
            
            float heightWithoutMargins = height - 2.0f * margin;
            float startY = -height + margin + heightWithoutMargins * sliderPositionBegin + railMargin;
            float endY = -height + margin + heightWithoutMargins * sliderPositionEnd - railMargin;
            float posY = startY + pct * (endY - startY);

            Vector3 knobPosition = new Vector3((width / 2.0f) - knobRadius, posY + knobRadius, -knobDepth);

            knob.transform.localPosition = knobPosition;

            // FLOATING TEXT
            UpdateTextPosition();
        }

        private void BuildInverseCurve()
        {
            if (dataCurve == null)
                return;

            // TODO: check c is strictly monotonic and Piecewise linear, log error otherwise

            invDataCurve = new AnimationCurve();
            for (int i = 0; i < dataCurve.keys.Length; i++)
            {
                var kf = dataCurve.keys[i];
                var rkf = new Keyframe(kf.value, kf.time);
                if (kf.inTangent < 0)
                {
                    rkf.inTangent = 1 / kf.outTangent;
                    rkf.outTangent = 1 / kf.inTangent;
                }
                else
                {
                    rkf.inTangent = 1 / kf.inTangent;
                    rkf.outTangent = 1 / kf.outTangent;
                }
                invDataCurve.AddKey(rkf);
            }
        }

        private void UpdateTextPosition()
        {
            Vector3 knobPosition = knob.transform.localPosition;

            // FLOATING TEXT
            Transform textValueTransform = transform.Find("Canvas/TextValue");
            if (textValueTransform != null)
            {
                TextMeshPro text = textValueTransform.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();
                    rectTextValue.sizeDelta = new Vector2(5, knobRadius * 2.0f * 100.0f);
                    if (textValueAlign == SliderTextValueAlign.Left)
                    {
                        text.alignment = TextAlignmentOptions.Right;
                        rectTextValue.pivot = new Vector2(1, 1); // top right
                        rectTextValue.localPosition = new Vector3(-2.0f * margin, knobPosition.y, -0.002f);
                    }
                    else // Right (pour le moment)
                    {
                        text.alignment = TextAlignmentOptions.Left;
                        rectTextValue.pivot = new Vector2(0, 1); // top left
                        rectTextValue.localPosition = new Vector3(width + 2.0f * margin, knobPosition.y, -0.002f);
                    }
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

        private void SetText(string textValue)
        {
            textContent = textValue;

            TextMeshPro text = GetComponentInChildren<TextMeshPro>();
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
                OnClickSlider();
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
                OnReleaseSlider();
            }
        }

        public void OnClickSlider()
        {
            Pushed = true;
            ResetColor();
        }

        public void OnReleaseSlider()
        {
            Pushed = false;
            ResetColor();
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
                    if (HasCurveData())
                    {
                        Value = dataCurve.Evaluate(pct);
                    }
                    else // linear
                    {
                        Value = minValue + pct * (maxValue - minValue); // will replace the slider cursor.
                    }
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

            float heightWithoutMargins = height - 2.0f * margin;
            float startY = -height + margin + heightWithoutMargins * sliderPositionBegin + railMargin;
            float endY = -height + margin + heightWithoutMargins * sliderPositionEnd - railMargin;

            float currentValuePct = (Value - minValue) / (maxValue - minValue);
            float currentKnobPositionY = startY + currentValuePct * (endY - startY);

            // DRAG

            localProjectedWidgetPosition.y = Mathf.Lerp(currentKnobPositionY, localProjectedWidgetPosition.y, GlobalState.Settings.RaySliderDrag);

            // CLAMP
            // SNAP Y top
            if (localProjectedWidgetPosition.y < startY)
                localProjectedWidgetPosition.y = startY;

            // SNAP Y bottom
            if (localProjectedWidgetPosition.y > endY)
                localProjectedWidgetPosition.y = endY;

            // SNAP X to middle
            localProjectedWidgetPosition.x = width / 2.0f;

            // SET

            float pct = (localProjectedWidgetPosition.y - startY) / (endY - startY);
            Value = minValue + pct * (maxValue - minValue); // will replace the slider cursor.
            onSlideEvent.Invoke(currentValue);
            int intValue = Mathf.RoundToInt(currentValue);
            onSlideEventInt.Invoke(intValue);

            // Haptic intensity as we go deeper into the widget.
            //float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            //intensity *= intensity; // ease-in

            //VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            //cursorShapeTransform.position = worldProjectedWidgetPosition;
            rayEndPoint = worldProjectedWidgetPosition;
        }

        // --- / RAY API ----------------------------------------------------

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
            public SliderDataSource dataSource = UIVerticalSlider.default_data_source;
            public float minValue = UIVerticalSlider.default_min_value;
            public float maxValue = UIVerticalSlider.default_max_value;
            public float currentValue = UIVerticalSlider.default_current_value;

            public Material material = UIUtils.LoadMaterial(UIVerticalSlider.default_material_name);
            public Material railMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_rail_material_name);
            public Material knobMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_knob_material_name);

            public ColorVar color = UIOptions.BackgroundColorVar;
            public ColorVar textColor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public ColorVar railColor = UIOptions.SliderRailColorVar;
            public ColorVar knobColor = UIOptions.SliderKnobColorVar;

            public string caption = UIVerticalSlider.default_text;

            public  SliderTextValueAlign textValueAlign = UIVerticalSlider.default_text_value_align;
            public Sprite icon = UIUtils.LoadIcon(UIVerticalSlider.default_icon_name);
        }

        public static void Create(CreateArgs input)
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
            uiSlider.dataSource = input.dataSource;
            uiSlider.minValue = input.minValue;
            uiSlider.maxValue = input.maxValue;
            uiSlider.currentValue = input.currentValue;
            uiSlider.textContent = input.caption;
            uiSlider.sourceMaterial = input.material;
            uiSlider.sourceRailMaterial = input.railMaterial;
            uiSlider.sourceKnobMaterial = input.knobMaterial;
            uiSlider.textValueAlign = input.textValueAlign;
            uiSlider.baseColor.useConstant = false;
            uiSlider.baseColor.reference = input.color;
            uiSlider.textColor.useConstant = false;
            uiSlider.textColor.reference = input.textColor;
            uiSlider.pushedColor.useConstant = false;
            uiSlider.pushedColor.reference = input.pushedColor;
            uiSlider.selectedColor.useConstant = false;
            uiSlider.selectedColor.reference = input.selectedColor;

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
                
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiSlider.SetColor(input.color.value);
            }

            //
            // RAIL
            //

            float railWidth = 2 * uiSlider.railMargin;
            float railHeight = (input.height - 2 * input.margin) * (input.sliderEnd - input.sliderBegin);
            float railThickness = uiSlider.railThickness;
            float railMargin = uiSlider.railMargin;
            Vector3 railPosition = new Vector3(input.width / 2.0f - railMargin, -input.height + input.margin + (input.height - 2 * input.margin) * uiSlider.sliderPositionEnd, -railThickness);

            uiSlider.rail = UIVerticalSliderRail.Create(
                new UIVerticalSliderRail.CreateArgs
                {
                    parent = go.transform,
                    widgetName = "Rail",
                    relativeLocation = railPosition,
                    width = railWidth,
                    height = railHeight,
                    thickness = railThickness,
                    margin = railMargin,
                    material = input.railMaterial,
                    c = input.railColor
                }
            );

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

            uiSlider.knob = UIVerticalSliderKnob.Create(
                new UIVerticalSliderKnob.CreateArgs
                { 
                    widgetName = "Knob", 
                    parent = go.transform, 
                    relativeLocation = knobPosition, 
                    radius = newKnobRadius, 
                    depth = newKnobDepth,
                    material = input.knobMaterial,
                    c = input.knobColor
                }
            );

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
                img.color = input.textColor.value;

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

                TextMeshPro t = text.AddComponent<TextMeshPro>();
                t.text = input.currentValue.ToString("#0.00");
                t.enableAutoSizing = true;
                t.fontSizeMin = 1;
                t.fontSizeMax = 500;
                t.fontStyle = FontStyles.Normal;
                t.alignment = TextAlignmentOptions.Right;
                t.color = input.textColor.value;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(1, 1); // top right?
                trt.sizeDelta = new Vector2(5, uiSlider.knobRadius * 2.0f * 100.0f); // size = 5, enough to hold the 0.00 float.
                float textPosRight = - 2.0f * uiSlider.margin; // TMP: au pif pour le moment
                trt.localPosition = new Vector3(textPosRight, knobPosition.y, -0.002f);
            }
        }
    }
}
