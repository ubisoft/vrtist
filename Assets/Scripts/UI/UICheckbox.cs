using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRtist
{
    /* NOTE: si on veut rendre leur fond transparent
     - MeshRenderer/AdditionalSettings/Priority = 1 (pas oblige, mais comme les labels)
     - UIElementTransparent Sorting Priority = 1 (pas oblige, mais deja fait)
     - Canvas/Canvas/Order in Layer = 1, pour l'image.
     - Canvas/Text/MeshRenderer/AdditionalSettings/Priority = 1
          OU
     - Canvas/Text/TMP/Extra/Order in Layer = 1.
     */

    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UICheckbox : UIElement
    {
        public enum CheckboxContent { CheckboxAndText, CheckboxOnly };

        public static readonly string default_widget_name = "New Checkbox";
        public static readonly float default_width = 0.3f;
        public static readonly float default_height = 0.05f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly string default_material_name = "UIElementTransparent";//"UIBase";
        //public static readonly Color default_color = UIElement.default_background_color;
        public static readonly CheckboxContent default_content = CheckboxContent.CheckboxAndText;
        public static readonly string default_text = "Checkbox";
        public static readonly string default_checked_icon_name = "checkbox_checked";
        public static readonly string default_unchecked_icon_name = "checkbox_unchecked";

        [SpaceHeader("Checkbox Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public Material source_material = null;
        public CheckboxContent content = default_content;
        public Sprite checkedSprite = null;
        public Sprite uncheckedSprite = null;
        [TextArea] public string textContent = "";

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public BoolChangedEvent onCheckEvent = new BoolChangedEvent();
        public UnityEvent onHoverEvent = new UnityEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        public bool isChecked = false;
        public bool Checked { get { return isChecked; } set { isChecked = value; UpdateCheckIcon(); } }

        public string Text { get { return textContent; } set { SetText(value); } }

        private bool grippedUnderThreshold = false;
        private Vector2 grippedPos = Vector2.zero;

        public override void ResetColor()
        {
            SetColor(Disabled ? DisabledColor
                  : (Pushed ? PushedColor
                  //                  : (Checked ? CheckedColor // NO specific color for CHECKED checkboxes.
                  : (Selected ? SelectedColor
                  : (Hovered ? HoveredColor
                  : BaseColor))));

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

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UICheckbox_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
            UpdateCheckIcon();
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

                // TEXT
                TextMeshProUGUI text = canvas.gameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = Text;
                    text.color = TextColor;
                    RectTransform rt = text.gameObject.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = new Vector2((width - minSide - margin) * 100.0f, (height - 2.0f * margin) * 100.0f);
                        rt.localPosition = new Vector3(minSide, -margin, -0.002f);
                    }
                }
            }
        }

        private void UpdateCheckIcon()
        {
            Image img = gameObject.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.sprite = isChecked ? checkedSprite : uncheckedSprite;
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

                Material materialInstance = Instantiate(source_material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UICheckbox_Material_Instance";
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
                ResetColor();
                NeedsRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        private void SetText(string textValue)
        {
            textContent = textValue;

            TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = textValue;
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
            WidgetBorderHapticFeedback();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);
            onHoverEvent.Invoke();
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();
            onHoverEvent.Invoke();
        }

        public override void OnRayExit()
        {
            base.OnRayExit();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayClick()
        {
            base.OnRayClick();
            onClickEvent.Invoke();
        }

        public override void OnRayReleaseInside()
        {
            onReleaseEvent.Invoke();

            Hovered = true;
            Pushed = false;

            Checked = !Checked;
            onCheckEvent.Invoke(Checked);

            ResetColor();

            grippedUnderThreshold = false;
        }

        public override bool OnRayReleaseOutside()
        {
            //return base.OnRayReleaseOutside();

            bool validate = false;

            if (grippedUnderThreshold)
            {
                validate = true;

                onReleaseEvent.Invoke();
                Checked = !Checked;
                onCheckEvent.Invoke(Checked);
            }

            Hovered = false;
            Pushed = false;
            ResetColor();

            grippedUnderThreshold = false;

            return validate;
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
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }


            Vector2 project = new Vector3(localWidgetPosition.x, localWidgetPosition.y);
            if (triggerJustClicked)
            {
                grippedPos = project;
            }

            if (Vector2.Distance(project, grippedPos) < 3.0f * height)
            {
                grippedUnderThreshold = true;
                localProjectedWidgetPosition.x = grippedPos.x;
                localProjectedWidgetPosition.y = grippedPos.y;
            }
            else
            {
                grippedUnderThreshold = false;
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        #endregion

        #region create

        public class CreateParams
        {
            public Transform parent = null;
            public string widgetName = UICheckbox.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UICheckbox.default_thickness);
            public float width = UICheckbox.default_width;
            public float height = UICheckbox.default_height;
            public float margin = UICheckbox.default_margin;
            public float thickness = UICheckbox.default_thickness;
            public Material material = UIUtils.LoadMaterial(UICheckbox.default_material_name);
            public ColorVar color = UIOptions.BackgroundColorVar;
            public ColorVar textColor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public string caption = UICheckbox.default_text;
            public CheckboxContent content = default_content;
            public Sprite checkedIcon = UIUtils.LoadIcon(UICheckbox.default_checked_icon_name);
            public Sprite uncheckedIcon = UIUtils.LoadIcon(UICheckbox.default_unchecked_icon_name);
        }


        public static UICheckbox Create(CreateParams input)
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

            UICheckbox uiCheckbox = go.AddComponent<UICheckbox>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiCheckbox.relativeLocation = input.relativeLocation;
            uiCheckbox.transform.parent = input.parent;
            uiCheckbox.transform.localPosition = parentAnchor + input.relativeLocation;
            uiCheckbox.transform.localRotation = Quaternion.identity;
            uiCheckbox.transform.localScale = Vector3.one;
            uiCheckbox.width = input.width;
            uiCheckbox.height = input.height;
            uiCheckbox.margin = input.margin;
            uiCheckbox.thickness = input.thickness;
            uiCheckbox.content = input.content;
            uiCheckbox.checkedSprite = input.checkedIcon;
            uiCheckbox.uncheckedSprite = input.uncheckedIcon;
            uiCheckbox.textContent = input.caption;
            uiCheckbox.source_material = input.material;
            uiCheckbox.baseColor.useConstant = false;
            uiCheckbox.baseColor.reference = input.color;
            uiCheckbox.textColor.useConstant = false;
            uiCheckbox.textColor.reference = input.textColor;
            uiCheckbox.pushedColor.useConstant = false;
            uiCheckbox.pushedColor.reference = input.pushedColor;
            uiCheckbox.selectedColor.useConstant = false;
            uiCheckbox.selectedColor.reference = input.selectedColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiCheckbox.Anchor = Vector3.zero;
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
            if (meshRenderer != null && uiCheckbox.source_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(uiCheckbox.source_material);
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiCheckbox.SetColor(input.color.value);
            }

            // Add a Canvas
            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiCheckbox.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 1;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiCheckbox.width, uiCheckbox.height);
            rt.localPosition = Vector3.zero;

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            float minSide = Mathf.Min(uiCheckbox.width, uiCheckbox.height);

            // Add an Image under the Canvas
            if (input.uncheckedIcon != null && input.checkedIcon != null)
            {
                GameObject image = new GameObject("Image");
                image.transform.parent = canvas.transform;

                Image img = image.AddComponent<Image>();
                img.sprite = input.uncheckedIcon;
                img.color = input.textColor.value;

                RectTransform trt = image.GetComponent<RectTransform>();
                trt.localScale = Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                // TODO: non square icons ratio...
                trt.sizeDelta = new Vector2(minSide - 2.0f * input.margin, minSide - 2.0f * input.margin);
                trt.localPosition = new Vector3(input.margin, -input.margin, -0.001f);
            }

            // Add a Text under the Canvas
            if (input.content == CheckboxContent.CheckboxAndText)
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
                t.text = input.caption;
                t.enableAutoSizing = false;
                t.fontSize = 18;
                t.fontSizeMin = 1;
                t.fontSizeMin = 500;
                t.fontStyle = FontStyles.Normal;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.color = input.textColor.value;
                
                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left

                trt.sizeDelta = new Vector2((input.width - minSide - input.margin) * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                trt.localPosition = new Vector3(minSide, -input.margin, -0.002f);
            }

            UIUtils.SetRecursiveLayer(go, "CameraHidden");

            return uiCheckbox;
        }
        #endregion
    }
}
