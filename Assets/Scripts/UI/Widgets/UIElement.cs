/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
        public ColorReference disabledTextColor = new ColorReference();
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
        public Color DisabledTextColor { get { return disabledTextColor.Value; } }
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
            // TODO: unlock the ray if it was locked on this widget.
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
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (null != meshRenderer)
            {
                Material material = meshRenderer.sharedMaterial;
                if (null != material)
                {
                    material.SetColor("_BaseColor", color);
                }
            }
        }

        public virtual Color GetColor()
        {
            return GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
        }

        public virtual void SetForegroundColor(Color color)
        {
        }

        public virtual void SetLightLayer(int layerIndex)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.renderingLayerMask = (1u << layerIndex);
            }
        }

        public virtual bool IgnoreRayInteraction()
        {
            if (!UIEnabled.Value) return true;

            if (Disabled) { return true; }

            return false;
        }

        public virtual void RebuildMesh() { }
        public virtual void ResetMaterial() { }

        public static void WidgetBorderHapticFeedback()
        {
            VRInput.SendHaptic(VRInput.primaryController, 0.025f, 0.1f);
        }

        public static void ClickHapticFeedback()
        {
            VRInput.SendHaptic(VRInput.primaryController, 0.05f, 0.3f);
        }

        #region ray

        // Most common code implemented here.

        public virtual void OnRayEnter()
        {
            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public virtual void OnRayEnterClicked()
        {
            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public virtual void OnRayHover(Ray ray)
        {
            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public virtual void OnRayHoverClicked()
        {
            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public virtual void OnRayExit()
        {
            Hovered = false;
            Pushed = false;
            ResetColor();
        }

        public virtual void OnRayExitClicked()
        {
            Hovered = true; // exiting while clicking shows a hovered button.
            Pushed = false;
            ResetColor();
        }

        public virtual void OnRayClick()
        {
            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public virtual void OnRayReleaseInside()
        {
            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        // @return true if the release is considered as a validation of the widget.
        public virtual bool OnRayReleaseOutside()
        {
            Hovered = false;
            Pushed = false;
            ResetColor();

            return false;
        }

        public virtual bool OverridesRayEndPoint() { return false; }
        public virtual void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint) { }

        #endregion
    }
}
