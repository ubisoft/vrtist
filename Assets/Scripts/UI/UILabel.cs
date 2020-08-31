using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UILabel : UIElement
    {
        public enum IconMarginBehavior { UseWidgetMargin, UseIconMargin };
        public enum LabelContent { TextOnly, ImageOnly, TextAndImage };
        public enum ImagePosition { Left, Right };

        // TODO: put in a scriptable object
        public static readonly string default_widget_name = "New Label";
        public static readonly float default_width = 0.15f;
        public static readonly float default_height = 0.05f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        //public static readonly Color default_label_background_color = UIElement.default_background_color;
        //public static readonly Color default_label_foreground_color = UIElement.default_foreground_color;
        public static readonly string default_text = "Label";
        public static readonly string default_material_name = "UIElementTransparent";
        public static readonly LabelContent default_content = LabelContent.TextOnly;
        public static readonly string default_icon_name = "paint";
        public static readonly ImagePosition default_image_position = ImagePosition.Left;
        public static readonly IconMarginBehavior default_icon_margin_behavior = IconMarginBehavior.UseWidgetMargin;
        public static readonly float default_icon_margin = 0.0f;

        [SpaceHeader("Label Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public LabelContent content = default_content;
        public ImagePosition imagePosition = default_image_position;
        public IconMarginBehavior iconMarginBehavior = default_icon_margin_behavior;
        [CentimeterFloat] public float iconMargin = default_icon_margin;
        public Material source_material = null;
        public Sprite image = null;
        [TextArea] public string textContent = "";

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onHoverEvent = new UnityEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        public string Text { get { return textContent; } set { SetText(value); } }

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UILabel_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
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
                sharedMaterialInstance.name = "UILabel_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        private void UpdateCanvasDimensions()
        {
            Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 1;

                RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(width, height);

                // TEXT
                TextMeshPro text = canvas.gameObject.GetComponentInChildren<TextMeshPro>(true);
                if (text != null)
                {
                    text.text = Text;
                    text.color = TextColor;
                    RectTransform rt = text.gameObject.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = new Vector2((width - 2.0f * margin) * 100.0f, (height - 2.0f * margin) * 100.0f);
                        rt.localPosition = new Vector3(margin, -margin, -0.002f);
                    }
                }
            }
        }

        public void UpdateTextColor()
        {
            TextMeshPro text = GetComponentInChildren<TextMeshPro>();
            if (text != null)
            {
                text.color = TextColor;
                // TODO: test to see if we need to go and change the _BaseColor of the material of the text object.
            }
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const int min_nbSubdivCornerFixed = 1;
            const int min_nbSubdivCornerPerUnit = 1;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
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
#if UNITY_EDITOR
            if (NeedsRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                ResetColor();
                UpdateTextColor();
                NeedsRebuild = false;
            }
#endif
        }

        public override void ResetColor()
        {
            SetColor(Disabled ? DisabledColor
                   : BaseColor);
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

            TextMeshPro text = GetComponentInChildren<TextMeshPro>();
            if (text != null)
            {
                text.text = textValue;
            }
        }

        #region ray

        public override void OnRayEnter()
        {
            base.OnRayEnter();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();
        }

        public override void OnRayHover()
        {
            base.OnRayHover();
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
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();
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

        public override void OnRayReleaseOutside()
        {
            base.OnRayReleaseOutside();
        }

        #endregion

        #region create

        public class CreateLabelParams
        {
            public Transform parent = null;
            public string widgetName = UILabel.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UILabel.default_thickness);
            public float width = UILabel.default_width;
            public float height = UILabel.default_height;
            public float margin = UILabel.default_margin;
            public float thickness = UILabel.default_thickness;
            public Material material = UIUtils.LoadMaterial(UILabel.default_material_name);
            public ColorVar bgcolor = UIOptions.BackgroundColorVar;
            public ColorVar fgcolor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public LabelContent labelContent = UILabel.default_content;
            public ImagePosition imagePosition = UILabel.default_image_position;
            public IconMarginBehavior iconMarginBehavior = UILabel.default_icon_margin_behavior;
            public float iconMargin = UILabel.default_icon_margin;
            public string caption = UILabel.default_text;
            public Sprite icon = UIUtils.LoadIcon(UILabel.default_icon_name);
        }

        public static UILabel Create(CreateLabelParams input)
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

            UILabel uiLabel = go.AddComponent<UILabel>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiLabel.relativeLocation = input.relativeLocation;
            uiLabel.transform.parent = input.parent;
            uiLabel.transform.localPosition = parentAnchor + input.relativeLocation;
            uiLabel.transform.localRotation = Quaternion.identity;
            uiLabel.transform.localScale = Vector3.one;
            uiLabel.width = input.width;
            uiLabel.height = input.height;
            uiLabel.margin = input.margin;
            uiLabel.thickness = input.thickness;
            uiLabel.content = input.labelContent;
            uiLabel.image = input.icon;
            uiLabel.textContent = input.caption;
            uiLabel.content = input.labelContent;
            uiLabel.imagePosition = input.imagePosition;
            uiLabel.iconMarginBehavior = input.iconMarginBehavior;
            uiLabel.iconMargin = input.iconMargin;
            uiLabel.source_material = input.material;
            uiLabel.baseColor.useConstant = false;
            uiLabel.baseColor.reference = input.bgcolor;
            uiLabel.textColor.useConstant = false;
            uiLabel.textColor.reference = input.fgcolor;
            uiLabel.pushedColor.useConstant = false;
            uiLabel.pushedColor.reference = input.pushedColor;
            uiLabel.selectedColor.useConstant = false;
            uiLabel.selectedColor.reference = input.selectedColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiLabel.Anchor = Vector3.zero;
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
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.rendererPriority = 1;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiLabel.SetColor(input.bgcolor.value);
            }

            // Add a Canvas
            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiLabel.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 1;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiLabel.width, uiLabel.height);
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
                t.renderer.sortingOrder = 1;
                t.fontStyle = FontStyles.Normal;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.color = input.fgcolor.value;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.sizeDelta = new Vector2((uiLabel.width - 2.0f * input.margin ) * 100.0f, (uiLabel.height - 2.0f * input.margin ) * 100.0f);
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.localPosition = new Vector3(input.margin, -input.margin, -0.002f); // centered, on-top
            }

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiLabel;
        }

        #endregion
    }
}
