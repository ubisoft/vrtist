using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIGradientPreview : UIElement
    {
        public static readonly string default_widget_name = "New Gradient";
        public static readonly float default_width = 0.30f;
        public static readonly float default_height = 0.04f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly string default_material_name = "SkyboxPreviewMaterial";

        private float thickness = 1.0f;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();

        private SkySettings colors = new SkySettings() { topColor = Color.red, middleColor = Color.blue, bottomColor = Color.yellow };
        public SkySettings Colors { get { return colors; } set { colors = value; ResetColor(); } }

        private void OnValidate()
        {
            // Realign button to parent anchor if we change the thickness.
            if (-thickness != relativeLocation.z)
                relativeLocation.z = -thickness;

            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                ResetColor();
                NeedsRebuild = false;
            }
        }

        public override void ResetColor()
        {
            // Set colors in shader
            Material material = GetComponent<MeshRenderer>()?.sharedMaterial;
            if (null != material)
            {
                material.SetColor("_TopColor", colors.topColor);
                material.SetColor("_MiddleColor", colors.middleColor);
                material.SetColor("_BottomColor", colors.bottomColor);
            }
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh[] meshes = Resources.LoadAll<Mesh>("Models/half_sphere");
            Mesh theNewMesh = meshes[0];
            theNewMesh.name = "UIGradientPreview_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;

            Vector3 originalSize = meshFilter.sharedMesh.bounds.size; // 1.997, 1, 1.997
            float sc = newWidth / originalSize.x; // 0.06 = 0.12 / 1.997

            transform.localPosition = new Vector3(width / 2.0f, -height / 2.0f, 0.0f);
            transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 180.0f);
            transform.localScale = new Vector3(sc, sc / 2.0f, sc);

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

        #endregion

        #region create

        public class CreateParams
        {
            public Transform parent = null;
            public string widgetName = UIButton.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -default_thickness);
            public float width = default_width;
            public float height = default_height;
            public float margin = default_margin;
            public float thickness = default_thickness;
            public Material material = UIUtils.LoadMaterial(default_material_name);
            public ColorVar bgcolor = UIOptions.BackgroundColorVar;
            public ColorVar fgcolor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public ColorVar checkedColor = UIOptions.CheckedColorVar;
        }

        public static UIGradientPreview Create(CreateParams input)
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

            UIGradientPreview uiGradientWidget = go.AddComponent<UIGradientPreview>();
            uiGradientWidget.relativeLocation = input.relativeLocation;
            uiGradientWidget.transform.parent = input.parent;
            uiGradientWidget.transform.localPosition = parentAnchor + input.relativeLocation;
            uiGradientWidget.transform.localRotation = Quaternion.identity;
            uiGradientWidget.transform.localScale = Vector3.one;
            uiGradientWidget.width = input.width;
            uiGradientWidget.height = input.height;
            uiGradientWidget.thickness = input.thickness;

            // TODO: put the mesh in a sub object to apply a transformation that wont perturb the base object.

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh[] meshes = Resources.LoadAll<Mesh>("Models/half_sphere");
                Mesh theNewMesh = meshes[0];
                meshFilter.sharedMesh = meshes[0];

                Vector3 originalSize = meshFilter.sharedMesh.bounds.size; // 1.997, 1, 1.997
                float sc = input.width / originalSize.x; // 0.06 = 0.12 / 1.997

                //transform.localPosition = new Vector3(width / 2.0f, -height / 2.0f, 0.0f);
                uiGradientWidget.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 180.0f);
                uiGradientWidget.transform.localScale = new Vector3(sc, sc / 2.0f, sc);

                uiGradientWidget.Anchor = Vector3.zero;
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
            if (meshRenderer != null && input.material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(input.material);
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 1 << 1; // "LightLayer 1"
            }

            UIUtils.SetRecursiveLayer(go, "UI");

            return uiGradientWidget;
        }

        #endregion
    }
}
