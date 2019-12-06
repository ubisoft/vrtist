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
    [RequireComponent(typeof(BoxCollider))]
    public class UI3DObject : UIElement
    {
        [SpaceHeader("3DObject Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        public float margin = 0.05f;
        public float thickness = 0.05f;
        public float depth = 0.05f;
        public GameObject objectPrefab = null;
        private GameObject instantiatedObject = null; // TODO: get this object in Start or Awake

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        private bool needRebuild = false;

        public float Depth { get { return depth; } set { depth = value; RebuildMesh(); } }
        
        void Start()
        {
            if (EditorApplication.isPlaying || Application.isPlaying)
            {
                onClickEvent.AddListener(OnPush3DObject);
                onReleaseEvent.AddListener(OnRelease3DObject);
            }
        }

        public override void RebuildMesh()
        {
            // TODO: scale objectPrefab to fit the new size.
            InstantiateChildObject();

            UpdateColliderDimensions();

            // TODO: do we want some text under the 3d object? or tooltip?
            UpdateCanvasDimensions();
        }

        private void UpdateColliderDimensions()
        {
            //MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            //BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            //if (meshFilter != null && coll != null)
            //{
            //    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
            //    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
            //    if (initColliderSize.z < UIElement.collider_min_depth_shallow)
            //    {
            //        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
            //        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
            //    }
            //    else
            //    {
            //        coll.center = initColliderCenter;
            //        coll.size = initColliderSize;
            //    }
            //}
        }

        private void UpdateCanvasDimensions()
        {
            //Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            //if (canvas != null)
            //{
            //    RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
            //    canvasRT.sizeDelta = new Vector2(width, height);

            //    float minSide = Mathf.Min(width, height);

            //    // IMAGE
            //    Image image = canvas.GetComponentInChildren<Image>();
            //    if (image != null)
            //    {
            //        RectTransform rt = image.gameObject.GetComponent<RectTransform>();
            //        if (rt)
            //        {
            //            rt.sizeDelta = new Vector2(minSide - 2.0f * margin, minSide - 2.0f * margin);
            //            rt.localPosition = new Vector3(margin, -margin, -0.001f);
            //        }
            //    }

            //    // TEXT
            //    Text text = canvas.gameObject.GetComponentInChildren<Text>();
            //    if (text != null)
            //    {
            //        RectTransform rt = text.gameObject.GetComponent<RectTransform>();
            //        if (rt != null)
            //        {
            //            rt.sizeDelta = new Vector2(width, height);
            //            float textPosLeft = image != null ? minSide : 0.0f;
            //            rt.localPosition = new Vector3(textPosLeft, -height / 2.0f, -0.002f);
            //        }
            //    }
            //}
        }

        private void InstantiateChildObject()
        {
            // TODO: do better, dont destroy and re-instantiate for every interaction.

            if(instantiatedObject != null)
            {
                DestroyImmediate(instantiatedObject);
            }

            GameObject child = Instantiate(objectPrefab, transform);
            Vector3 middle = new Vector3(width / 2.0f, -height / 2.0f, -depth / 2.0f);
            child.transform.localPosition = middle;
            child.transform.localRotation = Quaternion.identity;
            Vector3 childExtents = child.GetComponentInChildren<MeshFilter>().mesh.bounds.extents;
            float maxChildDim = Mathf.Max(new float[] { childExtents.x, childExtents.y, childExtents.z });
            float minDim = Mathf.Min(new float[] { width / 2.0f, height / 2.0f, depth / 2.0f });
            float ratio = minDim / maxChildDim;
            child.transform.localScale = new Vector3(ratio, ratio, ratio);

            instantiatedObject = child;
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_depth = 0.01f;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (depth < min_depth)
                depth = min_depth;

            needRebuild = true;
        }

        private void Update()
        {
            if (needRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 worldCenter = transform.TransformPoint(new Vector3(width / 2.0f, -height / 2.0f, -depth / 2.0f));
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(worldCenter, new Vector3(width, height, depth));
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
        }

        //private string GetText()
        //{
        //    Text text = GetComponentInChildren<Text>();
        //    if (text != null)
        //    {
        //        return text.text;
        //    }

        //    return null;
        //}

        //private void SetText(string textValue)
        //{
        //    Text text = GetComponentInChildren<Text>();
        //    if (text != null)
        //    {
        //        text.text = textValue;
        //    }
        //}

        private void OnTriggerEnter(Collider otherCollider)
        {
            // TODO: pass the Cursor to the object3d, test the object instead of a hardcoded name.
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
                // RE-instantiate an object from the prefab, the other one being in the user's hands.
                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                // TODO: rotate objectPrefab ???

                onHoverEvent.Invoke();
            }
        }

        public void OnPush3DObject()
        {
            // TODO: animate object, snap cursor to it?

            //SetColor(pushedColor);
        }

        public void OnRelease3DObject()
        {
            //SetColor(baseColor);
        }


        public static void CreateUI3DObject(
            string object3dName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float depth,
            GameObject prefab)
        {
            GameObject go = new GameObject(object3dName);
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

            UI3DObject ui3DObject = go.AddComponent<UI3DObject>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            ui3DObject.relativeLocation = relativeLocation;
            ui3DObject.transform.parent = parent;
            ui3DObject.transform.localPosition = parentAnchor + relativeLocation;
            ui3DObject.transform.localRotation = Quaternion.identity;
            ui3DObject.transform.localScale = Vector3.one;
            ui3DObject.width = width;
            ui3DObject.height = height;
            ui3DObject.depth = depth;
            ui3DObject.objectPrefab = prefab;

            ui3DObject.Anchor = Vector3.zero;

            // Child prefab
            GameObject child = Instantiate(prefab, ui3DObject.transform);
            Vector3 middle = new Vector3(width / 2.0f, -height / 2.0f, -depth / 2.0f);
            child.transform.localPosition = middle;
            child.transform.localRotation = Quaternion.identity;
            float childObjectRadius = child.GetComponentInChildren<MeshFilter>().mesh.bounds.extents.magnitude;
            float ratio = middle.magnitude / childObjectRadius;
            child.transform.localScale = new Vector3(ratio, ratio, ratio);

            ui3DObject.instantiatedObject = child;

            //BoxCollider coll = go.GetComponent<BoxCollider>();
            //if (coll != null)
            //{
            //    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
            //    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
            //    if (initColliderSize.z < UIElement.collider_min_depth_shallow)
            //    {
            //        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
            //        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
            //    }
            //    else
            //    {
            //        coll.center = initColliderCenter;
            //        coll.size = initColliderSize;
            //    }
            //    coll.isTrigger = true;
            //}

            // Add a Canvas
            //GameObject canvas = new GameObject("Canvas");
            //canvas.transform.parent = ui3DObject.transform;

            //Canvas c = canvas.AddComponent<Canvas>();
            //c.renderMode = RenderMode.WorldSpace;

            //RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            //rt.localScale = Vector3.one;
            //rt.localRotation = Quaternion.identity;
            //rt.anchorMin = new Vector2(0, 1);
            //rt.anchorMax = new Vector2(0, 1);
            //rt.pivot = new Vector2(0, 1); // top left
            //rt.sizeDelta = new Vector2(ui3DObject.width, ui3DObject.height);
            //rt.localPosition = Vector3.zero;

            //CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            //cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            //cs.referencePixelsPerUnit = 100; // default?

            ////canvas.AddComponent<GraphicRaycaster>(); // not sure it is mandatory, try without.

            //float minSide = Mathf.Min(ui3DObject.width, ui3DObject.height);

            //// Add an Image under the Canvas
            //if (icon != null)
            //{
            //    GameObject image = new GameObject("Image");
            //    image.transform.parent = canvas.transform;

            //    Image img = image.AddComponent<Image>();
            //    img.sprite = icon;

            //    RectTransform trt = image.GetComponent<RectTransform>();
            //    trt.localScale = Vector3.one;
            //    trt.localRotation = Quaternion.identity;
            //    trt.anchorMin = new Vector2(0, 1);
            //    trt.anchorMax = new Vector2(0, 1);
            //    trt.pivot = new Vector2(0, 1); // top left
            //    // TODO: non square icons ratio...
            //    trt.sizeDelta = new Vector2(minSide - 2.0f * margin, minSide - 2.0f * margin);
            //    trt.localPosition = new Vector3(margin, -margin, -0.001f);
            //}

            //// Add a Text under the Canvas
            //if (caption.Length > 0)
            //{
            //    GameObject text = new GameObject("Text");
            //    text.transform.parent = canvas.transform;

            //    Text t = text.AddComponent<Text>();
            //    //t.font = (Font)Resources.Load("MyLocalFont");
            //    t.text = caption;
            //    t.fontSize = 32;
            //    t.fontStyle = FontStyle.Bold;
            //    t.alignment = TextAnchor.MiddleLeft;
            //    t.horizontalOverflow = HorizontalWrapMode.Overflow;
            //    t.verticalOverflow = VerticalWrapMode.Overflow;

            //    RectTransform trt = t.GetComponent<RectTransform>();
            //    trt.localScale = 0.01f * Vector3.one;
            //    trt.localRotation = Quaternion.identity;
            //    trt.anchorMin = new Vector2(0, 1);
            //    trt.anchorMax = new Vector2(0, 1);
            //    trt.pivot = new Vector2(0, 1); // top left
            //    trt.sizeDelta = new Vector2(ui3DObject.width, ui3DObject.height);
            //    float textPosLeft = icon != null ? minSide : 0.0f;
            //    trt.localPosition = new Vector3(textPosLeft, -ui3DObject.height / 2.0f, -0.002f);
            //}
        }
    }
}
