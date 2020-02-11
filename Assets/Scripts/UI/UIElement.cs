using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class UIElement : MonoBehaviour
    {
        // "stylesheet"
        public static readonly Color default_color = new Color(0.8f, 0.8f, 0.8f, 1.0f); // light grey.
        public static readonly Color default_background_color = new Color(0.2f, 0.2f, 0.2f, 1.0f); // dark grey.
        public static readonly Color default_slider_rail_color = new Color(0.1f, 0.1f, 0.1f, 1.0f); // darker grey.
        public static readonly Color default_slider_knob_color = new Color(0.9f, 0.9f, 0.9f, 1.0f); // lighter grey.

        public static readonly float collider_min_depth_shallow = 0.03f;
        public static readonly float collider_min_depth_deep = 0.1f;

        [SpaceHeader("Base Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterVector3] public Vector3 relativeLocation = Vector3.zero; // location of this object relative to its parent anchor
        [CentimeterFloat] public float width = 1.0f;
        [CentimeterFloat] public float height = 1.0f;
        public Color baseColor = UIElement.default_color;

        private Vector3 anchor = Vector3.zero; // local position of anchor for children.

        //
        // Properties
        //

        public Vector3 Anchor { get { return anchor; } set { anchor = value; UpdateChildren(); } }
        public Vector3 RelativeLocation { get { return relativeLocation; } set { relativeLocation = value; UpdateLocalPosition(); } }
        public float Width { get { return width; } set { width = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }
        public float Height { get { return height; } set { height = value; RebuildMesh(); UpdateAnchor(); UpdateChildren(); } }
        public Color BaseColor { get { return baseColor; } set { baseColor = value; SetColor(value); } }

        public void UpdateLocalPosition()
        {
            UIElement parentElem = transform.parent ? transform.parent.gameObject.GetComponent<UIElement>() : null;
            if (parentElem)
            {
                transform.localPosition = parentElem.anchor + relativeLocation;
            }
            else
            {
                transform.localPosition = relativeLocation;

                // TODO: find a way to change the width/height/position of a top-level widget
                // that has rotations.

                //Vector3 worldPosition = transform.TransformPoint(relativeLocation);
                //if (transform.parent)
                //{
                //    Vector3 parentRelativeLocation = transform.parent.InverseTransformPoint(worldPosition);
                //    transform.localPosition = parentRelativeLocation;
                //}
                //else
                //{
                //    // not in the good frame.
                //    transform.position = worldPosition;
                //}
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

        public void SetColor(Color color)
        {
            Material material = GetComponent<MeshRenderer>().material; // THIS triggers the warning in editor.
            material.SetColor("_BaseColor", color);
        }

        public virtual void RebuildMesh() { }

        public virtual bool HandlesCursorBehavior() { return false; }
        public virtual void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform) { }
    }
}
