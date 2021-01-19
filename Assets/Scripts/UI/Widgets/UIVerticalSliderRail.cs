using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIVerticalSliderRail : MonoBehaviour
    {
        public float width;
        public float height;
        public float thickness;
        public float margin;

        public ColorReference _color = new ColorReference();
        public Color Color { get { return _color.Value; } set { _color.Value = value; ResetColor(); } }

        void Awake()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness, float newMargin)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
            theNewMesh.name = "UISliderRail_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;
            margin = newMargin;
        }

        public void ResetColor()
        {
            SetColor(Color);
        }

        private void SetColor(Color c)
        {
            GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", c);
        }

        public class CreateArgs
        {
            public Transform parent;
            public string widgetName;
            public Vector3 relativeLocation;
            public float width;
            public float height;
            public float thickness;
            public float margin;
            public Material material;
            public ColorVar c = UIOptions.SliderRailColorVar;
        }

        public static UIVerticalSliderRail Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";
            go.layer = LayerMask.NameToLayer("CameraHidden");

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

            UIVerticalSliderRail uiSliderRail = go.AddComponent<UIVerticalSliderRail>();
            uiSliderRail.transform.parent = input.parent;
            uiSliderRail.transform.localPosition = parentAnchor + input.relativeLocation;
            uiSliderRail.transform.localRotation = Quaternion.identity;
            uiSliderRail.transform.localScale = Vector3.one;
            uiSliderRail.width = input.width;
            uiSliderRail.height = input.height;
            uiSliderRail.thickness = input.thickness;
            uiSliderRail.margin = input.margin;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                Material newMaterial = Instantiate(input.material);
                newMaterial.name = "UIVerticalSliderRail_Material";
                meshRenderer.sharedMaterial = newMaterial;

                uiSliderRail._color.useConstant = false;
                uiSliderRail._color.constant = input.c.value;
                uiSliderRail._color.reference = input.c;
                meshRenderer.sharedMaterial.SetColor("_BaseColor", uiSliderRail.Color);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderRail;
        }
    }
}
