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
        private bool useCollider = true; // true if the collider of the whole item is used for the UI, when the content has no interactable items.
        public bool UseColliderForUI { get { return useCollider; } set { useCollider = value; GetComponent<BoxCollider>().enabled = value; } }

        public void SetSelected(bool value)
        {
            Content.gameObject.GetComponent<ListItemContent>().SetSelected(value);
        }

        public override void ResetColor()
        {
            //SetColor(Disabled ? DisabledColor
            //      : (Pushed ? PushedColor
            //      : (Checked ? CheckedColor
            //      : (Selected ? SelectedColor
            //      : (Hovered ? HoveredColor
            //      : BaseColor)))));

            // Make the content pop front if Hovered.
            content.transform.localPosition = Hovered ? new Vector3(0, 0, -0.003f) : Vector3.zero;
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
                    if (UseColliderForUI)
                    {
                        boxCollider = GetComponent<BoxCollider>();

                        Vector3 e = content.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents;
                        float colliderZ = Mathf.Max(2.0f * e.z, 0.5f);
                        boxCollider.center = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(0f, 0f, colliderZ / 2.0f - e.z)));
                        boxCollider.size = transform.InverseTransformVector(content.transform.TransformVector(new Vector3(2.0f * e.x, 2.0f * e.y, colliderZ)));
                        boxCollider.isTrigger = true;
                    }
                }
                else
                {
                    content.transform.localScale = Vector3.one;

                    // adapt collider to the whole cell size
                    if (UseColliderForUI)
                    {
                        boxCollider = GetComponent<BoxCollider>();

                        float collider_depth = 0.5f;
                        float collider_front = 0.001f; // TODO: get the thickness of the item mesh, or use the Depth property.
                        boxCollider.center = new Vector3(width / 2.0f, -height / 2.0f, collider_depth / 2.0f - collider_front);
                        boxCollider.size = new Vector3(width, height, collider_depth);
                        boxCollider.isTrigger = true;
                    }
                }
            }
        }

        public void OnAnySubItemClicked()
        {
            list.FireItem(Content);
        }

        #region ray

        public override void OnRayEnter()
        {
            base.OnRayEnter();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);
            //onHoverEvent.Invoke();
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();
            //onHoverEvent.Invoke();
        }

        public override void OnRayExit()
        {
            base.OnRayExit();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();
            WidgetBorderHapticFeedback();
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
            list.FireItem(Content);
        }

        public override void OnRayReleaseOutside()
        {
            base.OnRayReleaseOutside();
        }

        #endregion
    }
}
