using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace VRtist
{
    [ExecuteInEditMode]
    public class UIColorPicker : UIElement
    {
        [SpaceHeader("Picker Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        public float thickness = 0.001f;
        public float padding = 0.01f;
        public float hueToSaturationRatio = 0.12f;//1.0f / 7.0f;
        public float hueToPreviewRatio = 0.88f;//3.0f / 4.0f;

        [SpaceHeader("SubComponents", 6, 0.8f, 0.8f, 0.8f)]
        public UIColorPickerSaturation saturation = null;
        public UIColorPickerHue hue = null;
        public UIColorPickerPreview preview = null;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public ColorChangedEvent onColorChangedEvent = new ColorChangedEvent();

        private Color currentColor;
        public Color CurrentColor
        {
            get { return currentColor; }
            set { SetPickedColor(value); }
        }

        private bool needRebuild = false;

        void Start()
        {
            if (EditorApplication.isPlaying || Application.isPlaying)
            {
                OnColorChanged();
                CurrentColor = new Color(0.2f, 0.5f, 0.6f);
            }
        }

        public void SetPickedColor(Color color)
        {
            currentColor = color;
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            hue.SetHue(h);

            Color baseColor = Color.HSVToRGB(h, 1f, 1f);
            saturation.SetBaseColor(baseColor);
            saturation.SetSaturation(new Vector2(s, v));

            preview.SetColor(color);
        }

        public void OnColorChanged()
        {
            float h = hue.GetHue();
            Vector2 sat = saturation.GetSaturation();

            Color baseColor = Color.HSVToRGB(h, 1f, 1f);
            currentColor = Color.Lerp(Color.white, baseColor, sat.x);
            currentColor = Color.Lerp(Color.black, currentColor, sat.y);

            saturation.SetBaseColor(baseColor);

            preview.SetColor(currentColor);

            onColorChangedEvent.Invoke(currentColor);
        }

        public override void RebuildMesh()
        {
            Vector3 saturationPosition = new Vector3(0.0f, hueToSaturationRatio * -(height - padding) - padding, 0.0f);
            float saturationWidth = width;
            float saturationHeight = (1.0f-hueToSaturationRatio) * (height - padding);
            float saturationThickness = thickness;

            saturation.RebuildMesh(saturationWidth, saturationHeight, saturationThickness);
            saturation.transform.localPosition = Anchor + saturationPosition;

            Vector3 huePosition = new Vector3(0.0f, 0.0f, 0.0f);
            float hueWidth = hueToPreviewRatio * (width - padding);
            float hueHeight = hueToSaturationRatio * (height - padding);
            float hueThickness = thickness;

            hue.RebuildMesh(hueWidth, hueHeight, hueThickness);
            hue.transform.localPosition = Anchor + huePosition;

            Vector3 previewPosition = new Vector3(hueToPreviewRatio * (width - padding) + padding, 0.0f, 0.0f);
            float previewWidth = (1.0f -hueToPreviewRatio) * (width - padding);
            float previewHeight = hueToSaturationRatio * (height - padding);
            float previewThickness = thickness;

            preview.RebuildMesh(previewWidth, previewHeight, previewThickness);
            preview.transform.localPosition = Anchor + previewPosition;
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_thickness = 0.001f;
            const float min_padding = 0.0f;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (thickness < min_thickness)
                thickness = min_thickness;
            if (padding < min_padding)
                padding = min_padding;
            
            // TODO: Test max padding relative to global width.
            //       See UIButton or UIPanel for examples about margin vs width

            needRebuild = true;
        }

        private void Update()
        {
            if (needRebuild)
            {
                // NOTE: I do all these things because properties can't be called from the inspector.
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -2.0f * height / 3.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(0,0, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width, 0, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(0, -height, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width, -height, -0.001f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
        }

        //private void OnTriggerEnter(Collider otherCollider)
        //{
        //    // TODO: pass the Cursor to the button, test the object instead of a hardcoded name.
        //    if (otherCollider.gameObject.name == "Cursor")
        //    {
        //        onClickEvent.Invoke();
        //        VRInput.SendHaptic(VRInput.rightController, 0.03f);
        //    }
        //}

        //private void OnTriggerExit(Collider otherCollider)
        //{
        //    if (otherCollider.gameObject.name == "Cursor")
        //    {
        //        onReleaseEvent.Invoke();
        //    }
        //}

        //private void OnTriggerStay(Collider otherCollider)
        //{
        //    if (otherCollider.gameObject.name == "Cursor")
        //    {
        //        onHoverEvent.Invoke();
        //    }
        //}

        public static void CreateUIColorPicker(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            float padding,
            float hueToSaturationRatio,
            float hueToPreviewRatio,
            Material saturationMaterial,
            Material hueMaterial,
            Material previewMaterial)
        {
            GameObject go = new GameObject(objectName);

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

            UIColorPicker uiColorPicker = go.AddComponent<UIColorPicker>();
            uiColorPicker.relativeLocation = relativeLocation;
            uiColorPicker.transform.parent = parent;
            uiColorPicker.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPicker.transform.localRotation = Quaternion.identity;
            uiColorPicker.transform.localScale = Vector3.one;
            uiColorPicker.width = width;
            uiColorPicker.height = height;
            uiColorPicker.thickness = thickness;
            uiColorPicker.padding = padding;

            // Sub Components

            Vector3 saturationPosition = new Vector3(0.0f, hueToSaturationRatio * -(height - padding) - padding, 0.0f);
            float saturationWidth = width;
            float saturationHeight = (1.0f - hueToSaturationRatio) * (height - padding);
            float saturationThickness = thickness;

            uiColorPicker.saturation = UIColorPickerSaturation.CreateUIColorPickerSaturation(
                "Saturation", go.transform, 
                saturationPosition, saturationWidth, saturationHeight, saturationThickness,
                saturationMaterial);
            uiColorPicker.saturation.colorPicker = uiColorPicker;

            Vector3 huePosition = new Vector3(0.0f, 0.0f, 0.0f);
            float hueWidth = hueToPreviewRatio * (width - padding);
            float hueHeight = hueToSaturationRatio * (height - padding);
            float hueThickness = thickness;

            uiColorPicker.hue = UIColorPickerHue.CreateUIColorPickerHue(
                "Hue", go.transform,
                huePosition, hueWidth, hueHeight, hueThickness,
                hueMaterial);
            uiColorPicker.hue.colorPicker = uiColorPicker;

            Vector3 previewPosition = new Vector3(hueToPreviewRatio * (width - padding) + padding, 0.0f, 0.0f);
            float previewWidth = (1.0f - hueToPreviewRatio) * (width - padding);
            float previewHeight = hueToSaturationRatio * (height - padding);
            float previewThickness = thickness;

            uiColorPicker.preview = UIColorPickerPreview.CreateUIColorPickerPreview(
                "Preview", go.transform,
                previewPosition, previewWidth, previewHeight, previewThickness,
                previewMaterial);
        }
    }
}
