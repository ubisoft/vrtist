using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerHue : UIElement
    {
        // UIElement ?

        //private float width = 1.0f;
        //private float height = 1.0f;
        private float thickness = 1.0f;

        public UIColorPicker colorPicker = null;
        float cursorPosition = 0.5f; // normalized position

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

        public override void ResetColor()
        {

        }

        public float GetHue()
        {
            return cursorPosition;
        }

        // value: [0..1]
        public void SetHue(float value)
        {
            cursorPosition = value;
            cursor.localPosition = new Vector3(width * value, -height/2.0f, 0);
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(newWidth, newHeight, newThickness);
            theNewMesh.name = "UIColorPickerHue_GeneratedMesh";
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

        // --- RAY API ----------------------------------------------------

        public override void OnRayEnter()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            ResetColor();
            //VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
        }

        public override void OnRayEnterClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();
        }

        public override void OnRayHover()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            ResetColor();
            //onHoverEvent.Invoke();
        }

        public override void OnRayHoverClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            ResetColor();
            //onHoverEvent.Invoke();
        }

        public override void OnRayExit()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            ResetColor();
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
        }

        public override void OnRayExitClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true; // exiting while clicking shows a hovered button.
            Pushed = false;
            ResetColor();
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
        }

        public override void OnRayClick()
        {
            if (IgnoreRayInteraction())
                return;

            colorPicker.OnClick();

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayReleaseInside()
        {
            if (IgnoreRayInteraction())
                return;

            colorPicker.OnRelease();

            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayReleaseOutside()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            ResetColor();
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
            SetHue(Mathf.Clamp(pct, 0, 1));
            colorPicker.OnColorChanged();

            // Haptic intensity as we go deeper into the widget.
            //float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            //intensity *= intensity; // ease-in

            //VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            //cursorShapeTransform.position = worldProjectedWidgetPosition;
            rayEndPoint = worldProjectedWidgetPosition;
        }
        // --- / RAY API ----------------------------------------------------












        public static UIColorPickerHue CreateUIColorPickerHue(
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

            UIColorPickerHue uiColorPickerHue = go.AddComponent<UIColorPickerHue>();
            uiColorPickerHue.relativeLocation = relativeLocation;
            uiColorPickerHue.transform.parent = parent;
            uiColorPickerHue.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPickerHue.transform.localRotation = Quaternion.identity;
            uiColorPickerHue.transform.localScale = Vector3.one;
            uiColorPickerHue.width = width;
            uiColorPickerHue.height = height;
            uiColorPickerHue.thickness = thickness;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(width, height, thickness);
                uiColorPickerHue.Anchor = Vector3.zero;
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
            cursor.transform.parent = uiColorPickerHue.transform;
            cursor.transform.localPosition = Vector3.zero;
            uiColorPickerHue.cursor = cursor.transform;

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiColorPickerHue;
        }
    }
}
