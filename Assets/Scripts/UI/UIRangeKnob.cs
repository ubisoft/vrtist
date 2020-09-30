using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIRangeKnob : MonoBehaviour
    {
        public float width;
        public float radius;
        public float depth;

        public ColorReference baseColor = new ColorReference();
        public ColorReference pushedColor = new ColorReference();
        public ColorReference hoveredColor = new ColorReference();

        private bool isPushed = false;
        private bool isHovered = false;

        public Color BaseColor { get { return baseColor.Value; } }
        public Color PushedColor { get { return pushedColor.Value; } }
        public Color HoveredColor { get { return hoveredColor.Value; } }

        public bool Pushed { get { return isPushed; } set { isPushed = value; ResetColor(); } }
        public bool Hovered { get { return isHovered; } set { isHovered = value; ResetColor(); } }

        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        public void RebuildMesh(float newWidth, float newKnobRadius, float newKnobDepth, int knobNbSubdivCornerFixed, int knobNbSubdivCornerPerUnit)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            // Make a cylinder using RoundedBox
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(newWidth, 2.0f * newKnobRadius, newKnobRadius, newKnobDepth, knobNbSubdivCornerFixed, knobNbSubdivCornerPerUnit);
            theNewMesh.name = "UIRangeKnob_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            radius = newKnobRadius;
            depth = newKnobDepth;
            nbSubdivCornerFixed = knobNbSubdivCornerFixed;
            nbSubdivCornerPerUnit = knobNbSubdivCornerPerUnit;
        }

        public void ResetColor()
        {
            SetColor( isPushed ? PushedColor
                  : ( isHovered ? HoveredColor
                  : BaseColor));
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
            public float width = 0.0f;
            public float radius;
            public float depth;
            public int nbSubdivCornerFixed = 3;
            public int nbSubdivCornerPerUnit = 3;
            public Material material;
            public ColorVar baseColor = UIOptions.SliderKnobColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar hoveredColor = UIOptions.SelectedColorVar;
        }

        public static UIRangeKnob Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";
            go.layer = LayerMask.NameToLayer("UI");

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

            UIRangeKnob uiRangeKnob = go.AddComponent<UIRangeKnob>();
            uiRangeKnob.transform.parent = input.parent;
            uiRangeKnob.transform.localPosition = parentAnchor + input.relativeLocation;
            uiRangeKnob.transform.localRotation = Quaternion.identity;
            uiRangeKnob.transform.localScale = Vector3.one;
            uiRangeKnob.width = input.width;
            uiRangeKnob.radius = input.radius;
            uiRangeKnob.depth = input.depth;
            uiRangeKnob.nbSubdivCornerFixed = input.nbSubdivCornerFixed;
            uiRangeKnob.nbSubdivCornerPerUnit = input.nbSubdivCornerPerUnit;
            uiRangeKnob.baseColor.useConstant = false;
            uiRangeKnob.baseColor.reference = input.baseColor;
            uiRangeKnob.pushedColor.useConstant = false;
            uiRangeKnob.pushedColor.reference = input.pushedColor;
            uiRangeKnob.hoveredColor.useConstant = false;
            uiRangeKnob.hoveredColor.reference = input.hoveredColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBoxEx(input.width, 2.0f * input.radius, input.radius, input.depth, input.nbSubdivCornerFixed, input.nbSubdivCornerPerUnit);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(input.material);
                meshRenderer.sharedMaterial.SetColor("_BaseColor", uiRangeKnob.BaseColor);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiRangeKnob;
        }
    }
}
