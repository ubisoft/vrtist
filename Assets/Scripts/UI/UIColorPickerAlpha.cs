using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerAlpha : UIElement
    {
        public UIColorPicker colorPicker = null;

        private float thickness = 1.0f;
        public float cursorPosition = 1.0f; // normalized position, full opaque

        public Transform cursor;

        public override void ResetColor()
        {

        }

        // TMP - REMOVE AFTER TESTS ------------------
        private void OnValidate()
        {
            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                UpdateCursorPosition();
                NeedsRebuild = false;
            }
        }
        // TMP - REMOVE AFTER TESTS ------------------

        public float GetAlpha()
        {
            return cursorPosition;
        }

        // value: [0..1]
        public void SetAlpha(float value)
        {
            cursorPosition = value;
            UpdateCursorPosition();
        }

        public void UpdateCursorPosition()
        {
            Vector3 cs = cursor.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size;

            cursor.localPosition = new Vector3(width * cursorPosition, -height / 2.0f, -cs.z / 2.0f); //-thickness - cs.z / 2.0f );
            cursor.localRotation = Quaternion.identity; // tmp
            cursor.localScale = new Vector3(1, height / cs.y, 1);
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(newWidth, newHeight, newThickness);
            theNewMesh.name = "UIColorPickerAlpha_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;

            UpdateColliderDimensions();
        }

        public void UpdateColliderDimensions()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            if (meshFilter != null && coll != null)
            {
                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                if (initColliderSize.z < UIElement.collider_min_depth_deep)
                {
                    coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_deep / 2.0f);
                    coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_deep);
                }
                else
                {
                    coll.center = initColliderCenter;
                    coll.size = initColliderSize;
                }
            }
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
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();
        }

        public override void OnRayExit()
        {
            base.OnRayExit();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();
        }

        public override void OnRayClick()
        {
            base.OnRayClick();
            colorPicker.OnClick();
        }

        public override void OnRayReleaseInside()
        {
            base.OnRayReleaseInside();
            colorPicker.OnRelease();
        }

        public override bool OnRayReleaseOutside()
        {
            return base.OnRayReleaseOutside();
        }

        public override bool OverridesRayEndPoint() { return true; }
        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.rightController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

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

            float startX = 0;
            float endX = width;

            float currentKnobPositionX = cursorPosition * width;

            // DRAG

            if (!triggerJustClicked) // if trigger just clicked, use the actual projection, no interpolation.
            {
                localProjectedWidgetPosition.x = Mathf.Lerp(currentKnobPositionX, localProjectedWidgetPosition.x, GlobalState.Settings.RaySliderDrag);
            }

            // CLAMP

            if (localProjectedWidgetPosition.x < startX)
                localProjectedWidgetPosition.x = startX;

            if (localProjectedWidgetPosition.x > endX)
                localProjectedWidgetPosition.x = endX;

            localProjectedWidgetPosition.y = -height / 2.0f;

            // SET

            float pct = localProjectedWidgetPosition.x / width;
            SetAlpha(Mathf.Clamp(pct, 0, 1));
            colorPicker.OnColorChanged();

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            //cursorShapeTransform.position = worldProjectedWidgetPosition;
            rayEndPoint = worldProjectedWidgetPosition;
        }

        #endregion

        #region create

        public static UIColorPickerAlpha Create(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            Material material,
            GameObject cursorPrefab)
        {
            GameObject go = new GameObject(objectName);
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

            UIColorPickerAlpha uiColorPickerAlpha = go.AddComponent<UIColorPickerAlpha>();
            uiColorPickerAlpha.relativeLocation = relativeLocation;
            uiColorPickerAlpha.transform.parent = parent;
            uiColorPickerAlpha.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPickerAlpha.transform.localRotation = Quaternion.identity;
            uiColorPickerAlpha.transform.localScale = Vector3.one;
            uiColorPickerAlpha.width = width;
            uiColorPickerAlpha.height = height;
            uiColorPickerAlpha.thickness = thickness;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(width, height, thickness);
                uiColorPickerAlpha.Anchor = Vector3.zero;
                BoxCollider coll = go.GetComponent<BoxCollider>();
                if (coll != null)
                {
                    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                    if (initColliderSize.z < UIElement.collider_min_depth_deep)
                    {
                        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_deep / 2.0f);
                        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_deep);
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
            if (meshRenderer != null && material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(material);
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            // Add a cursor
            GameObject cursor = Instantiate<GameObject>(cursorPrefab);
            cursor.transform.parent = uiColorPickerAlpha.transform;
            cursor.transform.localPosition = Vector3.zero;
            cursor.transform.localRotation = Quaternion.identity;
            uiColorPickerAlpha.cursor = cursor.transform;

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiColorPickerAlpha;
        }

        #endregion
    }
}
