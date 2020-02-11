using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine.UI;
using System;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UITimeBar : UIElement
    {
        [SpaceHeader("TimeBar Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = 0.005f;
        [CentimeterFloat] public float thickness = 0.001f;
        public float sliderPositionBegin = 0.3f;
        public float sliderPositionEnd = 0.8f;
        public Color pushedColor = new Color(0.3f, 0.3f, 0.3f);

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("TimeBar SubComponents Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float railMargin = 0.005f;
        [CentimeterFloat] public float railThickness = 0.001f;
        [CentimeterFloat] public float knobHeadWidth = 0.002f;
        [CentimeterFloat] public float knobHeadHeight = 0.013f;
        [CentimeterFloat] public float knobHeadDepth = 0.003f;
        [CentimeterFloat] public float knobFootWidth = 0.001f;
        [CentimeterFloat] public float knobFootHeight = 0.005f;
        [CentimeterFloat] public float knobFootDepth = 0.001f; // == railThickness
        // TODO: add colors here?

        [SpaceHeader("TimeBar Values", 6, 0.8f, 0.8f, 0.8f)]
        public int minValue = 0;
        public int maxValue = 250;
        public int currentValue = 0;
        

        // TODO: type? handle int and float.
        //       precision, step?

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public IntChangedEvent onSlideEvent = new IntChangedEvent(); // TODO: maybe make 2 callbacks, one for floats, one for ints
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        [SerializeField] private UITimeBarRail rail = null;
        [SerializeField] private UITimeBarKnob knob = null;

        private bool needRebuild = false;

        public float TimeBarPositionBegin { get { return sliderPositionBegin; } set { sliderPositionBegin = value; RebuildMesh(); } }
        public float TimeBarPositionEnd { get { return sliderPositionEnd; } set { sliderPositionEnd = value; RebuildMesh(); } }
        public string Text { get { return GetText(); } set { SetText(value); } }
        public int Value { get { return GetValue(); } set { SetValue(value); UpdateValueText(); UpdateTimeBarPosition(); } }

        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                onSlideEvent.AddListener(OnSlide);
                onClickEvent.AddListener(OnClickTimeBar);
                onReleaseEvent.AddListener(OnReleaseTimeBar);
            }
        }

        public override void RebuildMesh()
        {
            // RAIL
            Vector3 railPosition = new Vector3(margin + (width - 2 * margin) * sliderPositionBegin, -height / 2, -0.0f); // put z = 0 back
            float railWidth = (width - 2 * margin) * (sliderPositionEnd - sliderPositionBegin);
            float railHeight = 3 * railMargin;
            
            rail.RebuildMesh(railWidth, railHeight, railThickness, railMargin);
            rail.transform.localPosition = railPosition;

            // KNOB
            float newKnobHeadWidth = knobHeadWidth;
            float newKnobHeadHeight = knobHeadHeight;
            float newKnobHeadDepth = knobHeadDepth;
            float newKnobFootWidth = knobFootWidth;
            float newKnobFootHeight = knobFootHeight;
            float newKnobFootDepth = knobFootDepth;

            knob.RebuildMesh(newKnobHeadWidth, newKnobHeadHeight, newKnobHeadDepth, newKnobFootWidth, newKnobFootHeight, newKnobFootDepth);
            
            // BASE
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UITimeBar_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
            UpdateTimeBarPosition();
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
                    UpdateTimeBarPosition();
                    SetColor(baseColor);
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
            Vector3 posTopTimeBarBegin = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionBegin, -margin, -0.001f));
            Vector3 posTopTimeBarEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionEnd, -margin, -0.001f));
            Vector3 posBottomTimeBarBegin = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionBegin, -height + margin, -0.001f));
            Vector3 posBottomTimeBarEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionEnd, -height + margin, -0.001f));

            Vector3 eps = new Vector3(0.001f, 0, 0);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopTimeBarBegin);
            Gizmos.DrawLine(posTopTimeBarBegin, posBottomTimeBarBegin);
            Gizmos.DrawLine(posBottomTimeBarBegin, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(posTopTimeBarBegin + eps, posTopTimeBarEnd);
            Gizmos.DrawLine(posTopTimeBarEnd, posBottomTimeBarEnd);
            Gizmos.DrawLine(posBottomTimeBarEnd, posBottomTimeBarBegin + eps);
            Gizmos.DrawLine(posBottomTimeBarBegin + eps, posTopTimeBarBegin + eps);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(posTopTimeBarEnd + eps, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomTimeBarEnd + eps);
            Gizmos.DrawLine(posBottomTimeBarEnd + eps, posTopTimeBarEnd + eps);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        private void UpdateValueText()
        {
            Canvas canvas = knob.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                Transform textValueTransform = canvas.transform.Find("Text");
                Text txt = textValueTransform.gameObject.GetComponent<Text>();
                if (txt != null)
                {
                    txt.text = ((int)currentValue).ToString();
                }
            }
        }

        private void UpdateTimeBarPosition()
        {
            float pct = currentValue / (maxValue - minValue);

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;
            float posX = startX + pct * (endX - startX);

            Vector3 knobPosition = new Vector3(posX, -height / 2.0f, 0.0f);

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

        private int GetValue()
        {
            return currentValue;
        }

        private void SetValue(int intValue)
        {
            currentValue = intValue;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                // HIDE cursor

                onClickEvent.Invoke();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                // SHOW cursor

                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                // NOTE: The correct "currentValue" is already computed in the HandleCursorBehavior callback.
                //       Just call the listeners here.
                onSlideEvent.Invoke(currentValue);
            }
        }

        public void OnClickTimeBar()
        {
            SetColor(pushedColor);
        }

        public void OnReleaseTimeBar()
        {
            SetColor(baseColor);
        }

        public void OnSlide(int f)
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

                // TODO: snap X to INT positions

                float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);

                Value = minValue + (int)pct * (maxValue - minValue); // will replace the slider cursor.

                // Haptic intensity as we go deeper into the widget.
                float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                intensity *= intensity; // ease-in

                // TODO : Re-enable
                VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;
        }

        public static void CreateUITimeBar(
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
            int min_slider_value,
            int max_slider_value,
            int cur_slider_value,
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

            UITimeBar uiTimeBar = go.AddComponent<UITimeBar>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiTimeBar.relativeLocation = relativeLocation;
            uiTimeBar.transform.parent = parent;
            uiTimeBar.transform.localPosition = parentAnchor + relativeLocation;
            uiTimeBar.transform.localRotation = Quaternion.identity;
            uiTimeBar.transform.localScale = Vector3.one;
            uiTimeBar.width = width;
            uiTimeBar.height = height;
            uiTimeBar.margin = margin;
            uiTimeBar.thickness = thickness;
            uiTimeBar.sliderPositionBegin = slider_begin;
            uiTimeBar.sliderPositionEnd = slider_end;
            uiTimeBar.minValue = min_slider_value;
            uiTimeBar.maxValue = max_slider_value;
            uiTimeBar.currentValue = cur_slider_value;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // TODO: new mesh, with time ticks texture
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
                uiTimeBar.Anchor = Vector3.zero;
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
                uiTimeBar.BaseColor = background_color;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 4; // "LightLayer 3"
            }

            //
            // RAIL
            // TODO: no more rail, or let the rail be the textured bar with time "ticks"

            Vector3 railPosition = new Vector3(margin + (width - 2 * margin) * slider_begin, -height / 2, -0.0f); // put z = 0 back
            float railWidth = (width - 2 * margin) * (slider_end - slider_begin);
            float railHeight = 3 * uiTimeBar.railMargin; // TODO: see if we can tie this to another variable, like height.
            float railThickness = uiTimeBar.railThickness;
            float railMargin = uiTimeBar.railMargin;

            uiTimeBar.rail = UITimeBarRail.CreateUITimeBarRail("Rail", go.transform, railPosition, railWidth, railHeight, railThickness, railMargin, rail_material, rail_color);


            // KNOB
            Vector3 knobPosition = new Vector3(0, 0, 0);
            float newKnobHeadWidth = uiTimeBar.knobHeadWidth;
            float newKnobHeadHeight = uiTimeBar.knobHeadHeight;
            float newKnobHeadDepth = uiTimeBar.knobHeadDepth;
            float newKnobFootWidth = uiTimeBar.knobFootWidth;
            float newKnobFootHeight = uiTimeBar.knobFootHeight;
            float newKnobFootDepth = uiTimeBar.knobFootDepth;

            uiTimeBar.knob = UITimeBarKnob.CreateUITimeBarKnob("Knob", go.transform, knobPosition, 
                newKnobHeadWidth, newKnobHeadHeight, newKnobHeadDepth, newKnobFootWidth, newKnobFootHeight, newKnobFootDepth, 
                knob_material, knob_color);

            //
            // CANVAS (to hold the 2 texts)
            //

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiTimeBar.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiTimeBar.width, uiTimeBar.height);
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
                trt.sizeDelta = new Vector2((uiTimeBar.width-2*uiTimeBar.margin) * uiTimeBar.sliderPositionBegin, uiTimeBar.height);
                float textPosLeft = uiTimeBar.margin;
                trt.localPosition = new Vector3(textPosLeft, -uiTimeBar.height / 2.0f, -0.002f);
            }

            // Text VALUE
            //if (caption.Length > 0)
            {
                GameObject text = new GameObject("TextValue");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                //t.font = (Font)Resources.Load("MyLocalFont");
                t.text = cur_slider_value.ToString("#.00");
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
                trt.sizeDelta = new Vector2((uiTimeBar.width - 2 * uiTimeBar.margin) * (1-uiTimeBar.sliderPositionEnd), uiTimeBar.height);
                float textPosRight = uiTimeBar.width - uiTimeBar.margin;
                trt.localPosition = new Vector3(textPosRight, -uiTimeBar.height / 2.0f, -0.002f);
            }
        }
    }
}
