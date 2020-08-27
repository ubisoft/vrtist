using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class UIColorPicker : UIElement
    {
        private static readonly string default_widget_name = "New ColorPicker";
        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.3f;
        private static readonly float default_thickness = UIElement.default_element_thickness;
        private static readonly float default_padding = 0.01f;
        private static readonly float default_hueToSaturationRatio = 0.12f;
        private static readonly float default_hueToPreviewRatio = 0.88f;
        private static readonly string default_saturation_material_name = "Saturation";
        private static readonly string default_hue_material_name = "Hue";
        private static readonly string default_preview_material_name = "Preview";
        private static readonly string default_saturation_cursor_name = "Cursor_Saturation";
        private static readonly string default_hue_cursor_name = "Cursor_Hue";

        [SpaceHeader("Picker Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float thickness = 0.001f;
        [CentimeterFloat] public float padding = 0.01f;
        public float hueToSaturationRatio = 0.12f;//1.0f / 7.0f;
        public float hueToPreviewRatio = 0.88f;//3.0f / 4.0f;

        [SpaceHeader("SubComponents", 6, 0.8f, 0.8f, 0.8f)]
        public UIColorPickerSaturation saturation = null;
        public UIColorPickerHue hue = null;
        public UIColorPickerPreview preview = null;
        public UICheckbox hdrCheckbox = null;
        public UIButton minusOneButton = null;
        public UIButton resetHdrButton = null;
        public UIButton plusOneButton = null;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public ColorChangedEvent onColorChangedEvent = new ColorChangedEvent();
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        private bool isHdr = false;
        private float hdrIntensity = 1.0f;
        public bool IsHdr
        {
            get { return isHdr; }
            set { isHdr = value; hdrIntensity = 1.0f; UpdateIsHdr(value); } // reset intensity???
        }

        private Color currentColor;
        public Color CurrentColor
        {
            get { return currentColor; }
            set { SetPickedColor(value); }
        }

        private void OnEnable()
        {
            CurrentColor = CurrentColor;
        }

        public void SetPickedColor(Color color)
        {
            currentColor = color;
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            hue.SetHue(h);

            Color baseColor = Color.HSVToRGB(h, 1f, 1f);
            saturation.SetBaseColor(baseColor);

            // try to infer original value from hdr value
            while (v > 1.0f)
            {
                v /= 2.0f;
            }

            saturation.SetSaturation(new Vector2(s, v));

            preview.SetPreviewColor(Color.HSVToRGB(h, s, v));
        }

        public void OnMinusOneHdr()
        {
            if (IsHdr)
            {
                hdrIntensity /= 2.0f;
                OnColorChanged();
            }
        }

        public void OnResetHdr()
        {
            if (IsHdr)
            {
                hdrIntensity = 1.0f;
                OnColorChanged();
            }
        }

        public void OnPlusOneHdr()
        {
            if (IsHdr)
            {
                hdrIntensity *= 2.0f;
                OnColorChanged();
            }
        }

        private void UpdateIsHdr(bool value)
        {
            hdrCheckbox.Checked = value;
        }




        public void OnClick()
        {
            onClickEvent.Invoke();
        }

        public void OnRelease()
        {
            onReleaseEvent.Invoke();
        }

        public void OnColorChanged()
        {
            float h = hue.GetHue(); // get linear position on rainbow
            Vector2 sat = saturation.GetSaturation(); // get linear vec2, cursor position on square

            Color baseColor = Color.HSVToRGB(h, 1f, 1f);
            currentColor = Color.Lerp(Color.white, baseColor.linear, sat.x); // linear computations on the linear version of the color
            currentColor = Color.Lerp(Color.black, currentColor, sat.y);

            saturation.SetBaseColor(baseColor);

            preview.SetPreviewColor(currentColor.gamma); // back to sRGB

            if (IsHdr)
            {
                currentColor *= hdrIntensity;
            }

            onColorChangedEvent.Invoke(currentColor);
        }



        public override void RebuildMesh()
        {
            Vector3 saturationPosition = new Vector3(0.0f, hueToSaturationRatio * -(height - padding) - padding, 0.0f);
            float saturationWidth = width;
            float saturationHeight = (1.0f-hueToSaturationRatio) * (height - padding);
            float saturationThickness = thickness;

            saturation.RebuildMesh(saturationWidth, saturationHeight, saturationThickness);
            saturation.relativeLocation = saturationPosition;
            //saturation.transform.localPosition = Anchor + saturationPosition;

            Vector3 huePosition = new Vector3(0.0f, 0.0f, 0.0f);
            float hueWidth = hueToPreviewRatio * (width - padding);
            float hueHeight = hueToSaturationRatio * (height - padding);
            float hueThickness = thickness;

            hue.RebuildMesh(hueWidth, hueHeight, hueThickness);
            hue.relativeLocation = huePosition;
            //hue.transform.localPosition = Anchor + huePosition;

            Vector3 previewPosition = new Vector3(hueToPreviewRatio * (width - padding) + padding, 0.0f, 0.0f);
            float previewWidth = (1.0f -hueToPreviewRatio) * (width - padding);
            float previewHeight = hueToSaturationRatio * (height - padding);
            float previewThickness = thickness;

            preview.RebuildMesh(previewWidth, previewHeight, previewThickness);
            preview.relativeLocation = previewPosition;
            //preview.transform.localPosition = Anchor + previewPosition;

            // TODO: also move the HDR buttons
            // ...
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
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                //ResetColor();
                NeedsRebuild = false;
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
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }






        public class CreateArgs
        {
            public Transform parent = null;
            public string widgetName = UIColorPicker.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIColorPicker.default_thickness);
            public float width = UIColorPicker.default_width;
            public float height = UIColorPicker.default_height;
            public float thickness = UIColorPicker.default_thickness;
            public float padding = UIColorPicker.default_padding;
            public float hueToSaturationRatio = UIColorPicker.default_hueToSaturationRatio;
            public float hueToPreviewRatio = UIColorPicker.default_hueToPreviewRatio;
            public Material saturationMaterial = UIUtils.LoadMaterial(UIColorPicker.default_saturation_material_name);
            public Material hueMaterial = UIUtils.LoadMaterial(UIColorPicker.default_hue_material_name);
            public Material previewMaterial = UIUtils.LoadMaterial(UIColorPicker.default_preview_material_name);
            public GameObject saturationCursorPrefab = UIUtils.LoadPrefab(UIColorPicker.default_saturation_cursor_name);
            public GameObject hueCursorPrefab = UIUtils.LoadPrefab(UIColorPicker.default_hue_cursor_name);
        }

        public static void Create( CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            //go.tag = "UICollider"; le colorpicker en lui meme n'a pas de geometrie a collider, seulement ses enfants.
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

            UIColorPicker uiColorPicker = go.AddComponent<UIColorPicker>();
            uiColorPicker.relativeLocation = input.relativeLocation;
            uiColorPicker.transform.parent = input.parent;
            uiColorPicker.transform.localPosition = parentAnchor + input.relativeLocation;
            uiColorPicker.transform.localRotation = Quaternion.identity;
            uiColorPicker.transform.localScale = Vector3.one;
            uiColorPicker.width = input.width;
            uiColorPicker.height = input.height;
            uiColorPicker.thickness = input.thickness;
            uiColorPicker.padding = input.padding;

            //
            // Sub Components
            //

            //      Saturation

            Vector3 saturationPosition = new Vector3(0.0f, input.hueToSaturationRatio * -(input.height - input.padding) - input.padding, 0.0f);
            float saturationWidth = input.width;
            float saturationHeight = (1.0f - input.hueToSaturationRatio) * (input.height - input.padding);
            float saturationThickness = input.thickness;

            uiColorPicker.saturation = UIColorPickerSaturation.CreateUIColorPickerSaturation(
                "Saturation", go.transform, 
                saturationPosition, saturationWidth, saturationHeight, saturationThickness,
                input.saturationMaterial, input.saturationCursorPrefab);
            uiColorPicker.saturation.colorPicker = uiColorPicker;

            //      Hue

            Vector3 huePosition = new Vector3(0.0f, 0.0f, 0.0f);
            float hueWidth = input.hueToPreviewRatio * (input.width - input.padding);
            float hueHeight = input.hueToSaturationRatio * (input.height - input.padding);
            float hueThickness = input.thickness;

            uiColorPicker.hue = UIColorPickerHue.CreateUIColorPickerHue(
                "Hue", go.transform,
                huePosition, hueWidth, hueHeight, hueThickness,
                input.hueMaterial, input.hueCursorPrefab);
            uiColorPicker.hue.colorPicker = uiColorPicker;

            //      Preview

            Vector3 previewPosition = new Vector3(input.hueToPreviewRatio * (input.width - input.padding) + input.padding, 0.0f, 0.0f);
            float previewWidth = (1.0f - input.hueToPreviewRatio) * (input.width - input.padding);
            float previewHeight = input.hueToSaturationRatio * (input.height - input.padding);
            float previewThickness = input.thickness;

            uiColorPicker.preview = UIColorPickerPreview.CreateUIColorPickerPreview(
                "Preview", go.transform,
                previewPosition, previewWidth, previewHeight, previewThickness,
                input.previewMaterial);
        }
    }
}
