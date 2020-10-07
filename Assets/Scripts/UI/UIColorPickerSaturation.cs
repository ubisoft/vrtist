using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerSaturation : UIElement
    {
        // UIElement ?

        //private float width = 1.0f;
        //private float height = 1.0f;
        private float thickness = 1.0f;

        public UIColorPicker colorPicker = null;

        Color rootColor;
        Vector2 cursorPosition = new Vector2(0.5f, 0.5f); // normalized

        public Transform cursor;

        void Awake()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                colorPicker = GetComponentInParent<UIColorPicker>();
                width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public void SetBaseColor(Color clr)
        {
            rootColor = clr;
            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial.SetColor("_Color", clr);
        }

        public Vector2 GetSaturation()
        {
            return cursorPosition;
        }

        public void SetSaturation(Vector2 sat)
        {
            cursorPosition = sat;
            cursor.localPosition = new Vector3(width * sat.x, -height * (1.0f-sat.y), 0);
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(newWidth, newHeight, newThickness);
            theNewMesh.name = "UIColorPickerSaturation_GeneratedMesh";
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
            float startY = 0;
            float endY = -height;

            Vector2 currentKnobPosition = new Vector2(cursorPosition.x * width, (-1.0f + cursorPosition.y) * height);

            // DRAG

            if (!triggerJustClicked) // if trigger just clicked, use the actual projection, no interpolation.
            {
                localProjectedWidgetPosition.x = Mathf.Lerp(currentKnobPosition.x, localProjectedWidgetPosition.x, GlobalState.Settings.RaySliderDrag);
                localProjectedWidgetPosition.y = Mathf.Lerp(currentKnobPosition.y, localProjectedWidgetPosition.y, GlobalState.Settings.RaySliderDrag);
            }

            // CLAMP

            if (localProjectedWidgetPosition.x < startX)
                localProjectedWidgetPosition.x = startX;

            if (localProjectedWidgetPosition.x > endX)
                localProjectedWidgetPosition.x = endX;

            if (localProjectedWidgetPosition.y > startY)
                localProjectedWidgetPosition.y = startY;

            if (localProjectedWidgetPosition.y < endY)
                localProjectedWidgetPosition.y = endY;

            // SET

            float x = localProjectedWidgetPosition.x / width;
            float y = 1.0f - (-localProjectedWidgetPosition.y / height);
            x = Mathf.Clamp(x, 0, 1);
            y = Mathf.Clamp(y, 0, 1);
            SetSaturation(new Vector2(x, y));
            colorPicker.OnColorChanged();

            // Haptic intensity as we go deeper into the widget.
            //float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            //intensity *= intensity; // ease-in

            //VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            //cursorShapeTransform.position = worldProjectedWidgetPosition;
            rayEndPoint = worldProjectedWidgetPosition;
        }

        #endregion

        #region create

        public static UIColorPickerSaturation CreateUIColorPickerSaturation(
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

            UIColorPickerSaturation uiColorPickerSaturation = go.AddComponent<UIColorPickerSaturation>();
            uiColorPickerSaturation.relativeLocation = relativeLocation;
            uiColorPickerSaturation.transform.parent = parent;
            uiColorPickerSaturation.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPickerSaturation.transform.localRotation = Quaternion.identity;
            uiColorPickerSaturation.transform.localScale = Vector3.one;
            uiColorPickerSaturation.width = width;
            uiColorPickerSaturation.height = height;
            uiColorPickerSaturation.thickness = thickness;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(width, height, thickness);
                uiColorPickerSaturation.Anchor = Vector3.zero;
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
            cursor.transform.parent = uiColorPickerSaturation.transform;
            cursor.transform.localPosition = Vector3.zero;
            uiColorPickerSaturation.cursor = cursor.transform;

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiColorPickerSaturation;
        }

        #endregion
    }
}
