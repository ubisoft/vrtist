using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [RequireComponent(typeof(BoxCollider))]
    public class UIDynamicListItem : UIElement
    {
        private Transform content = null;
        public Transform Content { get { return content; } set { content = value; value.parent = transform; AdaptContent(); } }

        public GameObjectHashChangedEvent onObjectClickedEvent = new GameObjectHashChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private BoxCollider boxCollider = null;

        private void Start()
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        public void AdaptContent()
        {
            if (content != null)
            {
                Vector3 childExtents = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents; // TODO: what is many meshFilters?
                float maxChildDim = Mathf.Max(new float[] { childExtents.x, childExtents.y, childExtents.z });
                float minDim = Mathf.Min(new float[] { width / 2.0f, height / 2.0f, width / 2.0f }); // depth?
                float ratio = minDim / maxChildDim;
                content.transform.localScale = new Vector3(ratio, ratio, ratio);

                // adapt collider to the new mesh size (in local space)
#if UNITY_EDITOR
                boxCollider = GetComponent<BoxCollider>();
#endif
                Vector3 e = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents;
                boxCollider.size = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(2.0f * e.x, 2.0f * e.y, Mathf.Max(0.01f, 2.0f * e.z))));
                boxCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                int hash = gameObject.GetHashCode();
                onObjectClickedEvent.Invoke(hash);
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (!UIEnabled.Value) return;

            if (Disabled) { return; }

            if (otherCollider.gameObject.name == "Cursor")
            {

            }
        }
    }
}
