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
    public class UICheckbox : UIElement
    {
        [SpaceHeader("Checkbox Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        public float margin = 0.005f;
        public float thickness = 0.001f;
        public Color pushedColor = new Color(0.5f, 0.5f, 0.5f);
        public Sprite checkedSprite = null;
        public Sprite uncheckedSprite = null;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;
        public UnityEvent onCheckEvent = null;
        public UnityEvent onUncheckEvent = null;

        private bool isChecked = false;
        public bool Checked { get { return isChecked; } set { isChecked = value; UpdateCheckIcon(); } }

        private bool needRebuild = false;

        public string Text { get { return GetText(); } set { SetText(value); } }

        void Start()
        {
            if (EditorApplication.isPlaying || Application.isPlaying)
            {
                onClickEvent.AddListener(OnPushCheckbox);
                onReleaseEvent.AddListener(OnReleaseCheckbox);
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

                Text text = canvas.gameObject.GetComponentInChildren<Text>();
                if (text != null)
                {
                    RectTransform textRT = text.gameObject.GetComponent<RectTransform>();
                    textRT.sizeDelta = new Vector2(width, height);
                }

                // TODO: image
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
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                SetColor(baseColor);
                needRebuild = false;
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
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
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
            // TODO: pass the Cursor to the checkbox, test the object instead of a hardcoded name.
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

        public void OnPushCheckbox()
        {
            SetColor(pushedColor);
            Checked = !Checked;
            if (Checked)
            {
                onCheckEvent.Invoke();
            }
            else
            {
                onUncheckEvent.Invoke();
            }
        }

        public void OnReleaseCheckbox()
        {
            SetColor(baseColor);
        }


        public static void CreateUICheckbox(
            string checkboxName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            float thickness,
            Material material,
            Color color,
            string caption,
            Sprite checkedIcon,
            Sprite uncheckedIcon)
        {
            GameObject go = new GameObject(checkboxName);
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

            UICheckbox uiCheckbox = go.AddComponent<UICheckbox>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiCheckbox.relativeLocation = relativeLocation;
            uiCheckbox.transform.parent = parent;
            uiCheckbox.transform.localPosition = parentAnchor + relativeLocation;
            uiCheckbox.transform.localRotation = Quaternion.identity;
            uiCheckbox.transform.localScale = Vector3.one;
            uiCheckbox.width = width;
            uiCheckbox.height = height;
            uiCheckbox.margin = margin;
            uiCheckbox.thickness = thickness;
            uiCheckbox.checkedSprite = checkedIcon;
            uiCheckbox.uncheckedSprite = uncheckedIcon;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
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

                uiCheckbox.BaseColor = color;
            }

            // Add a Canvas
            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiCheckbox.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

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

            //canvas.AddComponent<GraphicRaycaster>(); // not sure it is mandatory, try without.

            float minSide = Mathf.Min(uiCheckbox.width, uiCheckbox.height);

            // Add an Image under the Canvas
            if (uncheckedIcon != null && checkedIcon != null)
            {
                GameObject image = new GameObject("Image");
                image.transform.parent = canvas.transform;

                Image img = image.AddComponent<Image>();
                img.sprite = uncheckedIcon;

                RectTransform trt = image.GetComponent<RectTransform>();
                trt.localScale = Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0, 1);
                trt.anchorMax = new Vector2(0, 1);
                trt.pivot = new Vector2(0, 1); // top left
                // TODO: non square icons ratio...
                trt.sizeDelta = new Vector2(minSide - 2.0f * margin, minSide - 2.0f * margin);
                trt.localPosition = new Vector3(margin, -margin, -0.001f);
            }

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
                trt.sizeDelta = new Vector2(uiCheckbox.width, uiCheckbox.height);
                float textPosLeft = uncheckedIcon != null ? minSide : 0.0f;
                trt.localPosition = new Vector3(textPosLeft, -uiCheckbox.height / 2.0f, -0.002f);
            }
        }
    }
}
