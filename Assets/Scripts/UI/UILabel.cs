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
    public class UILabel : UIElement
    {
        public static readonly string default_label_name = "New Label";
        public static readonly float default_width = 0.15f;
        public static readonly float default_height = 0.05f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly Color default_label_background_color = UIElement.default_background_color;
        public static readonly Color default_label_foreground_color = UIElement.default_foreground_color;
        public static readonly string default_text = "Label";
        public static readonly string default_material_name = "UIElementTransparent";
        public static readonly LabelContent default_content = LabelContent.TextOnly;
        public static readonly string default_icon_name = "paint";

        public enum IconMarginBehavior { UseWidgetMargin, UseIconMargin };
        public enum LabelContent { TextOnly, ImageOnly, TextAndImage };
        public enum ImagePosition { Left, Right };

        [SpaceHeader("Label Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = 0.005f;
        [CentimeterFloat] public float thickness = 0.001f;
        public LabelContent content = LabelContent.TextOnly;
        public ImagePosition imagePosition = ImagePosition.Left;
        public IconMarginBehavior iconMarginBehavior = IconMarginBehavior.UseWidgetMargin;
        [CentimeterFloat] public float iconMargin = 0.0f;
        public Sprite image = null;
        [TextArea] public string textContent = "";
        public Color textColor = UILabel.default_label_foreground_color;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onHoverEvent = new UnityEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private bool needRebuild = false;

        public string Text { get { return GetText(); } set { SetText(value); } }
        public Color TextColor { get { return textColor; } set { textColor = value; UpdateTextColor(); } }

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

                Material material = UIUtils.LoadMaterial("UIElementTransparent");
                Material materialInstance = Instantiate(material);

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
                Text text = canvas.gameObject.GetComponentInChildren<Text>();
                if(text != null)
                {
                    // tmp: pour eviter que tous les labels se retrouvent vides de text.
                    if (textContent.Length == 0 && text.text.Length > 0)
                        textContent = text.text;
                    //------------------


                    text.text = textContent;
                    text.color = textColor;
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
            Text text = GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = textColor;
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

            needRebuild = true;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (needRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                if(!EditorApplication.isPlaying)
                    SetColor(Disabled ? disabledColor : baseColor);
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
            textContent = textValue;

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

            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
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

        public static UILabel Create(Transform parent)
        {
            return UILabel.CreateEx(
                parent, 
                default_label_name,
                new Vector3(0, 0, -default_thickness), 
                default_width, 
                default_height, 
                default_margin, 
                default_thickness,
                UIUtils.LoadMaterial(default_material_name), 
                default_label_background_color, 
                default_label_foreground_color, 
                default_content,
                default_text,
                UIUtils.LoadIcon(default_icon_name)
            );
        }

        public static UILabel CreateEx(
            Transform parent,
            string labelName,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            float thickness,
            Material material,
            Color bgcolor,
            Color fgcolor,
            LabelContent labelContent,
            string caption,
            Sprite icon)
        {
            GameObject go = new GameObject(labelName);
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

            UILabel uiLabel = go.AddComponent<UILabel>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiLabel.relativeLocation = relativeLocation;
            uiLabel.transform.parent = parent;
            uiLabel.transform.localPosition = parentAnchor + relativeLocation;
            uiLabel.transform.localRotation = Quaternion.identity;
            uiLabel.transform.localScale = Vector3.one;
            uiLabel.width = width;
            uiLabel.height = height;
            uiLabel.margin = margin;
            uiLabel.thickness = thickness;
            uiLabel.content = labelContent;
            uiLabel.image = icon;
            uiLabel.textContent = caption;
            // text color and bg color are set below

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
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
            if (meshRenderer != null && material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(material);
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiLabel.BaseColor = bgcolor;
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
            if (caption.Length > 0)
            {
                GameObject text = new GameObject("Text");
                text.transform.parent = canvas.transform;

                Text t = text.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                t.text = caption;
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.UpperLeft;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
                t.color = fgcolor;

                RectTransform trt = t.GetComponent<RectTransform>();
                trt.localScale = 0.01f * Vector3.one;
                trt.sizeDelta = new Vector2((uiLabel.width - 2.0f * margin ) * 100.0f, (uiLabel.height - 2.0f * margin ) * 100.0f);
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                trt.localPosition = new Vector3(margin, -margin, -0.002f); // centered, on-top

                uiLabel.TextColor = fgcolor;
            }

            return uiLabel;
        }
    }
}
