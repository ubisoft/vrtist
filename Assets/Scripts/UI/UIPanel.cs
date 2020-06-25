using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIPanel : UIElement
    {
        [SpaceHeader("Panel Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = 0.02f;
        [CentimeterFloat] public float radius = 0.01f;
        [CentimeterFloat] public float thickness = 0.001f;
        public enum BackgroundGeometryStyle { Tube, Flat };
        public BackgroundGeometryStyle backgroundGeometryStyle = BackgroundGeometryStyle.Tube;

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

                Material material = UIUtils.LoadMaterial("UIElementTransparent");
                Material materialInstance = Instantiate(material);
                
                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIPanel_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        public static void Create(
            string panelName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            float radius,
            float thickness,
            BackgroundGeometryStyle backgroundGeometryStyle,
            Material material,
            Color color)
        {
            GameObject go = new GameObject(panelName);

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

            UIPanel uiPanel = go.AddComponent<UIPanel>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiPanel.relativeLocation = relativeLocation;
            uiPanel.transform.parent = parent;
            uiPanel.transform.localPosition = parentAnchor + relativeLocation;
            uiPanel.transform.localRotation = Quaternion.identity;
            uiPanel.transform.localScale = Vector3.one;
            uiPanel.width = width;
            uiPanel.height = height;
            uiPanel.margin = margin;
            uiPanel.radius = radius;
            uiPanel.thickness = thickness;
            uiPanel.backgroundGeometryStyle = backgroundGeometryStyle;

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = (uiPanel.backgroundGeometryStyle == BackgroundGeometryStyle.Tube)
                ? UIUtils.BuildRoundedRectTube(width, height, margin, radius)
                : UIUtils.BuildRoundedBox(width, height, margin, thickness);
                uiPanel.Anchor = Vector3.zero; // TODO: thickness goes +Z and Anchor stays zero? or thickness goes -Z and Anchor follows the surface?
            }

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(material);
                Material sharedMaterial = meshRenderer.sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                sharedMaterial.SetColor("_BaseColor", color);
            }
        }
    }
}
