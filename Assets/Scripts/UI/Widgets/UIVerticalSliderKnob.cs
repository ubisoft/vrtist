/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIVerticalSliderKnob : MonoBehaviour
    {
        // TODO: Add Canvas and Text

        public float radius;
        public float depth;

        public ColorReference _color = new ColorReference();
        public Color Color { get { return _color.Value; } set { _color.Value = value; ResetColor(); } }

        public void RebuildMesh(float newKnobRadius, float newKnobDepth)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            // Make a cylinder using RoundedBox
            Mesh theNewMesh = UIUtils.BuildRoundedBox(2.0f * newKnobRadius, 2.0f * newKnobRadius, newKnobRadius, newKnobDepth);
            theNewMesh.name = "UIVerticalSliderKnob_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            radius = newKnobRadius;
            depth = newKnobDepth;
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
            public float radius;
            public float depth;
            public Material material;
            public ColorVar c = UIOptions.SliderKnobColorVar;
        }

        public static UIVerticalSliderKnob Create(CreateArgs input)
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

            UIVerticalSliderKnob uiSliderKnob = go.AddComponent<UIVerticalSliderKnob>();
            uiSliderKnob.transform.parent = input.parent;
            uiSliderKnob.transform.localPosition = parentAnchor + input.relativeLocation;
            uiSliderKnob.transform.localRotation = Quaternion.identity;
            uiSliderKnob.transform.localScale = Vector3.one;
            uiSliderKnob.radius = input.radius;
            uiSliderKnob.depth = input.depth;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(2.0f * input.radius, 2.0f * input.radius, input.radius, input.depth);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && input.material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(input.material);
                uiSliderKnob._color.useConstant = false;
                uiSliderKnob._color.reference = input.c;
                meshRenderer.sharedMaterial.SetColor("_BaseColor", uiSliderKnob.Color);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderKnob;
        }
    }
}
