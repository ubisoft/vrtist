using UnityEngine;

namespace VRtist
{
    public class UIElement : MonoBehaviour
    {
        // "stylesheet"
        public static readonly Color default_foreground_color = new Color(0.9f, 0.9f, 0.9f, 1.0f); // white but not full white
        public static readonly Color default_background_color = new Color(0.1742f, 0.5336f, 0.723f, 1.0f); // default blue (44 136 184)
        public static readonly Color default_pushed_color = new Color(0.0f, 0.65f, 1.0f, 1.0f); // light vivid blue
        public static readonly Color default_checked_color = new Color(0.0f, 0.85f, 1.0f, 1.0f); // light vivid blue
        public static readonly Color default_hover_color = new Color(2.0f, 0.8f, 0.0f, 1.0f); // hdr yellow
        //public static readonly Color default_disabled_color = new Color(0.20f, 0.23f, 0.25f); // dark grey blue
        //public static readonly Color default_disabled_color = new Color(0.4708f, 0.7317f, 0.8679f); // light grey blue
        public static readonly Color default_disabled_color = new Color(0.5873f, 0.6170f, 0.6320f); // middle grey blue
        public static readonly Color default_slider_rail_color = new Color(0.1f, 0.1f, 0.1f, 1.0f); // darker grey.
        public static readonly Color default_slider_knob_color = new Color(0.9f, 0.9f, 0.9f, 1.0f); // lighter grey.

        public static readonly float collider_min_depth_shallow = 0.03f;
        public static readonly float collider_min_depth_deep = 0.1f;

        public static OrderedGuard<bool> UIEnabled = new OrderedGuard<bool>(true);

        [SpaceHeader("Base Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterVector3] public Vector3 relativeLocation = Vector3.zero; // location of this object relative to its parent anchor
        [CentimeterFloat] public float width = 1.0f;
        [CentimeterFloat] public float height = 1.0f;
        public Color baseColor = UIElement.default_background_color;
        public Color disabledColor = UIElement.default_disabled_color;

        private bool isDisabled = false;

        private Vector3 anchor = Vector3.zero; // local position of anchor for children.

        //
        // Properties
        //

        public Vector3 Anchor { get { return anchor; } set { anchor = value; UpdateChildren(); } }
        public Vector3 RelativeLocation { get { return relativeLocation; } set { relativeLocation = value; UpdateLocalPosition(); } }
        public float Width { get { return width; } set { width = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }
        public float Height { get { return height; } set { height = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }
        public Color BaseColor { get { return baseColor; } set { baseColor = value; SetColor(value); } }
        public Color DisabledColor { get { return disabledColor; } set { disabledColor = value; SetColor(value); } }
        public bool Disabled { get { return isDisabled; } set { isDisabled = value; SetColor(value?DisabledColor:BaseColor); } }

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
            //anchor = new Vector3(-width / 2.0f, height / 2.0f, 0.0f);
            anchor = Vector3.zero;
        }

        public virtual void SetColor(Color color)
        {
            // NOTE: est-ce qu'on a vraiment besoin d'acceder a material, sachant que le sharedMaterial est deja
            // une instance manuelle, et qu'il ne share rien, c'est son unique instance???
            //Material material = GetComponent<MeshRenderer>().material; // THIS triggers the warning in editor.
            Material material = GetComponent<MeshRenderer>().sharedMaterial;
            material.SetColor("_BaseColor", color);
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
    }
}
