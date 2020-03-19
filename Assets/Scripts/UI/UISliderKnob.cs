using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UISliderKnob : MonoBehaviour
    {
        public float radius;
        public float depth;

        private Color _color;
        public Color Color { get { return _color; } set { _color = value; ApplyColor(_color); } }

        public void RebuildMesh(float newKnobRadius, float newKnobDepth)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            // Make a cylinder using RoundedBox
            Mesh theNewMesh = UIUtils.BuildRoundedBox(2.0f * newKnobRadius, 2.0f * newKnobRadius, newKnobRadius, newKnobDepth);
            theNewMesh.name = "UISliderKnob_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            radius = newKnobRadius;
            depth = newKnobDepth;
        }

        private void ApplyColor(Color c)
        {
            GetComponent<MeshRenderer>().material.SetColor("_BaseColor", c);
        }

        public static UISliderKnob CreateUISliderKnob(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float head_radius,
            float head_depth,
            Material material,
            Color c)
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

            UISliderKnob uiSliderKnob = go.AddComponent<UISliderKnob>();
            uiSliderKnob.transform.parent = parent;
            uiSliderKnob.transform.localPosition = parentAnchor + relativeLocation;
            uiSliderKnob.transform.localRotation = Quaternion.identity;
            uiSliderKnob.transform.localScale = Vector3.one;
            uiSliderKnob.radius = head_radius;
            uiSliderKnob.depth = head_depth;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(2.0f * head_radius, 2.0f * head_radius, head_radius, head_depth);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(material);
                uiSliderKnob.Color = c;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderKnob;
        }
    }
}
