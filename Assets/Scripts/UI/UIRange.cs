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
    public class UIRange : UIElement
    {
        public enum RangeContent { All, LabelOnly, MinMaxOnly, BarOnly };
        public enum RangeValueType { Float, Int };
        public enum RangeWidgetPart { Background, NameLabel, GlobalMinLabel, Rail, LeftKnob, MiddleKnob, RightKnob, GlobalMaxLabel };

        public static readonly string default_widget_name = "New Range";
        public static readonly float default_width = 0.3f;
        public static readonly float default_height = 0.03f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly float default_label_end = 0.2f;
        public static readonly float default_slider_begin = 0.25f;
        public static readonly float default_slider_end = 0.9f;
        public static readonly float default_rail_margin = 0.01f;
        public static readonly float default_rail_thickness = 0.001f;
        public static readonly float default_knob_radius = 0.01f;
        public static readonly float default_knob_depth = 0.005f;
        public static readonly float default_min_value = 0.0f;
        public static readonly float default_max_value = 250.0f;
        public static readonly float default_current_min_value = 102.2f;
        public static readonly float default_current_max_value = 153.7f;
        public static readonly string default_material_name = "UIBase";
        public static readonly string default_rail_material_name = "UIRangeRail";
        public static readonly string default_knob_center_material_name = "UIRangeKnobCenter";
        public static readonly string default_knob_end_material_name = "UIRangeKnobEnd";
        public static readonly string default_label = "Range";
        public static readonly UIRangeKnob.TextBehavior default_text_behavior = UIRangeKnob.TextBehavior.Center;
        public static readonly RangeContent default_content = RangeContent.All;
        public static readonly UIRange.RangeValueType default_value_type = UIRange.RangeValueType.Int;

        [SpaceHeader("Range Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public float labelPositionEnd = default_slider_begin;
        public float sliderPositionBegin = default_slider_begin;
        public float sliderPositionEnd = default_slider_end;
        public Material sourceMaterial = null;
        public Material sourceRailMaterial = null;
        public Material sourceKnobCenterMaterial = null;
        public Material sourceKnobEndMaterial = null;
        [TextArea] public string labelContent = "";

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Range SubComponents Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float railMargin = default_rail_margin;
        [CentimeterFloat] public float railThickness = default_rail_thickness;

        [CentimeterFloat] public float knobRadius = default_knob_radius;
        [CentimeterFloat] public float knobDepth = default_knob_depth;

        public int knobNbSubdivCornerFixed = 6;
        public int knobNbSubdivCornerPerUnit = 3;

        [SpaceHeader("Range Values", 6, 0.8f, 0.8f, 0.8f)]
        public UIRangeKnob.TextBehavior textBehavior = default_text_behavior;
        public RangeContent content = default_content;
        public RangeValueType valueType = default_value_type;
        public Vector2 currentRange = new Vector2(12, 15);
        
        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public RangeChangedEventFloat onSlideEvent = new RangeChangedEventFloat();
        public RangeChangedEventInt onSlideEventInt = new RangeChangedEventInt();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        public UIRangeRail rail = null;
        public UIRangeKnob midKnob = null;
        public UIRangeKnob minKnob = null;
        public UIRangeKnob maxKnob = null;

        public float LabelPositionEnd { get { return labelPositionEnd; } set { labelPositionEnd = value; RebuildMesh(); } }
        public float RangePositionBegin { get { return sliderPositionBegin; } set { sliderPositionBegin = value; RebuildMesh(); } }
        public float RangePositionEnd { get { return sliderPositionEnd; } set { sliderPositionEnd = value; RebuildMesh(); } }
        public string Label { get { return labelContent; } set { SetLabel(value); } }
        public Vector2 CurrentRange { get { return currentRange; } set { SetCurrentRange(value); RebuildMesh(); UpdateValueText(); } }

        private bool keyboardOpen = false;
        private RangeWidgetPart keyboardSourcePart = RangeWidgetPart.Background;
        private RangeWidgetPart grippedPart = RangeWidgetPart.Background;

        public void UpdateGlobalRange()
        {
            Vector2 range = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            SetGlobalRange(range); 
            RebuildMesh();
            UpdateValueText();
        }

        public override void RebuildMesh()
        {
            // RAIL
            Vector3 railPosition = new Vector3(margin + (width - 2 * margin) * sliderPositionBegin, railMargin - height / 2, -railThickness);
            float railWidth = (width - 2 * margin) * (sliderPositionEnd - sliderPositionBegin);
            float railHeight = 2 * railMargin; // no inner rectangle, only margin driven rounded borders.

            rail.RebuildMesh(railWidth, railHeight, railThickness, railMargin);
            rail.transform.localPosition = railPosition;

            // KNOB(s)
            float newKnobRadius = knobRadius;
            float newKnobDepth = knobDepth;

            Vector2 globalRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            float pctBegin = (currentRange.x - globalRange.x) / (globalRange.y - globalRange.x);
            float pctEnd = (currentRange.y - globalRange.x) / (globalRange.y - globalRange.x);

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;
            float posX = startX + pctBegin * (endX - startX);
            float posXE = startX + pctEnd * (endX - startX);
            float newKnobWidth = posXE - posX + 2.0f * knobRadius;

            float smallRadius = newKnobRadius * 0.9f;// .8f; // smaller
            float tallDepth = newKnobDepth * 1.2f;

            minKnob.RebuildMesh(2.0f * smallRadius, smallRadius, tallDepth, knobNbSubdivCornerFixed, knobNbSubdivCornerPerUnit);
            maxKnob.RebuildMesh(2.0f * smallRadius, smallRadius, tallDepth, knobNbSubdivCornerFixed, knobNbSubdivCornerPerUnit);
            midKnob.RebuildMesh(newKnobWidth, newKnobRadius, newKnobDepth, knobNbSubdivCornerFixed, knobNbSubdivCornerPerUnit);

            // BASE
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UIRange_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
            UpdateRangePosition();
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
            Canvas canvas = transform.Find("Canvas").GetComponent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(width, height);

                float textPosRight = width - margin;
                float textPosLeft = margin;
                float textPosMiddle = margin + (width - 2 * margin) * labelPositionEnd;

                Transform textTransform = canvas.transform.Find("Text");
                TextMeshProUGUI text = textTransform.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = labelContent;
                    text.color = TextColor;
                    if (content == RangeContent.All || content == RangeContent.LabelOnly)
                    {
                        text.gameObject.SetActive(true);
                        RectTransform rectText = textTransform.GetComponent<RectTransform>();
                        if (content == RangeContent.LabelOnly)
                        {
                            rectText.sizeDelta = new Vector2((width - 2 * margin) * sliderPositionBegin * 100.0f, (height - 2.0f * margin) * 100.0f);
                            rectText.localPosition = new Vector3(textPosLeft, -margin, -0.002f);
                        }
                        else
                        {
                            rectText.sizeDelta = new Vector2((width - 2 * margin) * labelPositionEnd * 100.0f, (height - 2.0f * margin) * 100.0f);
                            rectText.localPosition = new Vector3(textPosLeft, -margin, -0.002f);
                        }
                    }
                    else
                    {
                        text.gameObject.SetActive(false);
                    }
                }

                Transform minTextValueTransform = canvas.transform.Find("MinTextValue");
                TextMeshProUGUI minTextValue = minTextValueTransform.GetComponent<TextMeshProUGUI>();
                if (minTextValue != null)
                {
                    minTextValue.color = TextColor;
                    if (content == RangeContent.All || content == RangeContent.MinMaxOnly)
                    {
                        minTextValue.gameObject.SetActive(true);
                        RectTransform rectTextValue = minTextValueTransform.GetComponent<RectTransform>();
                        if (content == RangeContent.MinMaxOnly)
                        {
                            rectTextValue.sizeDelta = new Vector2((width - 2 * margin) * (sliderPositionBegin) * 100.0f, (height - 2.0f * margin) * 100.0f);
                            rectTextValue.localPosition = new Vector3(textPosLeft, -margin, -0.002f);
                        }
                        else
                        {
                            rectTextValue.sizeDelta = new Vector2((width - 2 * margin) * (sliderPositionBegin - labelPositionEnd) * 100.0f, (height - 2.0f * margin) * 100.0f);
                            rectTextValue.localPosition = new Vector3(textPosMiddle, -margin, -0.002f);
                        }
                    }
                    else
                    {
                        minTextValue.gameObject.SetActive(false);
                    }
                }

                Transform maxTextValueTransform = canvas.transform.Find("MaxTextValue");
                TextMeshProUGUI maxTextValue = maxTextValueTransform.GetComponent<TextMeshProUGUI>();
                if (maxTextValue != null)
                {
                    maxTextValue.color = TextColor;
                    if (content == RangeContent.All || content == RangeContent.MinMaxOnly)
                    {
                        maxTextValue.gameObject.SetActive(true);
                        RectTransform rectTextValue = maxTextValueTransform.GetComponent<RectTransform>();
                        rectTextValue.sizeDelta = new Vector2((width - 2 * margin) * (1 - sliderPositionEnd) * 100.0f, (height - 2.0f * margin) * 100.0f);
                        rectTextValue.localPosition = new Vector3(textPosRight, -margin, -0.002f);
                    }
                    else
                    {
                        maxTextValue.gameObject.SetActive(false);
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
                sharedMaterialInstance.name = "UIRange_Material_Instance";
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
                sharedMaterialInstance.name = "UIRangeRail_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }

            meshRenderer = minKnob.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = minKnob.BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material materialInstance = Instantiate(sourceKnobEndMaterial);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIRangeKnob_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }

            meshRenderer = maxKnob.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = maxKnob.BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material materialInstance = Instantiate(sourceKnobEndMaterial);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIRangeKnob_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }

            meshRenderer = midKnob.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = midKnob.BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material materialInstance = Instantiate(sourceKnobCenterMaterial);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIRangeKnob_Material_Instance";
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
            //if (currentValue < minValue)
            //    currentValue = minValue;
            //if (currentValue > maxValue)
            //    currentValue = maxValue;

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
                    UpdateRangePosition();
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
            minKnob.ResetColor();
            maxKnob.ResetColor();
            midKnob.ResetColor();

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

        private void OnDrawGizmosSelected()
        {
            float widthWithoutMargins = width - 2.0f * margin;

            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));
            Vector3 posTopLabelEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * labelPositionEnd, -margin, -0.001f));
            Vector3 posBottomLabelEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * labelPositionEnd, -height + margin, -0.001f));
            Vector3 posTopRangeBegin = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionBegin, -margin, -0.001f));
            Vector3 posTopRangeEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionEnd, -margin, -0.001f));
            Vector3 posBottomRangeBegin = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionBegin, -height + margin, -0.001f));
            Vector3 posBottomRangeEnd = transform.TransformPoint(new Vector3(margin + widthWithoutMargins * sliderPositionEnd, -height + margin, -0.001f));

            Vector3 eps = new Vector3(0.001f, 0, 0);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopLabelEnd);
            Gizmos.DrawLine(posTopLabelEnd, posBottomLabelEnd);
            Gizmos.DrawLine(posBottomLabelEnd, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(posTopLabelEnd + eps, posTopRangeBegin);
            Gizmos.DrawLine(posTopRangeBegin, posBottomRangeBegin);
            Gizmos.DrawLine(posBottomRangeBegin, posBottomLabelEnd + eps);
            Gizmos.DrawLine(posBottomLabelEnd + eps, posTopLabelEnd + eps);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(posTopRangeBegin + eps, posTopRangeEnd);
            Gizmos.DrawLine(posTopRangeEnd, posBottomRangeEnd);
            Gizmos.DrawLine(posBottomRangeEnd, posBottomRangeBegin + eps);
            Gizmos.DrawLine(posBottomRangeBegin + eps, posTopRangeBegin + eps);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(posTopRangeEnd + eps, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomRangeEnd + eps);
            Gizmos.DrawLine(posBottomRangeEnd + eps, posTopRangeEnd + eps);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        private void UpdateValueText()
        {
            Canvas canvas = transform.Find("Canvas").GetComponent<Canvas>();
            if (canvas != null)
            {
                Transform minTextValueTransform = canvas.transform.Find("MinTextValue");
                TextMeshProUGUI minTxt = minTextValueTransform.gameObject.GetComponent<TextMeshProUGUI>();
                if (minTxt != null)
                {
                    minTxt.text = valueType == RangeValueType.Float
                        ? GlobalState.Animation.StartFrame.ToString("#0.00")
                        : Mathf.RoundToInt(GlobalState.Animation.StartFrame).ToString();
                }

                Transform maxTextValueTransform = canvas.transform.Find("MaxTextValue");
                TextMeshProUGUI maxTxt = maxTextValueTransform.gameObject.GetComponent<TextMeshProUGUI>();
                if (maxTxt != null)
                {
                    maxTxt.text = valueType == RangeValueType.Float
                        ? GlobalState.Animation.EndFrame.ToString("#0.00")
                        : Mathf.RoundToInt(GlobalState.Animation.EndFrame).ToString();
                }
            }

            minKnob.UpdateText(currentRange.x, textBehavior);
            maxKnob.UpdateText(currentRange.y, textBehavior);
        }

        private void UpdateRangePosition()
        {
            Vector2 globalRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            float pctBegin = (currentRange.x - globalRange.x) / (globalRange.y - globalRange.x);
            float pctEnd = (currentRange.y - globalRange.x) / (globalRange.y - globalRange.x);

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;
            float posX = startX + pctBegin * (endX - startX);
            float posXE = startX + pctEnd * (endX - startX);

            float smallRadius = knobRadius * 0.9f;// .8f; // smaller
            float bigDepth = knobDepth * 1.2f;

            minKnob.transform.localPosition = new Vector3(posX - smallRadius, smallRadius - (height / 2.0f), -bigDepth);
            maxKnob.transform.localPosition = new Vector3(posXE- smallRadius, smallRadius - (height / 2.0f), -bigDepth);
            midKnob.transform.localPosition = new Vector3(posX - knobRadius, knobRadius - (height / 2.0f), -knobDepth);
        }

        private void SetLabel(string textValue)
        {
            labelContent = textValue;

            Transform t = transform.Find("Canvas/Text");
            TextMeshProUGUI text = t.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = textValue;
            }
        }

        public override void SetLightLayer(int layerIndex)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.renderingLayerMask = (1u << layerIndex);
            }

            // Rail, Knob, Text and TextValue
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer r in renderers)
            {
                r.renderingLayerMask = (1u << layerIndex);
            }
        }

        private void SetGlobalRange(Vector2 globalRange)
        {
            if (globalRange.x < globalRange.y)
            {
                Vector2 newCurrentRange = new Vector2(
                    Mathf.Max(globalRange.x, currentRange.x),
                    Mathf.Min(globalRange.y, currentRange.y));

                if (currentRange != newCurrentRange)
                {
                    currentRange = newCurrentRange;

                    onSlideEvent.Invoke(CurrentRange);
                    Vector2Int intRange = new Vector2Int(Mathf.RoundToInt(currentRange.x), Mathf.RoundToInt(currentRange.y));
                    onSlideEventInt.Invoke(intRange);
                }
            }
            else
            {
                Debug.LogError($"Incoherent GlobalRange set {globalRange.x} {globalRange.y}");
            }
        }

        private void SetCurrentRange(Vector2 value)
        {
            if (value.y - value.x >= 1)
            {
                Vector2 globalRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                currentRange.x = Mathf.Clamp(value.x, globalRange.x, globalRange.y);
                currentRange.y = Mathf.Clamp(value.y, globalRange.x, globalRange.y);
            }
            else
            {
                Debug.LogError($"Trying to set an incoherent CurrentRange {value.x} {value.y}");
            }
        }

        public override bool IgnoreRayInteraction()
        {
            return base.IgnoreRayInteraction() || ToolsUIManager.Instance.numericKeyboardOpen;
        }

        private void OnValidateKeyboard(string value)
        {
            Label = value;
            // OnLabelNameChanged.Invoke();
        }

        private void OnValidateNumericKeyboard(float value)
        {
            switch (keyboardSourcePart)
            {
                case RangeWidgetPart.NameLabel:
                    {
                        break;
                    }
                case RangeWidgetPart.GlobalMaxLabel:
                    {
                        GlobalState.Animation.EndFrame = (int)value;
                        break;
                    }
                case RangeWidgetPart.GlobalMinLabel:
                    {
                        GlobalState.Animation.StartFrame = (int)value;
                        break;
                    }
                default: break;
            }
        }

        #region ray

        public override void OnRayEnter()
        {
            base.OnRayEnter();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);

            bool joyRightJustClicked = false;
            bool joyRightJustReleased = false;
            bool joyRightLongPush = false;
            VRInput.GetInstantJoyEvent(VRInput.rightController, VRInput.JoyDirection.RIGHT, ref joyRightJustClicked, ref joyRightJustReleased, ref joyRightLongPush);

            bool joyLeftJustClicked = false;
            bool joyLeftJustReleased = false;
            bool joyLeftLongPush = false;
            VRInput.GetInstantJoyEvent(VRInput.rightController, VRInput.JoyDirection.LEFT, ref joyLeftJustClicked, ref joyLeftJustReleased, ref joyLeftLongPush);

            Vector3 localProjectedWidgetPosition = GetLocalProjected(ray);
            RangeWidgetPart hoveredPart = GetRangeWidgetPart(localProjectedWidgetPosition);

            if (joyRightJustClicked || joyLeftJustClicked || joyRightLongPush || joyLeftLongPush)
            {
                switch (hoveredPart)
                {
                    case RangeWidgetPart.LeftKnob:
                        {
                            if (joyRightJustClicked || joyRightLongPush)
                            {                                
                                float newMin = Mathf.Clamp(currentRange.x + 1.0f, GlobalState.Animation.StartFrame, currentRange.y - 1.0f);
                                CurrentRange = new Vector2(newMin, currentRange.y);
                            }
                            else if (joyLeftJustClicked || joyLeftLongPush)
                            {
                                float newMin = Mathf.Clamp(currentRange.x - 1.0f, GlobalState.Animation.StartFrame, currentRange.y - 1.0f);
                                CurrentRange = new Vector2(newMin, currentRange.y);
                            }
                            onSlideEvent.Invoke(CurrentRange);
                            Vector2Int intRange = new Vector2Int(Mathf.RoundToInt(currentRange.x), Mathf.RoundToInt(currentRange.y));
                            onSlideEventInt.Invoke(intRange);
                        }
                        break;

                    case RangeWidgetPart.MiddleKnob:
                        {
                            if (joyRightJustClicked || joyRightLongPush)
                            {

                                bool hasReachedMax = currentRange.y + 1.0f > GlobalState.Animation.EndFrame;
                                float newMin = hasReachedMax ? currentRange.x : currentRange.x + 1.0f;
                                float newMax = hasReachedMax ? currentRange.y : currentRange.y + 1.0f;
                                CurrentRange = new Vector2(newMin, newMax);
                            }
                            else if (joyLeftJustClicked || joyLeftLongPush)
                            {
                                bool hasReachedMin = currentRange.x - 1.0f < GlobalState.Animation.StartFrame;
                                float newMin = hasReachedMin ? currentRange.x : currentRange.x - 1.0f;
                                float newMax = hasReachedMin ? currentRange.y : currentRange.y - 1.0f;
                                CurrentRange = new Vector2(newMin, newMax);
                            }
                            onSlideEvent.Invoke(CurrentRange);
                            Vector2Int intRange = new Vector2Int(Mathf.RoundToInt(currentRange.x), Mathf.RoundToInt(currentRange.y));
                            onSlideEventInt.Invoke(intRange);
                        }
                        break;

                    case RangeWidgetPart.RightKnob:
                        {
                            if (joyRightJustClicked || joyRightLongPush)
                            {
                                float newMax = Mathf.Clamp(currentRange.y + 1.0f, currentRange.x + 1.0f, GlobalState.Animation.EndFrame);
                                CurrentRange = new Vector2(currentRange.x, newMax);
                            }
                            else if (joyLeftJustClicked || joyLeftLongPush)
                            {
                                float newMax = Mathf.Clamp(currentRange.y - 1.0f, currentRange.x + 1.0f, GlobalState.Animation.EndFrame);
                                CurrentRange = new Vector2(currentRange.x, newMax);
                            }
                            onSlideEvent.Invoke(CurrentRange);
                            Vector2Int intRange = new Vector2Int(Mathf.RoundToInt(currentRange.x), Mathf.RoundToInt(currentRange.y));
                            onSlideEventInt.Invoke(intRange);
                        }
                        break;
                    default: break;
                }
            }

            // Hovered State for sub elements
            switch (hoveredPart)
            {
                case RangeWidgetPart.LeftKnob:
                    minKnob.Hovered = true;
                    maxKnob.Hovered = false;
                    midKnob.Hovered = false;
                    break;

                case RangeWidgetPart.MiddleKnob:
                    minKnob.Hovered = false;
                    maxKnob.Hovered = false;
                    midKnob.Hovered = true;
                    break;

                case RangeWidgetPart.RightKnob:
                    minKnob.Hovered = false;
                    maxKnob.Hovered = true;
                    midKnob.Hovered = false;
                    break;

                default:
                    minKnob.Hovered = false;
                    maxKnob.Hovered = false;
                    midKnob.Hovered = false;
                    break;
            }
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();
        }

        public override void OnRayExit()
        {
            base.OnRayExit();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            // exiting while clicking shows a pushed slider, because we are acting on it, not like a button.
            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayClick()
        {
            base.OnRayClick();
            onClickEvent.Invoke();
        }

        public override void OnRayReleaseInside()
        {
            base.OnRayReleaseInside();
            onReleaseEvent.Invoke();
        }

        public override bool OnRayReleaseOutside()
        {
            onReleaseEvent.Invoke();
            return base.OnRayReleaseOutside();
        }

        public override bool OverridesRayEndPoint() { return true; }

        float lastProjected;
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.rightController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

            Vector3 localProjectedWidgetPosition = GetLocalProjected(ray);

            if (IgnoreRayInteraction())
            {
                // return endPoint at the surface of the widget.
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }

            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;
            Vector2 globalRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            float currentMinPct = (currentRange.x - globalRange.x) / (globalRange.y - globalRange.x);
            float currentMinPosX = startX + currentMinPct * (endX - startX);
            float currentMaxPct = (currentRange.y - globalRange.x) / (globalRange.y - globalRange.x);
            float currentMaxPosX = startX + currentMaxPct * (endX - startX);
            float halfRangeSizeX = (currentMaxPosX - currentMinPosX) / 2.0f;

            RangeWidgetPart hoveredPart = GetRangeWidgetPart(localProjectedWidgetPosition);

            //
            // Just triggered? Grip range min/max/middle or spawn keyboard.
            //

            if (triggerJustClicked)
            {
                grippedPart = RangeWidgetPart.Background; // reset

                switch (hoveredPart)
                {
                    case RangeWidgetPart.NameLabel:
                        {
                            // SPAWN KEYBOARD
                            ToolsUIManager.Instance.OpenKeyboard(OnValidateKeyboard, transform);
                            keyboardSourcePart = hoveredPart;
                            keyboardOpen = true;
                            rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                            return;
                        }
                    case RangeWidgetPart.GlobalMaxLabel:
                        {
                            // SPAWN KEYBOARD
                            ToolsUIManager.Instance.OpenNumericKeyboard(OnValidateNumericKeyboard, transform, GlobalState.Animation.EndFrame);
                            keyboardSourcePart = hoveredPart;
                            keyboardOpen = true;
                            rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                            return;
                        }
                    case RangeWidgetPart.GlobalMinLabel:
                        {
                            // SPAWN KEYBOARD
                            ToolsUIManager.Instance.OpenNumericKeyboard(OnValidateNumericKeyboard, transform, GlobalState.Animation.StartFrame);
                            keyboardSourcePart = hoveredPart;
                            keyboardOpen = true;
                            rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                            return;
                        }

                    case RangeWidgetPart.LeftKnob:
                    case RangeWidgetPart.MiddleKnob:
                    case RangeWidgetPart.RightKnob:
                        {
                            grippedPart = hoveredPart;
                            break;
                        }

                    case RangeWidgetPart.Background:
                    case RangeWidgetPart.Rail:
                    default:
                        break;
                }
            }

            //
            // Is gripped on something? Compute value and ray end point (canne a peche).
            //

            if (grippedPart == RangeWidgetPart.LeftKnob
             || grippedPart == RangeWidgetPart.MiddleKnob
             || grippedPart == RangeWidgetPart.RightKnob)
            {
                // DRAG

                if (!triggerJustClicked) // if trigger just clicked, use the actual projection, no interpolation.
                {
                    float drag = GlobalState.Settings.RaySliderDrag;
                    localProjectedWidgetPosition.x = Mathf.Lerp(lastProjected, localProjectedWidgetPosition.x, drag);
                }
                lastProjected = localProjectedWidgetPosition.x;

                // CLAMP

                switch (grippedPart)
                {
                    case RangeWidgetPart.LeftKnob:
                        {
                            if (localProjectedWidgetPosition.x < startX)
                                localProjectedWidgetPosition.x = startX;

                            if (localProjectedWidgetPosition.x > currentMaxPosX - 3.0f * knobRadius)
                                localProjectedWidgetPosition.x = currentMaxPosX - 3.0f * knobRadius;

                            // SET

                            float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);
                            float v = globalRange.x + pct * (globalRange.y - globalRange.x);

                            CurrentRange = new Vector2(v, CurrentRange.y);
                        }
                        break;

                    case RangeWidgetPart.MiddleKnob:
                        {
                            if (localProjectedWidgetPosition.x < startX + halfRangeSizeX)
                                localProjectedWidgetPosition.x = startX + halfRangeSizeX;

                            if (localProjectedWidgetPosition.x > endX - halfRangeSizeX)
                                localProjectedWidgetPosition.x = endX - halfRangeSizeX;

                            // SET

                            float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);
                            float halfRange = (currentRange.y - currentRange.x) / 2.0f;
                            float v = globalRange.x + pct * (globalRange.y - globalRange.x);

                            CurrentRange = new Vector2(v - halfRange, v + halfRange);
                        }
                        break;

                    case RangeWidgetPart.RightKnob:
                        {
                            if (localProjectedWidgetPosition.x < currentMinPosX + 3.0f * knobRadius)
                                localProjectedWidgetPosition.x = currentMinPosX + 3.0f * knobRadius;

                            if (localProjectedWidgetPosition.x > endX)
                                localProjectedWidgetPosition.x = endX;

                            // SET

                            float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);
                            float v = globalRange.x + pct * (globalRange.y - globalRange.x);

                            CurrentRange = new Vector2(currentRange.x, v);
                        }
                        break;
                }

                localProjectedWidgetPosition.y = -height / 2.0f;

                // SIGNAL

                onSlideEvent.Invoke(CurrentRange);
                Vector2Int intRange = new Vector2Int(Mathf.RoundToInt(currentRange.x), Mathf.RoundToInt(currentRange.y));
                onSlideEventInt.Invoke(intRange);
            }

            // OUT ray end point

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        public Vector3 GetLocalProjected(Ray ray)
        {
            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            float enter;
            widgetPlane.Raycast(ray, out enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            return localProjectedWidgetPosition;
        }

        public RangeWidgetPart GetRangeWidgetPart(Vector3 pos)
        {
            float widthWithoutMargins = width - 2.0f * margin;
            float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;

            if (pos.x < margin || pos.x > width - margin)
            {
                return RangeWidgetPart.Background;
            }
            else if (pos.x < margin + widthWithoutMargins * labelPositionEnd)
            {
                return RangeWidgetPart.NameLabel;
            }
            else if (pos.x < startX)
            {
                return RangeWidgetPart.GlobalMinLabel;
            }
            else if (pos.x < endX)
            {
                Vector2 globalRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                float currentMinPct = (currentRange.x - globalRange.x) / (globalRange.y - globalRange.x);
                float currentMinPos = startX + currentMinPct * (endX - startX);

                float currentMaxPct = (currentRange.y - globalRange.x) / (globalRange.y - globalRange.x);
                float currentMaxPos = startX + currentMaxPct * (endX - startX);

                if (pos.x < currentMinPos - knobRadius || pos.x > currentMaxPos + knobRadius)
                {
                    return RangeWidgetPart.Rail;
                }
                else if (pos.x < currentMinPos + knobRadius)
                {
                    return RangeWidgetPart.LeftKnob;
                }
                else if (pos.x < currentMaxPos - knobRadius)
                {
                    return RangeWidgetPart.MiddleKnob;
                }
                else
                {
                    return RangeWidgetPart.RightKnob;
                }
            }
            else 
            {
                return RangeWidgetPart.GlobalMaxLabel;
            }
        }

        #endregion

        #region create

        public class CreateArgs
        {
            public Transform parent = null;
            public string widgetName = UIRange.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIRange.default_thickness);
            public float width = UIRange.default_width;
            public float height = UIRange.default_height;
            public float margin = UIRange.default_margin;
            public float thickness = UIRange.default_thickness;
            public float labelEnd = UIRange.default_label_end;
            public float sliderBegin = UIRange.default_slider_begin;
            public float sliderEnd = UIRange.default_slider_end;
            public float railMargin = UIRange.default_rail_margin;
            public float railThickness = UIRange.default_rail_thickness;
            public float knobRadius = UIRange.default_knob_radius;
            public float knobDepth = UIRange.default_knob_depth;
            public RangeContent content = UIRange.default_content;
            public RangeValueType valueType = UIRange.default_value_type;
            public float currentMinValue = UIRange.default_current_min_value;
            public float currentMaxValue = UIRange.default_current_max_value;
            
            public Material material = UIUtils.LoadMaterial(UIRange.default_material_name);
            public Material railMaterial = UIUtils.LoadMaterial(UIRange.default_rail_material_name);
            public Material knobCenterMaterial = UIUtils.LoadMaterial(UIRange.default_knob_center_material_name);
            public Material knobEndMaterial = UIUtils.LoadMaterial(UIRange.default_knob_end_material_name);

            public ColorVar color = UIOptions.BackgroundColorVar;
            public ColorVar textColor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public ColorVar railColor = UIOptions.RangeRailColorVar;
            public ColorVar knobCenterColor = UIOptions.RangeKnobCenterColorVar;
            public ColorVar knobEndColor = UIOptions.RangeKnobEndColorVar;

            public string label = UIRange.default_label;
        }

        public static UIRange Create(CreateArgs input)
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

            UIRange uiRange = go.AddComponent<UIRange>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiRange.relativeLocation = input.relativeLocation;
            uiRange.transform.parent = input.parent;
            uiRange.transform.localPosition = parentAnchor + input.relativeLocation;
            uiRange.transform.localRotation = Quaternion.identity;
            uiRange.transform.localScale = Vector3.one;
            uiRange.width = input.width;
            uiRange.height = input.height;
            uiRange.margin = input.margin;
            uiRange.thickness = input.thickness;
            uiRange.labelPositionEnd = input.labelEnd;
            uiRange.sliderPositionBegin = input.sliderBegin;
            uiRange.sliderPositionEnd = input.sliderEnd;
            uiRange.railMargin = input.railMargin;
            uiRange.railThickness = input.railThickness;
            uiRange.knobRadius = input.knobRadius;
            uiRange.knobDepth = input.knobDepth;
            uiRange.content = input.content;
            uiRange.valueType = input.valueType;
            uiRange.currentRange.x = input.currentMinValue;
            uiRange.currentRange.y = input.currentMaxValue;
            uiRange.labelContent = input.label;
            uiRange.sourceMaterial = input.material;
            uiRange.sourceRailMaterial = input.railMaterial;
            uiRange.sourceKnobCenterMaterial = input.knobCenterMaterial;
            uiRange.sourceKnobEndMaterial = input.knobEndMaterial;
            uiRange.baseColor.useConstant = false;
            uiRange.baseColor.reference = input.color;
            uiRange.textColor.useConstant = false;
            uiRange.textColor.reference = input.textColor;
            uiRange.pushedColor.useConstant = false;
            uiRange.pushedColor.reference = input.pushedColor;
            uiRange.selectedColor.useConstant = false;
            uiRange.selectedColor.reference = input.selectedColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiRange.Anchor = Vector3.zero;
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
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiRange.SetColor(input.color.value);
            }

            //
            // RAIL
            //

            float railWidth = (input.width - 2 * input.margin) * (input.sliderEnd - input.sliderBegin);
            float railHeight = 3 * uiRange.railMargin; // TODO: see if we can tie this to another variable, like height.
            float railThickness = uiRange.railThickness;
            float railMargin = uiRange.railMargin;
            Vector3 railPosition = new Vector3(input.margin + (input.width - 2 * input.margin) * input.sliderBegin, -input.height / 2, -railThickness); // put z = 0 back

            uiRange.rail = UIRangeRail.Create(
                new UIRangeRail.CreateArgs
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

            Vector2 globalRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            // Left (min) KNOB
            {
                float newKnobRadius = uiRange.knobRadius * 0.9f;// .8f; // smaller
                float newKnobDepth = uiRange.knobDepth * 1.2f; // taller

                float pct = (uiRange.currentRange.x - globalRange.x) / (globalRange.y - globalRange.x);

                float widthWithoutMargins = input.width - 2.0f * input.margin;
                float startX = input.margin + widthWithoutMargins * uiRange.sliderPositionBegin + railMargin;
                float endX = input.margin + widthWithoutMargins * uiRange.sliderPositionEnd - railMargin;
                float posX = startX + pct * (endX - startX);

                Vector3 knobPosition = new Vector3(posX - newKnobRadius, newKnobRadius - (uiRange.height / 2.0f), -newKnobDepth);

                uiRange.minKnob = UIRangeKnob.Create(
                    new UIRangeKnob.CreateArgs
                    {
                        widgetName = "MinKnob",
                        parent = go.transform,
                        relativeLocation = knobPosition,
                        width = 2.0f * newKnobRadius,
                        radius = newKnobRadius,
                        depth = newKnobDepth,
                        material = input.knobEndMaterial,
                        baseColor = input.knobEndColor,
                        textColor = input.textColor,
                        initialValue = input.currentMinValue,
                        textBehavior = UIRangeKnob.TextBehavior.Center
                    }
                );
            }

            // Right (max) KNOB
            {
                float newKnobRadius = uiRange.knobRadius * 0.9f;// .8f; // smaller
                float newKnobDepth = uiRange.knobDepth * 1.2f; // taller

                float pct = (uiRange.currentRange.y - globalRange.x) / (globalRange.y - globalRange.x);

                float widthWithoutMargins = input.width - 2.0f * input.margin;
                float startX = input.margin + widthWithoutMargins * uiRange.sliderPositionBegin + railMargin;
                float endX = input.margin + widthWithoutMargins * uiRange.sliderPositionEnd - railMargin;
                float posX = startX + pct * (endX - startX);

                Vector3 knobPosition = new Vector3(posX - newKnobRadius, newKnobRadius - (uiRange.height / 2.0f), -newKnobDepth);

                uiRange.maxKnob = UIRangeKnob.Create(
                    new UIRangeKnob.CreateArgs
                    {
                        widgetName = "MaxKnob",
                        parent = go.transform,
                        relativeLocation = knobPosition,
                        width = 2.0f * newKnobRadius,
                        radius = newKnobRadius,
                        depth = newKnobDepth,
                        material = input.knobEndMaterial,
                        baseColor = input.knobEndColor,
                        textColor = input.textColor,
                        initialValue = input.currentMaxValue,
                        textBehavior = UIRangeKnob.TextBehavior.Center
                    }
                );
            }

            // Middle KNOB
            {
                float newKnobRadius = uiRange.knobRadius;
                float newKnobDepth = uiRange.knobDepth;

                float pctBegin = (uiRange.currentRange.x - globalRange.x) / (globalRange.y - globalRange.x);
                float pctEnd = (uiRange.currentRange.y - globalRange.x) / (globalRange.y - globalRange.x);

                float widthWithoutMargins = input.width - 2.0f * input.margin;
                float startX = input.margin + widthWithoutMargins * uiRange.sliderPositionBegin + railMargin;
                float endX = input.margin + widthWithoutMargins * uiRange.sliderPositionEnd - railMargin;
                float posX = startX + pctBegin * (endX - startX);
                float posXE = startX + pctEnd * (endX - startX);
                float newWidth = posXE - posX;
                
                Vector3 knobPosition = new Vector3(posX - newKnobRadius, newKnobRadius - (uiRange.height / 2.0f), -newKnobDepth);

                uiRange.midKnob = UIRangeKnob.Create(
                    new UIRangeKnob.CreateArgs
                    {
                        widgetName = "MidKnob",
                        parent = go.transform,
                        relativeLocation = knobPosition,
                        width = newWidth,
                        radius = newKnobRadius,
                        depth = newKnobDepth,
                        material = input.knobCenterMaterial,
                        baseColor = input.knobCenterColor,
                        textColor = input.textColor,
                        textBehavior = UIRangeKnob.TextBehavior.Hidden
                    }
                );
            }

            //
            // CANVAS (to hold the 2 texts)
            //

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiRange.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiRange.width, uiRange.height);
            rt.localPosition = Vector3.zero;

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            // LABEL
            if (input.content == RangeContent.All || input.content == RangeContent.LabelOnly)
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
                t.text = input.label;
                t.enableAutoSizing = true;
                t.fontSizeMin = 1;
                t.fontSizeMax = 500;
                t.fontStyle = FontStyles.Normal;
                t.alignment = TextAlignmentOptions.Left;
                t.color = input.textColor.value;
                t.ForceMeshUpdate();

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.sizeDelta = new Vector2((uiRange.width - 2 * uiRange.margin) * uiRange.labelPositionEnd * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                float textPosLeft = uiRange.margin;
                trt.localPosition = new Vector3(textPosLeft, -uiRange.margin, -0.002f);
            }

            // Min/Max value texts
            if (input.content == RangeContent.All || input.content == RangeContent.MinMaxOnly)
            {
                {
                    GameObject text = new GameObject("MinTextValue");
                    text.transform.parent = canvas.transform;

                    TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
                    t.text = input.currentMinValue.ToString("#0.00");
                    t.enableAutoSizing = true;
                    t.fontSizeMin = 1;
                    t.fontSizeMax = 500;
                    t.fontSize = 1.85f;
                    t.fontStyle = FontStyles.Normal;
                    t.alignment = TextAlignmentOptions.Left;
                    t.color = input.textColor.value;

                    RectTransform trt = t.GetComponent<RectTransform>();
                    trt.localScale = 0.01f * Vector3.one;
                    trt.localRotation = Quaternion.identity;
                    trt.anchorMin = new Vector2(0, 1);
                    trt.anchorMax = new Vector2(0, 1);
                    trt.pivot = new Vector2(0, 1); // top left
                    trt.sizeDelta = new Vector2((uiRange.width - 2 * uiRange.margin) * (uiRange.sliderPositionBegin - uiRange.labelPositionEnd) * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                    float textPosLeft = uiRange.margin + (uiRange.width - 2 * uiRange.margin) * uiRange.labelPositionEnd;
                    trt.localPosition = new Vector3(textPosLeft, -uiRange.margin, -0.002f);
                }

                {
                    GameObject text = new GameObject("MaxTextValue");
                    text.transform.parent = canvas.transform;

                    TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
                    t.text = input.currentMaxValue.ToString("#0.00");
                    t.enableAutoSizing = true;
                    t.fontSizeMin = 1;
                    t.fontSizeMax = 500;
                    t.fontSize = 1.85f;
                    t.fontStyle = FontStyles.Normal;
                    t.alignment = TextAlignmentOptions.Right;
                    t.color = input.textColor.value;

                    RectTransform trt = t.GetComponent<RectTransform>();
                    trt.localScale = 0.01f * Vector3.one;
                    trt.localRotation = Quaternion.identity;
                    trt.anchorMin = new Vector2(0, 1);
                    trt.anchorMax = new Vector2(0, 1);
                    trt.pivot = new Vector2(1, 1); // top right?
                    trt.sizeDelta = new Vector2((uiRange.width - 2 * uiRange.margin) * (1 - uiRange.sliderPositionEnd) * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                    float textPosRight = uiRange.width - uiRange.margin;
                    trt.localPosition = new Vector3(textPosRight, -uiRange.margin, -0.002f);
                }
            }

            UIUtils.SetRecursiveLayer(go, "UI");
            uiRange.SetLightLayer(2);

            return uiRange;
        }

        #endregion
    }
}
