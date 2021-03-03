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
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// NOTE: (nico) found this here
// https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Custom-Pass.html
//

namespace VRtist
{
    class SelectionCustomPass : CustomPass
    {
        public LayerMask outlineLayer = 0;
        [ColorUsage(false, true)]
        public Color outlineColor = Color.black;
        public float threshold = 80;
        public Material overrideMaterial = null;
        public string overrideMaterialPassName = null;
        private int overrideMaterialPassIndex = -1;

        // To make sure the shader will ends up in the build, we keep it's reference in the custom pass
        [SerializeField]
        Shader fullscreenOutlineShader = null;

        Material fullscreenOutlineMaterial;
        MaterialPropertyBlock outlineProperties;
        ShaderTagId[] shaderTags;
        RTHandle outlineBuffer;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (null == fullscreenOutlineShader)
                fullscreenOutlineShader = Shader.Find("VRtist/SelectionCustomPass_FullscreenOutlineShader_Thin");
            fullscreenOutlineMaterial = CoreUtils.CreateEngineMaterial(fullscreenOutlineShader);
            outlineProperties = new MaterialPropertyBlock();

            if (overrideMaterial != null)
            {
                overrideMaterialPassIndex = overrideMaterial.FindPass(overrideMaterialPassName);
            }

            // List all the materials that will be replaced in the frame

            // TODO: est-ce qu'il faut en inclure plus??
            shaderTags = new ShaderTagId[3]
            {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            };

            outlineBuffer = RTHandles.Alloc(
                Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                useDynamicScale: true, name: "Outline Buffer"
            );
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(fullscreenOutlineMaterial);
            outlineBuffer.Release();
        }

        protected override void Execute(CustomPassContext ctx)
        {
            // 1) Draw meshes with a special override material
            DrawOutlineMeshes(ctx);

            // 2) Do a fullscreen pass to draw the outline from previous pass.
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer);
            outlineProperties.SetColor("_OutlineColor", outlineColor);
            outlineProperties.SetTexture("_OutlineBuffer", outlineBuffer);
            outlineProperties.SetFloat("_Threshold", threshold);
            CoreUtils.DrawFullScreen(ctx.cmd, fullscreenOutlineMaterial, outlineProperties, shaderPassId: 0);
        }

        void DrawOutlineMeshes(CustomPassContext ctx)
        {
            // TODO: see how we can render objects without lights here.
            // Their rendered color is compared to black to find the outline... black on black...

            var result = new RendererListDesc(shaderTags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                // We need the lighting render configuration to support rendering lit objects
                rendererConfiguration = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = outlineLayer,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = overrideMaterialPassIndex
            };

            CoreUtils.SetRenderTarget(ctx.cmd, outlineBuffer, ClearFlag.Color,
                new Color(99.0f, 99.0f, 99.0f) // clear target with a big number, hopefully bigger than anything. Next compare if <, instead of >
                );
            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }
    }
}
