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
using UnityEngine.Events;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIKeyView : UIElement
    {
        public static readonly string default_widget_name = "New KeyView";
        public static readonly float default_width = 0.15f;
        public static readonly float default_height = 0.05f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly string default_material_name = "UIElementTransparent";

        public Dopesheet dopesheet = null;

        [SpaceHeader("Label Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public Material source_material = null;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onHoverEvent = new UnityEvent();
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UIKeyView_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
        }

        private void UpdateColliderDimensions()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            if (meshFilter != null && coll != null)
            {
                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                {
                    coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                    coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                }
                else
                {
                    coll.center = initColliderCenter;
                    coll.size = initColliderSize;
                }
            }
        }

        public override void ResetMaterial()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material materialInstance = Instantiate(source_material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIKeyView_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const int min_nbSubdivCornerFixed = 1;
            const int min_nbSubdivCornerPerUnit = 1;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (margin > width / 2.0f || margin > height / 2.0f)
                margin = Mathf.Min(width / 2.0f, height / 2.0f);
            if (nbSubdivCornerFixed < min_nbSubdivCornerFixed)
                nbSubdivCornerFixed = min_nbSubdivCornerFixed;
            if (nbSubdivCornerPerUnit < min_nbSubdivCornerPerUnit)
                nbSubdivCornerPerUnit = min_nbSubdivCornerPerUnit;

            // Realign button to parent anchor if we change the thickness.
            if (-thickness != relativeLocation.z)
                relativeLocation.z = -thickness;

            NeedsRebuild = true;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (NeedsRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                ResetColor();
                NeedsRebuild = false;
            }
#endif
        }

        public override void ResetColor()
        {
            SetColor(Disabled ? DisabledColor
                  : (Selected ? SelectedColor
                  : BaseColor));
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        #region ray

        public override void OnRayEnter()
        {
            base.OnRayEnter();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);
            onHoverEvent.Invoke();
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();
            onHoverEvent.Invoke();
        }

        public override void OnRayExit()
        {
            base.OnRayExit();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();
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
        }

        public override bool OnRayReleaseOutside()
        {
            return base.OnRayReleaseOutside();
        }

        public override bool OverridesRayEndPoint() { return true; }

        int closestIndex = -1;
        int deltaFrame = 0;
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            float enter;
            widgetPlane.Raycast(ray, out enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            if (IgnoreRayInteraction())
            {
                // return endPoint at the surface of the widget.
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }

            // CLAMP

            float startX = 0.0f;
            float endX = width;

            if (localProjectedWidgetPosition.x < startX)
                localProjectedWidgetPosition.x = startX;

            if (localProjectedWidgetPosition.x > endX)
                localProjectedWidgetPosition.x = endX;

            localProjectedWidgetPosition.y = -height / 2.0f;

            // GRIP CLOSEST Keyframe

            if (triggerJustClicked)
            {
                deltaFrame = 0;
                float distThreshold = 0.31f / 20.0f;
                closestIndex = -1;
                float closestDistance = Mathf.Infinity;
                int i = 0;
                foreach (Transform child in transform)
                {
                    float dist = Mathf.Abs(localProjectedWidgetPosition.x - child.localPosition.x);
                    if (dist < closestDistance && dist < distThreshold)
                    {
                        closestDistance = dist;
                        closestIndex = i;
                    }
                    i++;
                }

                if (closestIndex != -1)
                {
                    localProjectedWidgetPosition.x = transform.GetChild(closestIndex).localPosition.x;
                }
            }
            else if (triggerJustReleased)
            {
                dopesheet.OnUpdateKeyframe(closestIndex, deltaFrame);
                deltaFrame = 0;
                closestIndex = -1;
            }
            else
            {
                // JOYSTICK Left/Right
                bool joyRightJustClicked = false;
                bool joyRightJustReleased = false;
                bool joyRightLongPush = false;
                VRInput.GetInstantJoyEvent(VRInput.primaryController, VRInput.JoyDirection.RIGHT, ref joyRightJustClicked, ref joyRightJustReleased, ref joyRightLongPush);

                bool joyLeftJustClicked = false;
                bool joyLeftJustReleased = false;
                bool joyLeftLongPush = false;
                VRInput.GetInstantJoyEvent(VRInput.primaryController, VRInput.JoyDirection.LEFT, ref joyLeftJustClicked, ref joyLeftJustReleased, ref joyLeftLongPush);

                if (joyRightJustClicked || joyLeftJustClicked || joyRightLongPush || joyLeftLongPush)
                {
                    float localDeltaOneFrame = dopesheet != null ? 0.31f / (dopesheet.LocalLastFrame - dopesheet.LocalFirstFrame) : 0.0f;
                    if (joyRightJustClicked || joyRightLongPush)
                    {
                        if (closestIndex != -1)
                        {
                            deltaFrame++;
                            Transform child = transform.GetChild(closestIndex);
                            Vector3 newChildPosition = child.localPosition + new Vector3(+localDeltaOneFrame, 0, 0);
                            localProjectedWidgetPosition.x = newChildPosition.x;
                            child.localPosition = newChildPosition;
                        }
                    }
                    else if (joyLeftJustClicked || joyLeftLongPush)
                    {
                        if (closestIndex != -1)
                        {
                            deltaFrame--;
                            Transform child = transform.GetChild(closestIndex);
                            Vector3 newChildPosition = child.localPosition + new Vector3(-localDeltaOneFrame, 0 , 0);
                            localProjectedWidgetPosition.x = newChildPosition.x;
                            child.localPosition = newChildPosition;
                        }
                    }
                }
                else
                {
                    if (closestIndex != -1)
                    {
                        localProjectedWidgetPosition.x = transform.GetChild(closestIndex).localPosition.x;
                    }
                }
            }

            // OUT ray end point

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        #endregion

        #region create

        public class CreateParams
        {
            public Transform parent = null;
            public string widgetName = UIKeyView.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIKeyView.default_thickness);
            public float width = UIKeyView.default_width;
            public float height = UIKeyView.default_height;
            public float margin = UIKeyView.default_margin;
            public float thickness = UIKeyView.default_thickness;
            public Material material = UIUtils.LoadMaterial(UIKeyView.default_material_name);
            public ColorVar bgcolor = UIOptions.BackgroundColorVar;
            public ColorVar fgcolor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
        }

        public static UIKeyView Create(CreateParams input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (input.parent)
            {
                UIElement elem = input.parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIKeyView uiKeyView = go.AddComponent<UIKeyView>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiKeyView.relativeLocation = input.relativeLocation;
            uiKeyView.transform.parent = input.parent;
            uiKeyView.transform.localPosition = parentAnchor + input.relativeLocation;
            uiKeyView.transform.localRotation = Quaternion.identity;
            uiKeyView.transform.localScale = Vector3.one;
            uiKeyView.width = input.width;
            uiKeyView.height = input.height;
            uiKeyView.margin = input.margin;
            uiKeyView.thickness = input.thickness;
            uiKeyView.source_material = input.material;
            uiKeyView.baseColor.useConstant = false;
            uiKeyView.baseColor.reference = input.bgcolor;
            uiKeyView.textColor.useConstant = false;
            uiKeyView.textColor.reference = input.fgcolor;
            uiKeyView.pushedColor.useConstant = false;
            uiKeyView.pushedColor.reference = input.pushedColor;
            uiKeyView.selectedColor.useConstant = false;
            uiKeyView.selectedColor.reference = input.selectedColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiKeyView.Anchor = Vector3.zero;
                BoxCollider coll = go.GetComponent<BoxCollider>();
                if (coll != null)
                {
                    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                    if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                    {
                        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                    }
                    else
                    {
                        coll.center = initColliderCenter;
                        coll.size = initColliderSize;
                    }
                    coll.isTrigger = true;
                }
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(input.material);
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.rendererPriority = 1;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiKeyView.SetColor(input.bgcolor.value);
            }

            UIUtils.SetRecursiveLayer(go, "CameraHidden");

            return uiKeyView;
        }

        #endregion
    }
}
