using System;
using TMPro;
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
    public class UISlider : UIElement
    {
        public enum SliderDataSource { Curve, MinMax };

        public static readonly string default_widget_name = "New Slider";
        public static readonly float default_width = 0.3f;
        public static readonly float default_height = 0.03f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly float default_slider_begin = 0.3f;
        public static readonly float default_slider_end = 0.8f;
        public static readonly float default_rail_margin = 0.004f;
        public static readonly float default_rail_thickness = 0.001f;
        public static readonly float default_knob_radius = 0.01f;
        public static readonly float default_knob_depth = 0.005f;
        public static readonly float default_min_value = 0.0f;
        public static readonly float default_max_value = 1.0f;
        public static readonly float default_current_value = 0.5f;
        public static readonly string default_material_name = "UIBase";
        public static readonly string default_rail_material_name = "UISliderRail";
        public static readonly string default_knob_material_name = "UISliderKnob";
        public static readonly string default_text = "Slider";
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

        // TODO: precision, step?

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public FloatChangedEvent onSlideEvent = new FloatChangedEvent();
        public IntChangedEvent onSlideEventInt = new IntChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        public UISliderRail rail = null;
        public UISliderKnob knob = null;

        public float SliderPositionBegin { get { return sliderPositionBegin; } set { sliderPositionBegin = value; RebuildMesh(); } }
        public float SliderPositionEnd { get { return sliderPositionEnd; } set { sliderPositionEnd = value; RebuildMesh(); } }
        public string Text { get { return textContent; } set { SetText(value); } }
        public float Value { get { return GetValue(); } set { SetValue(value); UpdateValueText(); UpdateSliderPosition(); } }

        public bool HasCurveData()
        {
            return (dataSource == SliderDataSource.Curve && dataCurve != null && dataCurve.keys.Length > 0);
        }

        public override void RebuildMesh()
        {
            // RAIL
            Vector3 railPosition = new Vector3(margin + (width - 2 * margin) * sliderPositionBegin, railMargin - height / 2, -railThickness);
            float railWidth = (width - 2 * margin) * (sliderPositionEnd - sliderPositionBegin);
            float railHeight = 2 * railMargin; // no inner rectangle, only margin driven rounded borders.

            rail.RebuildMesh(railWidth, railHeight, railThickness, railMargin);
            rail.transform.localPosition = railPosition;

            // KNOB
            float newKnobRadius = knobRadius;
            float newKnobDepth = knobDepth;

            knob.RebuildMesh(newKnobRadius, newKnobDepth);
            
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

                float textPosRight = width - margin;
                float textPosLeft = margin;

                Transform textTransform = canvas.transform.Find("Text");
                TextMeshPro text = textTransform.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.text = textContent;
                    text.color = TextColor;

                    RectTransform rectText = textTransform.GetComponent<RectTransform>();
                    rectText.sizeDelta = new Vector2((width - 2 * margin) * sliderPositionBegin * 100.0f, (height - 2.0f * margin) * 100.0f);
                    rectText.localPosition = new Vector3(textPosLeft, -margin, -0.002f);
                }

                Transform textValueTransform = canvas.transform.Find("TextValue");
                TextMeshPro textValue = textValueTransform.GetComponent<TextMeshPro>();
                if (textValue != null)
                {
                    textValue.color = TextColor;

                    RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();
                    rectTextValue.sizeDelta = new Vector2((width - 2 * margin) * (1 - sliderPositionEnd) * 100.0f, (height - 2.0f * margin) * 100.0f);
                    rectTextValue.localPosition = new Vector3(textPosRight, -margin, -0.002f);
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
                sharedMaterialInstance.name = "UISlider_Material_Instance";
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
                sharedMaterialInstance.name = "UISliderRail_Instance";
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
                sharedMaterialInstance.name = "UISliderKnob_Material_Instance";
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
                catch(Exception e)
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
            float widthWithoutMargins = width - 2.0f * margin;

            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));
            Vector3 posTopSliderBegin = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionBegin, -margin, -0.001f));
            Vector3 posTopSliderEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionEnd, -margin, -0.001f));
            Vector3 posBottomSliderBegin = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionBegin, -height + margin, -0.001f));
            Vector3 posBottomSliderEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionEnd, -height + margin, -0.001f));

            Vector3 eps = new Vector3(0.001f, 0, 0);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopSliderBegin);
            Gizmos.DrawLine(posTopSliderBegin, posBottomSliderBegin);
            Gizmos.DrawLine(posBottomSliderBegin, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(posTopSliderBegin + eps, posTopSliderEnd);
            Gizmos.DrawLine(posTopSliderEnd, posBottomSliderEnd);
            Gizmos.DrawLine(posBottomSliderEnd, posBottomSliderBegin + eps);
            Gizmos.DrawLine(posBottomSliderBegin + eps, posTopSliderBegin + eps);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(posTopSliderEnd + eps, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomSliderEnd + eps);
            Gizmos.DrawLine(posBottomSliderEnd + eps, posTopSliderEnd + eps);

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
                    txt.text = currentValue.ToString("#0.00");
                }
            }
        }

        private void UpdateSliderPosition()
        {
            float pct = HasCurveData() ? invDataCurve.Evaluate(currentValue) 
                : (currentValue - minValue) / (maxValue - minValue);

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;
            float posX = startX + pct * (endX - startX);

            Vector3 knobPosition = new Vector3(posX - knobRadius, knobRadius - (height / 2.0f), -knobDepth);

            knob.transform.localPosition = knobPosition;
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

        private float GetValue()
        {
            return currentValue;
        }

        private void SetValue(float floatValue)
        {
            currentValue = floatValue;
        }

        // --- RAY API ----------------------------------------------------

        public override void OnRayEnter()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayEnterClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayHover()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayHoverClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayExit()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayExitClicked()
        {
            if (IgnoreRayInteraction())
                return;

            // exiting while clicking shows a pushed slider, because we are acting on it, not like a button.
            Hovered = true;
            Pushed = true;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayClick()
        {
            if (IgnoreRayInteraction())
                return;

            onClickEvent.Invoke();

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayReleaseInside()
        {
            if (IgnoreRayInteraction())
                return;

            onReleaseEvent.Invoke();

            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayReleaseOutside()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            ResetColor();
        }

        public override bool OverridesRayEndPoint() { return true; }
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint) 
        {
            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.rightController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            float enter;
            widgetPlane.Raycast(ray, out enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            if (IgnoreRayInteraction())
            {
                // return endPoint at the surface of the widget.
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;

            // TODO: use curve, invert curve.
            float currentValuePct = (Value - minValue) / (maxValue - minValue);
            float currentKnobPositionX = startX + currentValuePct * (endX - startX);

            // DRAG

            if (!triggerJustClicked) // if trigger just clicked, use the actual projection, no interpolation.
            {
                localProjectedWidgetPosition.x = Mathf.Lerp(currentKnobPositionX, localProjectedWidgetPosition.x, GlobalState.Settings.RaySliderDrag);
            }

            // CLAMP

            if (localProjectedWidgetPosition.x < startX)
                localProjectedWidgetPosition.x = startX;

            if (localProjectedWidgetPosition.x > endX)
                localProjectedWidgetPosition.x = endX;

            localProjectedWidgetPosition.y = -height / 2.0f;

            // SET

            float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);
            // TODO: put the "curve" code here
            /*
             if (HasCurveData())
                    {
                        Value = dataCurve.Evaluate(pct);
                    }
                    else // linear
                    {
                        Value = minValue + pct * (maxValue - minValue); // will replace the slider cursor.
                    }
            */
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
            public string widgetName = UISlider.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UISlider.default_thickness);
            public float width = UISlider.default_width;
            public float height = UISlider.default_height;
            public float margin = UISlider.default_margin;
            public float thickness = UISlider.default_thickness;

            public float sliderBegin = UISlider.default_slider_begin;
            public float sliderEnd = UISlider.default_slider_end;
            public float railMargin = UISlider.default_rail_margin;
            public float railThickness = UISlider.default_rail_thickness;
            public float knobRadius = UISlider.default_knob_radius;
            public float knobDepth = UISlider.default_knob_depth;
            public SliderDataSource dataSource = UISlider.default_data_source;
            public float minValue = UISlider.default_min_value;
            public float maxValue = UISlider.default_max_value;
            public float currentValue = UISlider.default_current_value;

            public Material material = UIUtils.LoadMaterial(UISlider.default_material_name);
            public Material railMaterial = UIUtils.LoadMaterial(UISlider.default_rail_material_name);
            public Material knobMaterial = UIUtils.LoadMaterial(UISlider.default_knob_material_name);

            public ColorVar color = UIOptions.BackgroundColorVar;
            public ColorVar textColor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public ColorVar railColor = UIOptions.SliderRailColorVar; // UISlider.default_rail_color;
            public ColorVar knobColor = UIOptions.SliderKnobColorVar; // UISlider.default_knob_color;

            public string caption = UISlider.default_text;
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

            UISlider uiSlider = go.AddComponent<UISlider>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
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

            float railWidth = (input.width - 2 * input.margin) * (input.sliderEnd - input.sliderBegin);
            float railHeight = 3 * uiSlider.railMargin; // TODO: see if we can tie this to another variable, like height.
            float railThickness = uiSlider.railThickness;
            float railMargin = uiSlider.railMargin;
            Vector3 railPosition = new Vector3(input.margin + (input.width - 2 * input.margin) * input.sliderBegin, -input.height / 2, -railThickness); // put z = 0 back

            uiSlider.rail = UISliderRail.Create(
                new UISliderRail.CreateArgs
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

            // KNOB
            float newKnobRadius = uiSlider.knobRadius;
            float newKnobDepth = uiSlider.knobDepth;

            float pct = (uiSlider.currentValue - uiSlider.minValue) / (uiSlider.maxValue - uiSlider.minValue);
  
            float widthWithoutMargins = input.width - 2.0f * input.margin;
            float startX = input.margin + widthWithoutMargins * uiSlider.sliderPositionBegin + railMargin;
            float endX = input.margin + widthWithoutMargins * uiSlider.sliderPositionEnd - railMargin;
            float posX = startX + pct * (endX - startX);

            Vector3 knobPosition = new Vector3(posX - uiSlider.knobRadius, uiSlider.knobRadius - (uiSlider.height / 2.0f), -uiSlider.knobDepth);

            uiSlider.knob = UISliderKnob.Create(
                new UISliderKnob.CreateArgs
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
            // CANVAS (to hold the 2 texts)
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
            rt.localPosition = Vector3.zero;

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            // Add a Text under the Canvas
            if (input.caption.Length > 0)
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
                trt.sizeDelta = new Vector2((uiSlider.width-2*uiSlider.margin) * uiSlider.sliderPositionBegin * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                float textPosLeft = uiSlider.margin;
                trt.localPosition = new Vector3(textPosLeft, -uiSlider.margin, -0.002f);
            }

            // Text VALUE
            //if (caption.Length > 0)
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
                trt.sizeDelta = new Vector2((uiSlider.width - 2 * uiSlider.margin) * (1-uiSlider.sliderPositionEnd) * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                float textPosRight = uiSlider.width - uiSlider.margin;
                trt.localPosition = new Vector3(textPosRight, -uiSlider.margin, -0.002f);
            }
        }
    }
}
