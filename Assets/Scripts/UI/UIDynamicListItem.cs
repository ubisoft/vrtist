using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [RequireComponent(typeof(BoxCollider)),
     RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIDynamicListItem : UIElement
    {
        public UIDynamicList list;
        private Transform content = null;
        public Transform Content { get { return content; } set { content = value; value.parent = transform; AdaptContent(); } }
        public bool autoResizeContent = true;
        public bool autoCenterContent = true;

        [CentimeterFloat] public float depth = 1.0f;
        public float Depth { get { return depth; } set { depth = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }

        public GameObjectHashChangedEvent onObjectClickedEvent = new GameObjectHashChangedEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private BoxCollider boxCollider = null;

        public void SetSelected(bool value)
        {
            Content.gameObject.GetComponent<ListItemContent>().SetSelected(value);
        }

        public void AdaptContent()
        {
            // TODO: handle resize/center content
            // bool autoResizeContent, bool autoCenterContent

            if(content != null)
            {
                if(autoResizeContent)
                {
                    Vector3 childExtents = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents; // TODO: what is many meshFilters?
                    float w = (width / 2.0f) / childExtents.x;
                    float h = (height / 2.0f) / childExtents.y;
                    float d = (depth / 2.0f) / childExtents.z;

                    content.transform.localScale = new Vector3(w, h, d);

                    // adapt collider to the new mesh size (in local space)
                    boxCollider = GetComponent<BoxCollider>();

                    Vector3 e = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents;
                    float colliderZ = Mathf.Max(2.0f * e.z, 0.5f);
                    boxCollider.center = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(0f, 0f, colliderZ / 2.0f - e.z)));
                    boxCollider.size = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(2.0f * e.x, 2.0f * e.y, colliderZ)));
                    boxCollider.isTrigger = true;
                }
                else
                {
                    content.transform.localScale = Vector3.one;

                    // adapt collider to the whole cell size
                    boxCollider = GetComponent<BoxCollider>();

                    float collider_depth = 0.5f;
                    float collider_front = 0.001f; // TODO: get the thickness of the item mesh, or use the Depth property.
                    boxCollider.center = new Vector3(width / 2.0f, -height / 2.0f, collider_depth / 2.0f - collider_front);
                    boxCollider.size = new Vector3(width, height, collider_depth);
                    boxCollider.isTrigger = true;
                }
            }
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCursorColliderCenter);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            // Haptic intensity as we go deeper into the widget.
            float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            intensity *= intensity; // ease-in
            VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if(!UIEnabled.Value) return;

            if(Disabled) { return; }

            if(otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();

                list.FireItem(Content);
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if(!UIEnabled.Value) return;

            if(Disabled) { return; }

            if(otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if(!UIEnabled.Value) return;

            if(Disabled) { return; }

            if(otherCollider.gameObject.name == "Cursor")
            {

            }
        }
    }
}
