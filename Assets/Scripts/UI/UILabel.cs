using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
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
        [SpaceHeader("Label Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = 0.005f;
        public Color textColor = UIElement.default_color; // TODO: put in UIElement (foreground color and background color)

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        static public float labelThickness = 0.001f; // TODO: change mesh to have only one face, no depth

        private bool needRebuild = false;

        // TODO: expose a text field to avoid having to go 2 levels deep into the Text object

        public string Text { get { return GetText(); } set { SetText(value); } }
        public Color TextColor { get { return textColor; } set { textColor = value; UpdateTextColor(); } } // TODO: move into UIElement

        void Start()
        {
        }

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, labelThickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
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
            Text text = GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = textValue;
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            // TODO: pass the Cursor to the label, test the object instead of a hardcoded name.
            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                //VRInput.SendHaptic(VRInput.rightController, 0.03f, 1.0f);
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                onHoverEvent.Invoke();
            }
        }

        public static void CreateUILabel(
            string labelName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            Material material,
            Color bgcolor,
            Color fgcolor,
            string caption)
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

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, labelThickness);
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
                // TODO: see if we need to Instantiate(uiMaterial), or modify the instance created when calling meshRenderer.material
                //       to make the error disappear;

                // Get an instance of the same material
                // NOTE: sends an warning about leaking instances, because meshRenderer.material create instances while we are in EditorMode.
                //meshRenderer.sharedMaterial = uiMaterial;
                //Material material = meshRenderer.material; // instance of the sharedMaterial

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
                //t.font = (Font)Resources.Load("MyLocalFont");
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
        }
    }
}
