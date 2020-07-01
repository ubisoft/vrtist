using UnityEditor;
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
    public class UIButton : UIElement
    {
        public enum IconMarginBehavior { UseButtonMargin, UseIconMargin };
        public enum ButtonContent { TextOnly, ImageOnly, TextAndImage };

        // TODO: put in a scriptable object
        public static readonly string default_widget_name = "New Button";
        public static readonly float default_width = 0.15f;
        public static readonly float default_height = 0.05f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly string default_material_name = "UIBase";
        public static readonly Color default_color = UIElement.default_background_color;
        public static readonly string default_text = "Button";
        public static readonly string default_icon_name = "paint";
        public static readonly ButtonContent default_content = ButtonContent.TextAndImage;
        public static readonly IconMarginBehavior default_icon_margin_behavior = IconMarginBehavior.UseButtonMargin;
        public static readonly float default_icon_margin = 0.0f;

        [SpaceHeader("Button Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public IconMarginBehavior iconMarginBehavior = default_icon_margin_behavior;
        [CentimeterFloat] public float iconMargin = default_icon_margin;
        public ButtonContent content = default_content;
        public Material source_material = null;
        public Color pushedColor = UIElement.default_pushed_color;
        public Color checkedColor = UIElement.default_pushed_color;
        public Sprite baseSprite = null;
        public Sprite checkedSprite = null;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();
        public bool isCheckable = false;
        public BoolChangedEvent onCheckEvent = new BoolChangedEvent();
        public UnityEvent onHoverEvent = new UnityEvent();

        private bool needRebuild = false;

        public string Text { get { return GetText(); } set { SetText(value); } }

        private bool isChecked = false;

        public bool Checked
        {
            get { return isChecked; }
            set { isChecked = value; SetColor(Disabled ? DisabledColor : (value ? checkedColor : BaseColor)); UpdateCheckIcon(); }
        }

        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                onClickEvent.AddListener(OnPushButton);
                onReleaseEvent.AddListener(OnReleaseButton);
            }
        }

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UIButton_GeneratedMesh";
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

        private void UpdateCheckIcon()
        {
            Image img = gameObject.GetComponentInChildren<Image>();
            if (img != null && gameObject.activeSelf && isCheckable)
            {
                img.sprite = isChecked ? checkedSprite : baseSprite;
                img.enabled = (null != img.sprite);
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
                    RectTransform rt = image.gameObject.GetComponent<RectTransform>();
                    if (rt)
                    {
                        if (iconMarginBehavior == IconMarginBehavior.UseButtonMargin)
                        {
                            rt.sizeDelta = new Vector2(minSide - 2.0f * margin, minSide - 2.0f * margin);
                            rt.localPosition = new Vector3(margin, -margin, -0.001f);
                        }
                        else // IconMarginBehavior.UseIconMargin for the moment
                        {
                            rt.sizeDelta = new Vector2(minSide - 2.0f * iconMargin, minSide - 2.0f * iconMargin);
                            rt.localPosition = new Vector3(iconMargin, -iconMargin, -0.001f);
                        }
                    }
                }

                // TEXT
                Text text = canvas.gameObject.GetComponentInChildren<Text>();
                if (text != null)
                {
                    RectTransform rt = text.gameObject.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = new Vector2(width * 100.0f, height * 100.0f);
                        bool noImage = (image == null) || !image.gameObject.activeSelf;
                        float textPosLeft = noImage ? margin : minSide;
                        rt.localPosition = new Vector3(textPosLeft, 0.0f, -0.002f);
                    }
                }
            }
        }

        public void ActivateText(bool doActivate)
        {
            Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                Text text = canvas.gameObject.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.gameObject.SetActive(doActivate);
                }
            }
        }

        public void ActivateIcon(bool doActivate)
        {
            Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                Image image = canvas.gameObject.GetComponentInChildren<Image>(true);
                if (image != null)
                {
                    image.gameObject.SetActive(doActivate);
                }
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

            needRebuild = true;
        }

        private void Update()
        {
#if UNITY_EDITOR
            // NOTE: rebuild when touching a property in the inspector.
            // Boolean needRebuild is set in OnValidate();
            // The rebuild method called when using the gizmos is: Width and Height
            // properties in UIElement.
            // This comment is probably already obsolete.
            if (needRebuild)
            {
                // NOTE: I do all these things because properties can't be called from the inspector.
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                SetColor(Disabled ? disabledColor.Value : baseColor.Value);
                needRebuild = false;
            }
#endif
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

        public override void ResetMaterial()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = BaseColor;
                //if (meshRenderer.sharedMaterial != null)
                //{
                //    prevColor = GetColor();
                //}

                Material materialInstance = Instantiate(source_material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIButton_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
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

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            float currentTime = Time.unscaledTime;
            if ((currentTime - prevTime) > 0.4f && otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                prevTime = currentTime;
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();

                if (isCheckable)
                {
                    Checked = !Checked;
                    onCheckEvent.Invoke(Checked);
                }
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionStay())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onHoverEvent.Invoke();
            }
        }

        public void OnPushButton()
        {
            SetColor(Disabled ? DisabledColor : pushedColor);
        }

        public void OnReleaseButton()
        {
            SetColor(Disabled ? disabledColor.Value : (isChecked ? checkedColor : BaseColor));
        }

        //
        // CREATE
        //

        public class CreateButtonParams
        {
            public Transform parent = null;
            public string widgetName = UIButton.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIButton.default_thickness);
            public float width = UIButton.default_width;
            public float height = UIButton.default_height;
            public float margin = UIButton.default_margin;
            public float thickness = UIButton.default_thickness;
            public Material material = UIUtils.LoadMaterial(UIButton.default_material_name);
            public ColorVariable color = UIOptions.Instance.backgroundColor;
            public ButtonContent buttonContent = UIButton.default_content;
            public IconMarginBehavior iconMarginBehavior = UIButton.default_icon_margin_behavior;
            public float iconMargin = UIButton.default_icon_margin;
            public string caption = UIButton.default_text;
            public Sprite icon = UIUtils.LoadIcon(UIButton.default_icon_name);
        }

        public static UIButton Create(CreateButtonParams input)
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

            UIButton uiButton = go.AddComponent<UIButton>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiButton.relativeLocation = input.relativeLocation;
            uiButton.transform.parent = input.parent;
            uiButton.transform.localPosition = parentAnchor + input.relativeLocation;
            uiButton.transform.localRotation = Quaternion.identity;
            uiButton.transform.localScale = Vector3.one;
            uiButton.width = input.width;
            uiButton.height = input.height;
            uiButton.margin = input.margin;
            uiButton.thickness = input.thickness;
            uiButton.content = input.buttonContent;
            uiButton.iconMarginBehavior = input.iconMarginBehavior;
            uiButton.iconMargin = input.iconMargin;
            uiButton.source_material = input.material;
            uiButton.baseColor.useConstant = false;
            uiButton.baseColor.reference = input.color;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiButton.Anchor = Vector3.zero;
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
            if (meshRenderer != null && uiButton.source_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(uiButton.source_material);
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiButton.SetColor(input.color.value);
            }

            // Add a Canvas
            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiButton.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiButton.width, uiButton.height);
            rt.localPosition = Vector3.zero;

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            float minSide = Mathf.Min(uiButton.width, uiButton.height);

            // Add an Image under the Canvas
            if (input.buttonContent != ButtonContent.TextOnly)
            {
                GameObject image = new GameObject("Image");
                image.transform.parent = canvas.transform;

                Image img = image.AddComponent<Image>();
                img.sprite = input.icon;

                RectTransform trt = image.GetComponent<RectTransform>();
                trt.localScale = Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                // TODO: non square icons ratio...
                if (uiButton.iconMarginBehavior == IconMarginBehavior.UseButtonMargin)
                {
                    trt.sizeDelta = new Vector2(minSide - 2.0f * input.margin, minSide - 2.0f * input.margin);
                    trt.localPosition = new Vector3(input.margin, -input.margin, -0.001f);
                }
                else // IconMarginBehavior.UseIconMargin for the moment
                {
                    trt.sizeDelta = new Vector2(minSide - 2.0f * uiButton.iconMargin, minSide - 2.0f * uiButton.iconMargin);
                    trt.localPosition = new Vector3(uiButton.iconMargin, -uiButton.iconMargin, -0.001f);
                }
            }

            // Add a Text under the Canvas
            if (input.buttonContent != ButtonContent.ImageOnly)
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                t.text = input.caption;
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.sizeDelta = new Vector2(uiButton.width * 100.0f, uiButton.height * 100.0f);
                float textPosLeft = input.buttonContent == ButtonContent.TextAndImage ? minSide : input.margin;
                trt.localPosition = new Vector3(textPosLeft, 0.0f, -0.002f);
            }

            return uiButton;
        }
    }
}
