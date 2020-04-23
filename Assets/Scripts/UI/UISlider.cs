using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine.UI;
using System;
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
        [SpaceHeader("Slider Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = 0.005f;
        [CentimeterFloat] public float thickness = 0.001f;
        public float sliderPositionBegin = 0.3f;
        public float sliderPositionEnd = 0.8f;
        public Color pushedColor = new Color(0f, 0.6549f, 1f);

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Slider SubComponents Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float railMargin = 0.004f;
        [CentimeterFloat] public float railThickness = 0.001f;

        [CentimeterFloat] public float knobRadius = 0.01f;
        [CentimeterFloat] public float knobDepth = 0.005f;

        [SpaceHeader("Slider Values", 6, 0.8f, 0.8f, 0.8f)]
        public float minValue = 0.0f;
        public float maxValue = 1.0f;
        public float currentValue = 0.5f;

        // TODO: precision, step?

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public FloatChangedEvent onSlideEvent = new FloatChangedEvent();
        public IntChangedEvent onSlideEventInt = new IntChangedEvent();
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        [SerializeField] private UISliderRail rail = null;
        [SerializeField] private UISliderKnob knob = null;

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
                Transform textValueTransform = canvas.transform.Find("TextValue");

                RectTransform rectText = textTransform.GetComponent<RectTransform>();
                RectTransform rectTextValue = textValueTransform.GetComponent<RectTransform>();

                rectText.sizeDelta = new Vector2((width - 2 * margin) * sliderPositionBegin, height);
                rectText.localPosition = new Vector3(textPosLeft, -height / 2.0f, -0.002f);

                rectTextValue.sizeDelta = new Vector2((width - 2 * margin) * (1 - sliderPositionEnd), height);
                rectTextValue.localPosition = new Vector3(textPosRight, -height / 2.0f, -0.002f);
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

                Material material = UIUtils.LoadMaterial("UISliderRail");
                Material materialInstance = Instantiate(material);

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

                Material material = UIUtils.LoadMaterial("UISliderKnob");
                Material materialInstance = Instantiate(material);

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

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;
            float posX = startX + pct * (endX - startX);

            Vector3 knobPosition = new Vector3(posX - knobRadius, knobRadius - (height / 2.0f), -knobDepth);

            knob.transform.localPosition = knobPosition;
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
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                // HIDE cursor

                onClickEvent.Invoke();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                // SHOW cursor

                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                // NOTE: The correct "currentValue" is already computed in the HandleCursorBehavior callback.
                //       Just call the listeners here.
                onSlideEvent.Invoke(currentValue);

                int intValue = Mathf.RoundToInt(currentValue);
                onSlideEventInt.Invoke(intValue);
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

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;

            float snapXDistance = 0.002f;
            // TODO: snap X if X if a bit left of startX or a bit right of endX.

            if (localProjectedWidgetPosition.x > startX - snapXDistance && localProjectedWidgetPosition.x < endX + snapXDistance)
            {
                // SNAP X left
                if (localProjectedWidgetPosition.x < startX)
                    localProjectedWidgetPosition.x = startX;

                // SNAP X right
                if(localProjectedWidgetPosition.x > endX)
                    localProjectedWidgetPosition.x = endX;

                // SNAP Y to middle
                localProjectedWidgetPosition.y = -height / 2.0f;

                float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);

                // Actually move the slider ONLY if RIGHT_TRIGGER is pressed.
                bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
                if (triggerState)
                {
                    Value = minValue + pct * (maxValue - minValue); // will replace the slider cursor.
                }

                // Haptic intensity as we go deeper into the widget.
                float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                intensity *= intensity; // ease-in

                VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;
        }

        public static void CreateUISlider(
            string sliderName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            float thickness,
            float slider_begin,
            float slider_end,
            float rail_margin,
            float rail_thickness,
            float knob_radius,
            float knob_depth,
            float min_slider_value,
            float max_slider_value,
            float cur_slider_value,
            Material background_material,
            Material rail_material,
            Material knob_material,
            Color background_color,
            Color rail_color,
            Color knob_color,
            string caption)
        {
            GameObject go = new GameObject(sliderName);
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

            UISlider uiSlider = go.AddComponent<UISlider>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiSlider.relativeLocation = relativeLocation;
            uiSlider.transform.parent = parent;
            uiSlider.transform.localPosition = parentAnchor + relativeLocation;
            uiSlider.transform.localRotation = Quaternion.identity;
            uiSlider.transform.localScale = Vector3.one;
            uiSlider.width = width;
            uiSlider.height = height;
            uiSlider.margin = margin;
            uiSlider.thickness = thickness;
            uiSlider.sliderPositionBegin = slider_begin;
            uiSlider.sliderPositionEnd = slider_end;
            uiSlider.railMargin = rail_margin;
            uiSlider.railThickness = rail_thickness;
            uiSlider.knobRadius = knob_radius;
            uiSlider.knobDepth = knob_depth;
            uiSlider.minValue = min_slider_value;
            uiSlider.maxValue = max_slider_value;
            uiSlider.currentValue = cur_slider_value;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
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
            if (meshRenderer != null && background_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(background_material);
                uiSlider.BaseColor = background_color;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            //
            // RAIL
            //

            float railWidth = (width - 2 * margin) * (slider_end - slider_begin);
            float railHeight = 3 * uiSlider.railMargin; // TODO: see if we can tie this to another variable, like height.
            float railThickness = uiSlider.railThickness;
            float railMargin = uiSlider.railMargin;
            Vector3 railPosition = new Vector3(margin + (width - 2 * margin) * slider_begin, -height / 2, -railThickness); // put z = 0 back

            uiSlider.rail = UISliderRail.CreateUISliderRail("Rail", go.transform, railPosition, railWidth, railHeight, railThickness, railMargin, rail_material, rail_color);


            // KNOB
            Vector3 knobPosition = new Vector3(0, 0, 0);
            float newKnobRadius = uiSlider.knobRadius;
            float newKnobDepth = uiSlider.knobDepth;

            uiSlider.knob = UISliderKnob.CreateUISliderKnob("Knob", go.transform, knobPosition, newKnobRadius, newKnobDepth, knob_material, knob_color);

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
            if (caption.Length > 0)
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                //t.font = (Font)Resources.Load("MyLocalFont");
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
                trt.sizeDelta = new Vector2((uiSlider.width-2*uiSlider.margin) * uiSlider.sliderPositionBegin, uiSlider.height);
                float textPosLeft = uiSlider.margin;
                trt.localPosition = new Vector3(textPosLeft, -uiSlider.height / 2.0f, -0.002f);
            }

            // Text VALUE
            //if (caption.Length > 0)
            {
                GameObject text = new GameObject("TextValue");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                //t.font = (Font)Resources.Load("MyLocalFont");
                t.text = cur_slider_value.ToString("#0.00");
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
                trt.sizeDelta = new Vector2((uiSlider.width - 2 * uiSlider.margin) * (1-uiSlider.sliderPositionEnd), uiSlider.height);
                float textPosRight = uiSlider.width - uiSlider.margin;
                trt.localPosition = new Vector3(textPosRight, -uiSlider.height / 2.0f, -0.002f);
            }
        }
    }
}
