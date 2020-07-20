using UnityEngine;

namespace VRtist
{
    public class UIElement : MonoBehaviour
    {
        public static readonly float default_element_thickness = 0.001f;

        public static readonly float collider_min_depth_shallow = 0.03f;
        public static readonly float collider_min_depth_deep = 0.1f;

        public static OrderedGuard<bool> UIEnabled = new OrderedGuard<bool>(true);

        [SpaceHeader("Base Parameters", 6, 0.3f, 0.3f, 0.3f)]
        [CentimeterVector3] public Vector3 relativeLocation = Vector3.zero; // location of this object relative to its parent anchor
        [CentimeterFloat] public float width = 1.0f;
        [CentimeterFloat] public float height = 1.0f;
        
        public ColorReference baseColor = new ColorReference();
        public ColorReference textColor = new ColorReference();
        public ColorReference disabledColor = new ColorReference();
        public ColorReference pushedColor = new ColorReference();
        public ColorReference selectedColor = new ColorReference();
        public ColorReference hoveredColor = new ColorReference();

        private bool isDisabled = false;
        private bool isSelected = false;
        private bool isPushed = false;
        private bool isHovered = false;

        private Vector3 anchor = Vector3.zero; // local position of anchor for children.

        private bool needsRebuild = false;

        //
        // Properties
        //

        public Vector3 Anchor { get { return anchor; } set { anchor = value; UpdateChildren(); } }
        public Vector3 RelativeLocation { get { return relativeLocation; } set { relativeLocation = value; UpdateLocalPosition(); } }
        public float Width { get { return width; } set { width = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }
        public float Height { get { return height; } set { height = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }
        public Color BaseColor { get { return baseColor.Value; } }
        public Color TextColor { get { return textColor.Value; } }
        public Color DisabledColor { get { return disabledColor.Value; } }
        public Color PushedColor { get { return pushedColor.Value; } }
        public Color SelectedColor { get { return selectedColor.Value; } }
        public Color HoveredColor { get { return hoveredColor.Value; } }
        public bool Disabled { get { return isDisabled; } set { isDisabled = value; ResetColor(); } }
        public bool Selected { get { return isSelected; } set { isSelected = value; ResetColor(); } }
        public bool Pushed { get { return isPushed; } set { isPushed = value; ResetColor(); } }
        public bool Hovered { get { return isHovered; } set { isHovered = value; ResetColor(); } }
        public bool NeedsRebuild { get { return needsRebuild; } set { needsRebuild = value; } }

        protected float prevTime = -1f;

        public virtual void UpdateLocalPosition()
        {
            UIElement parentElem = transform.parent ? transform.parent.gameObject.GetComponent<UIElement>() : null;
            if (parentElem)
            {
                transform.localPosition = parentElem.anchor + relativeLocation;
            }
            else
            {
                transform.localPosition = relativeLocation;
            }
        }

        public virtual void OnDisable()
        {
            if (GlobalState.Instance && GlobalState.Instance.cursor.IsLockedOnThisWidget(transform))
            {
                GlobalState.Instance.cursor.LockOnWidget(false);
            }
        }

        public void UpdateChildren()
        {
            // Recompute localPosition for each children, using their relative position.
            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                Transform child = gameObject.transform.GetChild(i);
                UIElement elem = child.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    elem.UpdateLocalPosition();
                }
            }
        }

        public virtual void UpdateAnchor()
        {
            anchor = Vector3.zero;
        }

        public virtual void ResetColor()
        {
            SetColor(isDisabled ? DisabledColor
                  : (isPushed ? PushedColor
                  : (isSelected ? SelectedColor
                  : (isHovered ? HoveredColor
                  : BaseColor))));
        }

        public virtual void SetColor(Color color)
        {
            Material material = GetComponent<MeshRenderer>().sharedMaterial;
            material.SetColor("_BaseColor", color);
        }

        public virtual Color GetColor()
        {
            return GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
        }

        public virtual void SetLightLayer(uint layerIndex)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.renderingLayerMask = layerIndex; // "LightLayer 1"
            }
        }

        public bool NeedToIgnoreCollisionEnter()
        {
            if (!UIEnabled.Value) return true;

            if (Disabled) { return true; }

            if (GlobalState.IsCursorLockedOnWidget()) { return true; }

            return false;
        }

        public bool NeedToIgnoreCollisionExit()
        {
            if (!UIEnabled.Value) return true;

            if (Disabled) { return true; }

            if (GlobalState.IsCursorLockedOnWidget()) { return true; }

            return false;
        }

        public bool NeedToIgnoreCollisionStay()
        {
            if (!UIEnabled.Value) return true;

            if (Disabled) { return true; }

            return false;
        }

        public virtual void RebuildMesh() { }
        public virtual void ResetMaterial() { }
        public virtual bool HandlesCursorBehavior() { return false; }
        public virtual void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform) { }

        public virtual void OnRayEnter() { }
        public virtual void OnRayHover() { }
        public virtual void OnRayExit() { }
    }
}
