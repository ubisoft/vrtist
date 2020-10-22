using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerHSV : UIElement
    {
        public static readonly string default_widget_name = "HSV";
        public static readonly float default_width = 0.20f;
        public static readonly float default_height = 0.20f;
        public static readonly float default_thickness = 0.001f;
        public static readonly float default_trianglePct = 0.7f;
        public static readonly float default_innerCirclePct = 0.8f;
        public static readonly float default_outerCirclePct = 1.0f;
        public static readonly string default_sv_material_name = "SaturationMaterial";
        public static readonly string default_hue_material_name = "HueMaterial";
        public static readonly string default_saturation_cursor_name = "Cursor_Saturation";
        public static readonly string default_hue_cursor_name = "Cursor_Hue";

        public UIColorPicker colorPicker = null;
        public float trianglePct = default_trianglePct;
        public float innerCirclePct = default_innerCirclePct;
        public float outerCirclePct = default_outerCirclePct;
        public Transform hueCursor;
        public Transform svCursor;

        private float thickness = 1.0f;

        private float hueCursorPosition = 0.0f; // normalized position

        // 3 points (A, B, C) = (HUE, WHITE, BLACK)
        private Vector3 svCursorPosition = new Vector3(1.0f, 0.0f, 0.0f); // barycentric coordinates


        void Awake()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                colorPicker = GetComponentInParent<UIColorPicker>();
                //width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                //height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                //thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public override void ResetColor()
        {

        }

        public float Hue { get { return hueCursorPosition; } }
        public float Saturation { get { return 1.0f - svCursorPosition.z; }}
        public float Value { get { return 1.0f - svCursorPosition.y; } }
        public Vector3 HSV { set { 
                hueCursorPosition = value.x; 
                svCursorPosition.y = value.y; 
                svCursorPosition.z = value.z; 
                svCursorPosition.x = 1.0f - value.y - value.z;
                UpdateCursorPositions();
                UpdateSVColor();
            } 
        }

        private void UpdateCursorPositions()
        {
            float w2 = width / 2.0f;
            float h2 = height / 2.0f;
            float ir = innerCirclePct * w2;
            float or = outerCirclePct * w2;

            hueCursor.localPosition = new Vector3(
                w2 - ir * -Mathf.Cos(hueCursorPosition * 2.0f * Mathf.PI),
                -h2 - ir * Mathf.Sin(hueCursorPosition * 2.0f * Mathf.PI),
                -thickness);

            // TODO: cursor in triangle
        }

        private void UpdateSVColor()
        {
            Color baseColor = Color.HSVToRGB(svCursorPosition.x, 1f, 1f); // pure hue color
            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterials[1].SetColor("_Color", baseColor);
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness, float newTrianglePct, float newInnerCirclePct, float newOuterCirclePct)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildHSV(newWidth, newHeight, newThickness, newTrianglePct, newInnerCirclePct, newOuterCirclePct, 72);
            theNewMesh.name = "UIColorPickerHSV_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;
            trianglePct = newTrianglePct;
            innerCirclePct = newInnerCirclePct;
            outerCirclePct = newOuterCirclePct;

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

            float currentKnobPositionX = hueCursorPosition * width;

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
            //SetHue(Mathf.Clamp(pct, 0, 1));
            colorPicker.OnColorChanged();

            // Haptic intensity as we go deeper into the widget.
            //float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            //intensity *= intensity; // ease-in

            //VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            //cursorShapeTransform.position = worldProjectedWidgetPosition;
            rayEndPoint = worldProjectedWidgetPosition;
        }

        // Compute barycentric coordinates (u, v, w) for
        // point p with respect to triangle (a, b, c)
        // BEWARE: X is ignored, it is a 2D implementation!!!
        Vector3 GetBarycentricCoordinates(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float den = v0.x * v1.y - v1.x * v0.y;
            float v = (v2.x * v1.y - v1.x * v2.y) / den;
            float w = (v0.x * v2.y - v2.x * v0.y) / den;
            float u = 1.0f - v - w;
            return new Vector3(u, v, w);
        }

        #endregion

        #region create

        public class CreateParams
        {
            public Transform parent = null;
            public string widgetName = UIButton.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -default_thickness);
            public float width = default_width;
            public float height = default_height;
            public float thickness = default_thickness;
            public float trianglePct = default_trianglePct;
            public float innerCirclePct = default_innerCirclePct;
            public float outerCirclePct = default_outerCirclePct;
            public Material hueMaterial = UIUtils.LoadMaterial(default_hue_material_name);
            public Material svMaterial = UIUtils.LoadMaterial(default_sv_material_name);
            public GameObject hueCursorPrefab = UIUtils.LoadPrefab(default_hue_cursor_name);
            public GameObject svCursorPrefab = UIUtils.LoadPrefab(default_saturation_cursor_name);
        }

        public static UIColorPickerHSV Create(CreateParams input)
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

            UIColorPickerHSV uiColorPickerHSV = go.AddComponent<UIColorPickerHSV>();
            uiColorPickerHSV.relativeLocation = input.relativeLocation;
            uiColorPickerHSV.transform.parent = input.parent;
            uiColorPickerHSV.transform.localPosition = parentAnchor + input.relativeLocation;
            uiColorPickerHSV.transform.localRotation = Quaternion.identity;
            uiColorPickerHSV.transform.localScale = Vector3.one;
            uiColorPickerHSV.width = input.width;
            uiColorPickerHSV.height = input.height;
            uiColorPickerHSV.thickness = input.thickness;
            uiColorPickerHSV.trianglePct = input.trianglePct;
            uiColorPickerHSV.innerCirclePct = input.innerCirclePct;
            uiColorPickerHSV.outerCirclePct = input.outerCirclePct;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildHSV(
                    input.width, input.height, input.thickness, 
                    input.trianglePct, input.innerCirclePct, input.outerCirclePct, 72);
                uiColorPickerHSV.Anchor = Vector3.zero;
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
            if (meshRenderer != null && input.hueMaterial != null && input.svMaterial != null)
            {
                meshRenderer.sharedMaterials = new Material[] { Instantiate(input.hueMaterial), Instantiate(input.svMaterial) };
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 1 << 1; // "LightLayer 1"
            }

            // Add a cursor
            GameObject hueCursor = Instantiate<GameObject>(input.hueCursorPrefab);
            hueCursor.transform.parent = uiColorPickerHSV.transform;
            hueCursor.transform.localPosition = Vector3.zero;
            uiColorPickerHSV.hueCursor = hueCursor.transform;

            GameObject svCursor = Instantiate<GameObject>(input.svCursorPrefab);
            svCursor.transform.parent = uiColorPickerHSV.transform;
            svCursor.transform.localPosition = Vector3.zero;
            uiColorPickerHSV.svCursor = svCursor.transform;

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiColorPickerHSV;
        }

        #endregion
    }
}
