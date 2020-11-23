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
        [SerializeField, HideInInspector]
        Shader fullscreenOutlineShader;

        Material fullscreenOutlineMaterial;
        MaterialPropertyBlock outlineProperties;
        ShaderTagId[] shaderTags;
        RTHandle outlineBuffer;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            fullscreenOutlineShader = Shader.Find("VRtist/SelectionCustomPass_FullscreenOutlineShader_2");
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

        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
        {
            // 1) Draw meshes with a special override material
            DrawOutlineMeshes(renderContext, cmd, camera, cullingResult);

            // 2) Do a fullscreen pass to draw the outline from previous pass.
            SetCameraRenderTarget(cmd);
            outlineProperties.SetColor("_OutlineColor", outlineColor);
            outlineProperties.SetTexture("_OutlineBuffer", outlineBuffer);
            outlineProperties.SetFloat("_Threshold", threshold);
            CoreUtils.DrawFullScreen(cmd, fullscreenOutlineMaterial, outlineProperties, shaderPassId: 0);
        }

        void DrawOutlineMeshes(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            // TODO: see how we can render objects without lights here.
            // Their rendered color is compared to black to find the outline... black on black...

            var result = new RendererListDesc(shaderTags, cullingResult, hdCamera.camera)
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

            CoreUtils.SetRenderTarget(cmd, outlineBuffer, ClearFlag.Color,
                new Color(99.0f, 99.0f, 99.0f) // clear target with a big number, hopefully bigger than anything. Next compare if <, instead of >
                );
            HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
        }
    }
}
