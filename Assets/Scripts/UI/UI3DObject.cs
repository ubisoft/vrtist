﻿using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(SphereCollider))]
    public class UI3DObject : UIElement
    {
        [SpaceHeader("3DObject Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = 0.05f;
        [CentimeterFloat] public float thickness = 0.05f;
        [CentimeterFloat] public float depth = 0.05f;
        public GameObject objectPrefab = null;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public GameObjectHashChangedEvent onEnterUI3DObject = null;
        public GameObjectHashChangedEvent onExitUI3DObject = null;
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        public float Depth { get { return depth; } set { depth = value; RebuildMesh(); } }
        
        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                ToolsUIManager.Instance.RegisterUI3DObject(gameObject);
                onClickEvent.AddListener(OnPush3DObject);
                onReleaseEvent.AddListener(OnRelease3DObject);
            }
        }

        public override void RebuildMesh()
        {
            InstantiateChildObject();
        }

        private void InstantiateChildObject()
        {
            // TODO: do better, dont destroy and re-instantiate for every interaction.

            // Removes all children (to circumvent a bug which spawns too many instances)
            for(int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            GameObject child = Instantiate(objectPrefab, transform);
            Vector3 middle = new Vector3(width / 2.0f, -height / 2.0f, -depth / 2.0f);
            child.transform.localPosition = middle;
            child.transform.localRotation = Quaternion.identity;
            // TODO: problem, some object are multi-mesh... compute the bbox.
            Vector3 childExtents = child.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents;
            float maxChildDim = Mathf.Max(new float[] { childExtents.x, childExtents.y, childExtents.z });
            float minDim = Mathf.Min(new float[] { width / 2.0f, height / 2.0f, depth / 2.0f });
            float ratio = minDim / maxChildDim;
            child.transform.localScale = new Vector3(ratio, ratio, ratio);

            CopyChildColliderTo(child, gameObject);
        }

        static private void CopyChildColliderTo(GameObject child, GameObject go)
        {
            SphereCollider childColl = child.GetComponent<SphereCollider>();
            if (null == childColl)
                return;
            SphereCollider parentColl = go.GetComponent<SphereCollider>();
            if (parentColl == null)
            {
                parentColl = go.AddComponent<SphereCollider>();
            }
            parentColl.center = parentColl.transform.InverseTransformPoint(childColl.transform.TransformPoint(childColl.center));
            parentColl.radius = childColl.radius * childColl.transform.localScale.x;
            parentColl.isTrigger = true;
            childColl.enabled = false;
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

            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                NeedsRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));

            Vector3 worldCenter = transform.TransformPoint(new Vector3(width / 2.0f, -height / 2.0f, -depth / 2.0f));

            Vector3 front_top_left = transform.TransformPoint(new Vector3(0, 0, -depth));
            Vector3 front_top_right = transform.TransformPoint(new Vector3(width, 0, -depth));
            Vector3 front_bottom_left = transform.TransformPoint(new Vector3(0, -height, -depth));
            Vector3 front_bottom_right = transform.TransformPoint(new Vector3(width, -height, -depth));

            Vector3 back_top_left = transform.TransformPoint(new Vector3(0, 0, 0));
            Vector3 back_top_right = transform.TransformPoint(new Vector3(width, 0, 0));
            Vector3 back_bottom_left = transform.TransformPoint(new Vector3(0, -height, 0));
            Vector3 back_bottom_right = transform.TransformPoint(new Vector3(width, -height, 0));

            Gizmos.color = Color.white;

            Gizmos.DrawLine(front_top_left,     front_top_right);
            Gizmos.DrawLine(front_top_right,    front_bottom_right);
            Gizmos.DrawLine(front_bottom_right, front_bottom_left);
            Gizmos.DrawLine(front_bottom_left,  front_top_left);

            Gizmos.DrawLine(back_top_left,      back_top_right);
            Gizmos.DrawLine(back_top_right,     back_bottom_right);
            Gizmos.DrawLine(back_bottom_right,  back_bottom_left);
            Gizmos.DrawLine(back_bottom_left,   back_top_left);

            Gizmos.DrawLine(front_top_left,     back_top_left);
            Gizmos.DrawLine(front_top_right,    back_top_right);
            Gizmos.DrawLine(front_bottom_right, back_bottom_right);
            Gizmos.DrawLine(front_bottom_left,  back_bottom_left);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            // TODO: pass the Cursor to the object3d, test the object instead of a hardcoded name.
            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                int hash = gameObject.GetHashCode();
                
                onEnterUI3DObject.Invoke(hash);
                //VRInput.SendHaptic(VRInput.rightController, 0.03f, 1.0f);
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                // RE-instantiate an object from the prefab, the other one being in the user's hands.
                onReleaseEvent.Invoke();
                onExitUI3DObject.Invoke(gameObject.GetHashCode());
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

            CopyChildColliderTo(child, go);

            ToolsUIManager.Instance.RegisterUI3DObject(go);
        }
    }
}
