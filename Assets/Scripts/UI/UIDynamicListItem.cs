using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [RequireComponent(typeof(BoxCollider))]
    public class UIDynamicListItem : UIElement
    {
        public UIDynamicList list;
        private Transform content = null;
        public Transform Content { get { return content; } set { content = value; value.parent = transform; AdaptContent(); } }

        public GameObjectHashChangedEvent onObjectClickedEvent = new GameObjectHashChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private BoxCollider boxCollider = null;

        private void Start()
        {
        }

        public void AdaptContent()
        {
            if (content != null)
            {
                Vector3 childExtents = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents; // TODO: what is many meshFilters?
                float w = (width / 2.0f) / childExtents.x;
                float h = (height / 2.0f) / childExtents.y;

                content.transform.localScale = new Vector3(w, h, 1f);

                // adapt collider to the new mesh size (in local space)
                boxCollider = GetComponent<BoxCollider>();

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

                list.FireItem(Content);
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
