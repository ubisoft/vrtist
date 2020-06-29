using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIPanel : UIElement
    {
        public enum BackgroundGeometryStyle { Tube, Flat };

        private static readonly string default_widget_name = "New Panel";
        private static readonly float default_width = 0.4f;
        private static readonly float default_height = 0.6f;
        private static readonly float default_margin = 0.02f;
        private static readonly float default_radius = 0.01f;
        private static readonly float default_thickness = 0.001f;
        private static readonly UIPanel.BackgroundGeometryStyle default_bg_geom_style = UIPanel.BackgroundGeometryStyle.Flat;
        public static readonly string default_material_name = "UIPanel";
        public static readonly Color default_color = UIElement.default_background_color;

        [SpaceHeader("Panel Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float radius = default_radius;
        [CentimeterFloat] public float thickness = default_thickness;
        public BackgroundGeometryStyle backgroundGeometryStyle = default_bg_geom_style;
        public Material source_material = null;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int circleSubdiv = 8;
        public int nbSubdivPerUnit = 1;
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        private bool needRebuild = false;

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_radius = 0.001f;
            const int min_circleSubdiv = 3;
            const int min_nbSubdivPerUnit = 1;
            const int min_nbSubdivCornerFixed = 1;
            const int min_nbSubdivCornerPerUnit = 1;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (radius < min_radius)
                radius = min_radius;
            if (margin > width / 2.0f || margin > height / 2.0f)
                margin = Mathf.Min(width / 2.0f, height / 2.0f);
            if (radius > width / 2.0f || radius > height / 2.0f)
                radius = Mathf.Min(width / 2.0f, height / 2.0f);
            if (margin < radius)
                margin = radius;
            if (circleSubdiv < min_circleSubdiv)
                circleSubdiv = min_circleSubdiv;
            if (nbSubdivPerUnit < min_nbSubdivPerUnit)
                nbSubdivPerUnit = min_nbSubdivPerUnit;
            if (nbSubdivCornerFixed < min_nbSubdivCornerFixed)
                nbSubdivCornerFixed = min_nbSubdivCornerFixed;
            if (nbSubdivCornerPerUnit < min_nbSubdivCornerPerUnit)
                nbSubdivCornerPerUnit = min_nbSubdivCornerPerUnit;

            // Realign button to parent anchor if we change the thickness.
            if (-thickness != relativeLocation.z)
                relativeLocation.z = -thickness;

            needRebuild = true;

            // NOTE: RebuildMesh() cannot be called in OnValidate().
        }

        private void Update()
        {
            if (needRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                SetColor(Disabled ? disabledColor : baseColor);
                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(margin + radius, -margin - radius, 0.0f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, 0));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, 0));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(+margin, -height + margin, 0));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, 0));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = (backgroundGeometryStyle == BackgroundGeometryStyle.Tube)
                ? UIUtils.BuildRoundedRectTubeEx(
                    width, height, margin, radius,
                    circleSubdiv, nbSubdivPerUnit, nbSubdivCornerFixed, nbSubdivCornerPerUnit)
                : UIUtils.BuildRoundedBoxEx(
                    width, height, margin, thickness,
                    nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UIPanel_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;
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
                sharedMaterialInstance.name = "UIPanel_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }






        public class CreatePanelParams
        {
            public Transform parent = null;
            public string widgetName = UIPanel.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIPanel.default_thickness);
            public float width = UIPanel.default_width;
            public float height = UIPanel.default_height;
            public float margin = UIPanel.default_margin;
            public float thickness = UIPanel.default_thickness;
            public float radius = UIPanel.default_radius;
            public UIPanel.BackgroundGeometryStyle backgroundGeometryStyle = UIPanel.default_bg_geom_style;
            public Material material = UIUtils.LoadMaterial(UIPanel.default_material_name);
            public Color color = UIPanel.default_color;
        }


        public static void Create(CreatePanelParams input)
        {
            GameObject go = new GameObject(input.widgetName);

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

            UIPanel uiPanel = go.AddComponent<UIPanel>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiPanel.relativeLocation = input.relativeLocation;
            uiPanel.transform.parent = input.parent;
            uiPanel.transform.localPosition = parentAnchor + input.relativeLocation;
            uiPanel.transform.localRotation = Quaternion.identity;
            uiPanel.transform.localScale = Vector3.one;
            uiPanel.width = input.width;
            uiPanel.height = input.height;
            uiPanel.margin = input.margin;
            uiPanel.radius = input.radius;
            uiPanel.thickness = input.thickness;
            uiPanel.backgroundGeometryStyle = input.backgroundGeometryStyle;
            uiPanel.source_material = input.material;

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = (uiPanel.backgroundGeometryStyle == BackgroundGeometryStyle.Tube)
                ? UIUtils.BuildRoundedRectTube(input.width, input.height, input.margin, input.radius)
                : UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiPanel.Anchor = Vector3.zero; // TODO: thickness goes +Z and Anchor stays zero? or thickness goes -Z and Anchor follows the surface?
            }

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(input.material);
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiPanel.BaseColor = input.color;
            }
        }
    }
}
