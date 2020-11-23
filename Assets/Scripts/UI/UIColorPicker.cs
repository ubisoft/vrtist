using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class UIColorPicker : UIElement
    {
        // TODO: handle a "previous" color ??

        private static readonly string default_widget_name = "New ColorPicker";
        private static readonly float default_width = 0.20f;
        private static readonly float default_height = 0.25f;
        private static readonly float default_thickness = UIElement.default_element_thickness;
        private static readonly float default_padding = 0.01f;
        private static readonly float default_alphaToSaturationRatio = 0.14f;
        private static readonly float default_alphaToPreviewRatio = 0.84f;

        private static readonly string default_alpha_material_name = "AlphaMaterial";
        private static readonly string default_alpha_cursor_name = "Cursor_Alpha";

        private static readonly string default_preview_material_name = "PreviewMaterial";

        [SpaceHeader("Picker Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float thickness = default_thickness;
        [CentimeterFloat] public float padding = default_padding;
        public float alphaToSaturationRatio = default_alphaToSaturationRatio;
        public float alphaToPreviewRatio = default_alphaToPreviewRatio;
        public float trianglePct = UIColorPickerHSV.default_trianglePct;
        public float innerCirclePct = UIColorPickerHSV.default_innerCirclePct;
        public float outerCirclePct = UIColorPickerHSV.default_outerCirclePct;

        [SpaceHeader("SubComponents", 6, 0.8f, 0.8f, 0.8f)]
        public UIColorPickerHSV hsv = null;
        public UIColorPickerAlpha alpha = null;
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

            IsHdr = (v > 1.0f);

            // try to infer original value from hdr value
            while (v > 1.0f)
            {
                v /= 2.0f;
            }

            // Protect because the Color is set OnEnable, before everything is created.
            if (null != hsv)
            {
                hsv.HSV = new Vector3(h, s, v);
                alpha.SetAlpha(color.a);
                preview.SetPreviewColor(Color.HSVToRGB(h, s, v));
            }
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

        public void OnCheckHDR(bool value)
        {
            isHdr = value;
            hdrIntensity = 1.0f;

            OnColorChanged(); // we want to update the preview and signal all listeners.
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

        // Called when changing anything in sub-components.
        // Read every value, build a color and fire the global public event.
        public void OnColorChanged()
        {
            float h = hsv.Hue;
            float s = hsv.Saturation;
            float v = hsv.Value;

            currentColor = Color.HSVToRGB(h, s, v);

            if (IsHdr)
            {
                currentColor *= hdrIntensity;
            }

            preview.SetPreviewColor(currentColor.gamma); // back to sRGB

            currentColor.a = alpha.GetAlpha();

            onColorChangedEvent.Invoke(currentColor);
        }



        public override void RebuildMesh()
        {
            Vector3 hsvPosition = new Vector3(0.0f, 0.0f, 0.0f);
            float hsvWidth = width;
            float hsvHeight = (1.0f-alphaToSaturationRatio) * (height - padding); // 88%
            float hsvThickness = thickness;
            hsv.RebuildMesh(hsvWidth, hsvHeight, hsvThickness, trianglePct, innerCirclePct, outerCirclePct);
            hsv.relativeLocation = hsvPosition;

            Vector3 alphaPosition = new Vector3(0.0f, -hsvHeight - padding, 0.0f);
            float alphaWidth = alphaToPreviewRatio * (width - padding);
            float alphaHeight = alphaToSaturationRatio * (height - padding);
            float alphaThickness = thickness;
            alpha.RebuildMesh(alphaWidth, alphaHeight, alphaThickness);
            alpha.relativeLocation = alphaPosition;

            Vector3 previewPosition = new Vector3(alphaToPreviewRatio * (width - padding) + padding, -hsvHeight - padding, 0.0f);
            float previewWidth = (1.0f -alphaToPreviewRatio) * (width - padding);
            float previewHeight = alphaToSaturationRatio * (height - padding);
            float previewThickness = thickness;
            preview.RebuildMesh(previewWidth, previewHeight, previewThickness);
            preview.relativeLocation = previewPosition;

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
            public Vector3 relativeLocation = new Vector3(0, 0, -default_thickness);
            public float width = default_width;
            public float height = default_height;
            public float thickness = default_thickness;
            public float padding = default_padding;
            public float alphaToSaturationRatio = default_alphaToSaturationRatio;
            public float alphaToPreviewRatio = default_alphaToPreviewRatio;

            public float trianglePct = UIColorPickerHSV.default_trianglePct;
            public float innerCirclePct = UIColorPickerHSV.default_innerCirclePct;
            public float outerCirclePct = UIColorPickerHSV.default_outerCirclePct;

            public Material alphaMaterial = UIUtils.LoadMaterial(default_alpha_material_name);
            public GameObject alphaCursorPrefab = UIUtils.LoadPrefab(default_alpha_cursor_name);

            public Material previewMaterial = UIUtils.LoadMaterial(default_preview_material_name);
        }

        public static void Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            //go.tag = "UICollider"; le colorpicker en lui meme n'a pas de geometrie a collider, seulement ses enfants.

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
            uiColorPicker.trianglePct = input.trianglePct;
            uiColorPicker.innerCirclePct = input.innerCirclePct;
            uiColorPicker.outerCirclePct = input.outerCirclePct;

            //
            // Sub Components
            //

            //      HSV

            Vector3 hsvPosition = new Vector3(0.0f, 0.0f, 0.0f);
            float hsvWidth = input.width;
            float hsvHeight = (1.0f - input.alphaToSaturationRatio) * (input.height - input.padding);
            float hsvThickness = input.thickness;

            uiColorPicker.hsv = UIColorPickerHSV.Create( 
                new UIColorPickerHSV.CreateParams {
                    parent = go.transform,
                    widgetName = "Hsv", 
                    relativeLocation = hsvPosition, 
                    width = hsvWidth, 
                    height = hsvHeight, 
                    thickness = hsvThickness
            });
            uiColorPicker.hsv.colorPicker = uiColorPicker;

            //      Alpha

            Vector3 alphaPosition = new Vector3(0.0f, (1.0f - input.alphaToSaturationRatio) * -(input.height - input.padding) - input.padding, 0.0f);
            float alphaWidth = input.alphaToPreviewRatio * (input.width - input.padding);
            float alphaHeight = input.alphaToSaturationRatio * (input.height - input.padding);
            float alphaThickness = input.thickness;

            uiColorPicker.alpha = UIColorPickerAlpha.Create(
                "Alpha", go.transform, 
                alphaPosition, alphaWidth, alphaHeight, alphaThickness,
                input.alphaMaterial, input.alphaCursorPrefab);
            uiColorPicker.alpha.colorPicker = uiColorPicker;

            //      Preview

            Vector3 previewPosition = new Vector3(input.alphaToPreviewRatio * (input.width - input.padding) + input.padding, (1.0f - input.alphaToSaturationRatio) * -(input.height - input.padding) - input.padding, 0.0f);
            float previewWidth = (1.0f - input.alphaToPreviewRatio) * (input.width - input.padding);
            float previewHeight = input.alphaToSaturationRatio * (input.height - input.padding);
            float previewThickness = input.thickness;

            uiColorPicker.preview = UIColorPickerPreview.CreateUIColorPickerPreview(
                "Preview", go.transform,
                previewPosition, previewWidth, previewHeight, previewThickness,
                input.previewMaterial);

            UIUtils.SetRecursiveLayer(go, "UI");
        }
    }
}
