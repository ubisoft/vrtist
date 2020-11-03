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
        public static readonly float default_trianglePct = 0.75f;
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

        public float thickness = 1.0f;
        public Vector3 hsv = new Vector3(1,0,0);

        bool lockedOnCircle = false;
        bool lockedOnTriangle = false;

        // TMP
        //public Color tmpRGBColor = Color.green;
        //public float tmpSaturation = 0;
        //public float tmpValue = 0;
        // TMP

        // 3 points (A, B, C) = (HUE, WHITE, BLACK)
        //           C
        //          / \
        //         B---A
        public Vector3 barycentric = new Vector3(1.0f, 0.0f, 0.0f); // barycentric coordinates

        public override void ResetColor()
        {

        }

        // TMP - REMOVE AFTER TESTS ------------------
        //private void OnValidate()
        //{
        //    NeedsRebuild = true;
        //}

        //private void Update()
        //{
        //    if (NeedsRebuild)
        //    {
        //        float H, S, V;
        //        Color.RGBToHSV(tmpRGBColor, out H, out S, out V);
        //        HSV = new Vector3(H, S, V);
        //        tmpSaturation = S;
        //        tmpValue = V;
        //        RebuildMesh(width, height, thickness, trianglePct, innerCirclePct, outerCirclePct);
        //        UpdateCursorPositions();
        //        NeedsRebuild = false;
        //    }
        //}
        // TMP - REMOVE AFTER TESTS ------------------

        public float Hue { get { return hsv.x; } }
        public float Saturation { get { return hsv.y; } }
        public float Value { get { return hsv.z; } }

        //public float Saturation { 
        //    get { 
        //        Color rgb = BarycentricToRGB();
        //        float H, S, V;
        //        Color.RGBToHSV(rgb, out H, out S, out V);
        //        return S;
        //    }
        //} 
        //public float Value {
        //    get
        //    {
        //        Color rgb = BarycentricToRGB();
        //        float H, S, V;
        //        Color.RGBToHSV(rgb, out H, out S, out V);
        //        return V;
        //    }
        //}

        public Vector3 HSV { set { 
                hsv = value;
                barycentric = HSVtoBarycentric(value);

                // inject the hue into vertex colors of the mesh
                RebuildMesh(width, height, thickness, trianglePct, innerCirclePct, outerCirclePct);
                UpdateCursorPositions();
                //UpdateSVColor();
            } 
        }

        private Vector3 HSVtoBarycentric(Vector3 hsv)
        {
            float w2 = width / 2.0f;
            float h2 = height / 2.0f;
            float tr = trianglePct * w2;

            // 3 points (A, B, C) = (HUE, WHITE, BLACK)
            //           C
            //          / \
            //         / P \
            //        B-----A
            Vector3 pt_A_HUE = new Vector3(w2 + tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), -thickness);
            Vector3 pt_B_WHITE = new Vector3(w2 - tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), -thickness);
            Vector3 pt_C_BLACK = new Vector3(w2, -h2 + tr, -thickness);

            // Point on CA at Value position
            Vector3 pt_CA = Vector3.Lerp(pt_C_BLACK, pt_A_HUE, hsv.z);

            // Point on CB at Value position
            Vector3 pt_CB = Vector3.Lerp(pt_C_BLACK, pt_B_WHITE , hsv.z);

            // Point on CB_CA at Saturation position
            Vector3 pt_P = Vector3.Lerp(pt_CB, pt_CA, hsv.y);

            // From P and A, B C, find the barycentric coordinates.
            return GetBarycentricCoordinates2D(pt_P, pt_A_HUE, pt_B_WHITE, pt_C_BLACK);
        }

        private void UpdateCursorPositions()
        {
            float w2 = width / 2.0f;
            float h2 = height / 2.0f;
            float ir = innerCirclePct * w2; // circle inner radius
            float or = outerCirclePct * w2; // circle outer radius
            float mr = (ir + or) / 2.0f; // circle middle radius
            float cw = (or - ir); // circle width
            float tr = trianglePct * w2;
            Vector3 cs = hueCursor.GetComponentInChildren<MeshFilter>().mesh.bounds.size;

            hueCursor.localPosition = new Vector3(
                w2 + mr * -Mathf.Cos(hsv.x * 2.0f * Mathf.PI),
                -h2 + mr * Mathf.Sin(hsv.x * 2.0f * Mathf.PI),
                -cs.z / 2.0f); //-thickness - cs.z/2.0f);

            hueCursor.transform.localRotation = Quaternion.Euler(0,0, 90.0f - hsv.x * 360.0f); // tmp
            hueCursor.localScale = new Vector3(1, cw / cs.y, 1);

            // TODO: cursor in triangle
            // 3 points (A, B, C) = (HUE, WHITE, BLACK)
            //           C
            //          / \
            //         / P \
            //        B-----A
            Vector3 pt_A_HUE = new Vector3(w2 + tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), -thickness);
            Vector3 pt_B_WHITE = new Vector3(w2 - tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), -thickness);
            Vector3 pt_C_BLACK = new Vector3(w2, -h2 + tr, -thickness);

            svCursor.localPosition = pt_A_HUE * barycentric.x + pt_B_WHITE * barycentric.y + pt_C_BLACK * barycentric.z;
            svCursor.transform.localRotation = Quaternion.identity; // tmp
        }

        //private void UpdateSVColor()
        //{
        //    Color baseColor = Color.HSVToRGB(hsv.x, 1f, 1f); // pure hue color
        //    var renderer = GetComponent<MeshRenderer>();
        //    renderer.sharedMaterials[1].SetColor("_Color", baseColor);
        //}

        public void RebuildMesh(float newWidth, float newHeight, float newThickness, float newTrianglePct, float newInnerCirclePct, float newOuterCirclePct)
        {
            Color baseColor = Color.HSVToRGB(hsv.x, 1f, 1f); // pure hue color

            float minSide = Mathf.Min(newWidth, newHeight);
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildHSV(minSide, minSide, newThickness, newTrianglePct, newInnerCirclePct, newOuterCirclePct, 72, baseColor);
            theNewMesh.name = "UIColorPickerHSV_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = minSide;// newWidth;
            height = minSide;// newHeight;
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
            lockedOnCircle = false;
            lockedOnTriangle = false;

            base.OnRayReleaseInside();
            colorPicker.OnRelease();
        }

        public override bool OnRayReleaseOutside()
        {
            lockedOnCircle = false;
            lockedOnTriangle = false;

            return base.OnRayReleaseOutside();
        }

        Vector3 lastProjected;
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
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, -thickness);

            if (IgnoreRayInteraction())
            {
                // return endPoint at the surface of the widget.
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }

            float w2 = width / 2.0f;
            float h2 = height / 2.0f;
            float ir = innerCirclePct * w2; // circle inner radius
            float or = outerCirclePct * w2; // circle outer radius
            float mr = (ir + or) / 2.0f; // circle middle radius
            float cw = (or - ir); // circle width
            float tr = trianglePct * w2;

            Vector2 circleCenter_L = new Vector2(w2, -h2);
            Vector2 cursor_L = new Vector2(localProjectedWidgetPosition.x, localProjectedWidgetPosition.y);

            // Just clicked, find out which subpart to lock on
            if (triggerJustClicked)
            {
                float cursorDistanceFromCenter = Vector2.Distance(cursor_L, circleCenter_L);
                if (cursorDistanceFromCenter >= ir && cursorDistanceFromCenter <= or)
                {
                    lockedOnCircle = true;
                }
                else
                {
                    // TODO: really check for triangle bounds, not only bounding circle.
                    if (cursorDistanceFromCenter <= tr)
                    {
                        lockedOnTriangle = true;
                    }
                }
            }

            if (lockedOnCircle)
            {
                Vector2 cursor_C = cursor_L - circleCenter_L;
                float angle = Mathf.Rad2Deg * (Mathf.PI - Mathf.Atan2(cursor_C.y, cursor_C.x));
                float newHue = angle / 360.0f;

                // DRAG
                if (!triggerJustClicked)
                {
                    float oldHue = hsv.x;
                    if (newHue - oldHue < -0.5f) // ex: 0.9 -> 0.1 ==> 0.9 -> 1.1
                    {
                        newHue = Mathf.Lerp(oldHue, newHue + 1.0f, GlobalState.Settings.RayHueDrag);
                        if (newHue >= 1.0f)
                        {
                            newHue -= 1.0f;
                        }
                    }
                    else if (newHue - oldHue > 0.5f) // ex: 0.1 -> 0.9 ==> 1.1 -> 0.9
                    {
                        newHue = Mathf.Lerp(oldHue + 1.0f, newHue, GlobalState.Settings.RayHueDrag);
                        if (newHue >= 1.0f)
                        {
                            newHue -= 1.0f;
                        }
                    }
                    else // ex: 0.1 -> 0.2
                    {
                        newHue = Mathf.Lerp(oldHue, newHue, GlobalState.Settings.RayHueDrag);
                    }
                }

                HSV = new Vector3(newHue, hsv.y, hsv.z); // NOTE: also re-position the cursors.

                colorPicker.OnColorChanged();

                localProjectedWidgetPosition = new Vector3(
                    w2 + mr * -Mathf.Cos(hsv.x * 2.0f * Mathf.PI),
                    -h2 + mr * Mathf.Sin(hsv.x * 2.0f * Mathf.PI),
                    -thickness);
            }
            else if (lockedOnTriangle)
            {
                Vector3 pt_A_HUE = new Vector3(w2 + tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), -thickness);
                Vector3 pt_B_WHITE = new Vector3(w2 - tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), -thickness);
                Vector3 pt_C_BLACK = new Vector3(w2, -h2 + tr, -thickness);

                Vector3 closest;
                Vector3 baryOfClosest;
                ClosestPointToTriangle2D(localProjectedWidgetPosition, pt_A_HUE, pt_B_WHITE, pt_C_BLACK, out closest, out baryOfClosest);
                localProjectedWidgetPosition = new Vector3(closest.x, closest.y, -thickness);

                // DRAG
                if (!triggerJustClicked)
                {
                    // TODO: apply drag to the barycentric coords as well, otherwise it does not work.
                    //float drag = GlobalState.Settings.RaySliderDrag;
                    //localProjectedWidgetPosition = Vector3.Lerp(lastProjected, localProjectedWidgetPosition, GlobalState.Settings.RaySliderDrag);
                }
                lastProjected = localProjectedWidgetPosition;

                barycentric = baryOfClosest;
                float H, S, V;
                Color.RGBToHSV(BarycentricToRGB(), out H, out S, out V);
                // TODO: make a funciton PointInTriangleToRGB(closest, a, b, c), using the resterizer algo to find color instead of barycentric.
                hsv = new Vector3(hsv.x, S, V);

                UpdateCursorPositions();

                colorPicker.OnColorChanged();
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        // Compute barycentric coordinates (u, v, w) for
        // point p with respect to triangle (a, b, c)
        // BEWARE: Z is ignored, it is a 2D implementation!!!
        Vector3 GetBarycentricCoordinates2D(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float den = v0.x * v1.y - v1.x * v0.y;
            float v = (v2.x * v1.y - v1.x * v2.y) / den;
            float w = (v0.x * v2.y - v2.x * v0.y) / den;
            float u = 1.0f - v - w;
            return new Vector3(u, v, w);
        }
        
        // NOTE: it is not the same color as the one displayed by the shader on the triangle.
        // TODO: use the same algo as in the shadergraph.
        Color BarycentricToRGB()
        {
            // Base pure hue color
            Color baseColor = Color.HSVToRGB(hsv.x, 1f, 1f);
            // Add saturation
            Vector3 rgb = barycentric.x * new Vector3(baseColor.r, baseColor.g, baseColor.b) + barycentric.y * Vector3.one;
            // Add value
            rgb = Vector3.Lerp(Vector3.zero, rgb, 1 - barycentric.z);

            return new Color(rgb.x, rgb.y, rgb.z);
        }

        // From Realtime Collision Detection p.141
        void ClosestPointToTriangle2D(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out Vector3 outClosest, out Vector3 outBary)
        {
            // Check voronoi region outside A
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                outClosest = a;
                outBary = new Vector3(1,0,0);
                return;
            }

            // Check voronoi region outside B
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                outClosest = b;
                outBary = new Vector3(0, 1, 0);
                return;
            }

            // Check edge region of AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                outClosest = a + v * ab;
                outBary = new Vector3(1-v,v,0);
                return;
            }

            // Check voronoi region outside C
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                outClosest = c;
                outBary = new Vector3(0, 0, 1);
                return;
            }

            // Check edge region of AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                outClosest = a + w * ac;
                outBary = new Vector3(1 - w, 0, w);
                return;
            }

            // Check edge region of BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                outClosest = b + w * (c - b);
                outBary = new Vector3(0, 1 - w, w);
                return;
            }

            // P inside the triangle
            {
                float denom = 1.0f / (va + vb + vc);
                float v = vb * denom;
                float w = vc * denom;
                outClosest = a + ab * v + ac * w;
                outBary = new Vector3(1-v-w, v, w);
                return;
            }
        }

        //void ClosestPointToTriangle2D(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out Vector3 closest, out Vector3 baryOfClosest)
        //{
        //    baryOfClosest = GetBarycentricCoordinates2D(p, a, b, c);
        //    float u = baryOfClosest.x;
        //    float v = baryOfClosest.y;
        //    float w = baryOfClosest.z;
        //    if (v >= 0.0f && w >= 0.0f && (v + w) <= 1.0f)
        //    {
        //        closest = p;
        //    }
        //    else
        //    {
        //        // project on edges
        //        closest = p;
        //    }
        //}

        #endregion

        #region create

        public class CreateParams
        {
            public Transform parent = null;
            public string widgetName = UIButton.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, 0);// -default_thickness);
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
                    input.trianglePct, input.innerCirclePct, input.outerCirclePct, 72, Color.red);
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
            hueCursor.transform.localRotation = Quaternion.identity;
            uiColorPickerHSV.hueCursor = hueCursor.transform;

            GameObject svCursor = Instantiate<GameObject>(input.svCursorPrefab);
            svCursor.transform.parent = uiColorPickerHSV.transform;
            svCursor.transform.localPosition = Vector3.zero;
            svCursor.transform.localRotation = Quaternion.identity;
            uiColorPickerHSV.svCursor = svCursor.transform;

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiColorPickerHSV;
        }

        #endregion
    }
}
