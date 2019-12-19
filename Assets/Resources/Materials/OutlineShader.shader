Shader "VRtist/OutlineShader"
{
	Properties
	{
		_OutlineColor("Outline Color", Color) = (1,0.6,0,1)
		//_Outline("Outline width", Range(0.0, 0.03)) = .005
		_Outline("Outline width", Range(0.0, 10)) = 1

		//[Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase("UV Set for base", Float) = 0
	}

	HLSLINCLUDE

#pragma target 4.5
#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

//-------------------------------------------------------------------------------------
// Variant
//-------------------------------------------------------------------------------------

#pragma shader_feature_local _ALPHATEST_ON
#pragma shader_feature_local _DEPTHOFFSET_ON
#pragma shader_feature_local _DOUBLESIDED_ON
#pragma shader_feature_local _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
#pragma shader_feature_local _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
#pragma shader_feature_local _DISPLACEMENT_LOCK_TILING_SCALE
#pragma shader_feature_local _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
#pragma shader_feature_local _ _REFRACTION_PLANE _REFRACTION_SPHERE

#pragma shader_feature_local _ _EMISSIVE_MAPPING_PLANAR _EMISSIVE_MAPPING_TRIPLANAR
#pragma shader_feature_local _ _MAPPING_PLANAR _MAPPING_TRIPLANAR
#pragma shader_feature_local _NORMALMAP_TANGENT_SPACE
#pragma shader_feature_local _ _REQUIRE_UV2 _REQUIRE_UV3

#pragma shader_feature_local _NORMALMAP
#pragma shader_feature_local _MASKMAP
#pragma shader_feature_local _BENTNORMALMAP
#pragma shader_feature_local _EMISSIVE_COLOR_MAP

// _ENABLESPECULAROCCLUSION keyword is obsolete but keep here for compatibility. Do not used
// _ENABLESPECULAROCCLUSION and _SPECULAR_OCCLUSION_X can't exist at the same time (the new _SPECULAR_OCCLUSION replace it)
// When _ENABLESPECULAROCCLUSION is found we define _SPECULAR_OCCLUSION_X so new code to work
#pragma shader_feature_local _ENABLESPECULAROCCLUSION
#pragma shader_feature_local _ _SPECULAR_OCCLUSION_NONE _SPECULAR_OCCLUSION_FROM_BENT_NORMAL_MAP
#ifdef _ENABLESPECULAROCCLUSION
#define _SPECULAR_OCCLUSION_FROM_BENT_NORMAL_MAP
#endif

#pragma shader_feature_local _HEIGHTMAP
#pragma shader_feature_local _TANGENTMAP
#pragma shader_feature_local _ANISOTROPYMAP
#pragma shader_feature_local _DETAIL_MAP
#pragma shader_feature_local _SUBSURFACE_MASK_MAP
#pragma shader_feature_local _THICKNESSMAP
#pragma shader_feature_local _IRIDESCENCE_THICKNESSMAP
#pragma shader_feature_local _SPECULARCOLORMAP
#pragma shader_feature_local _TRANSMITTANCECOLORMAP

#pragma shader_feature_local _DISABLE_DECALS
#pragma shader_feature_local _DISABLE_SSR
#pragma shader_feature_local _ENABLE_GEOMETRIC_SPECULAR_AA

// Keyword for transparent
#pragma shader_feature _SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
#pragma shader_feature_local _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
#pragma shader_feature_local _ENABLE_FOG_ON_TRANSPARENT
#pragma shader_feature_local _TRANSPARENT_WRITES_MOTION_VEC

// MaterialFeature are used as shader feature to allow compiler to optimize properly
#pragma shader_feature_local _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
#pragma shader_feature_local _MATERIAL_FEATURE_TRANSMISSION
#pragma shader_feature_local _MATERIAL_FEATURE_ANISOTROPY
#pragma shader_feature_local _MATERIAL_FEATURE_CLEAR_COAT
#pragma shader_feature_local _MATERIAL_FEATURE_IRIDESCENCE
#pragma shader_feature_local _MATERIAL_FEATURE_SPECULAR_COLOR

#pragma shader_feature_local _ADD_PRECOMPUTED_VELOCITY

// enable dithering LOD crossfade
#pragma multi_compile _ LOD_FADE_CROSSFADE

//enable GPU instancing support
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer

//-------------------------------------------------------------------------------------
// Define
//-------------------------------------------------------------------------------------

// This shader support vertex modification
#define HAVE_VERTEX_MODIFICATION

// If we use subsurface scattering, enable output split lighting (for forward pass)
#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
#define OUTPUT_SPLIT_LIGHTING
#endif

#if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
#define _WRITE_TRANSPARENT_MOTION_VECTOR
#endif
//-------------------------------------------------------------------------------------
// Include
//-------------------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

//-------------------------------------------------------------------------------------
// variable declaration
//-------------------------------------------------------------------------------------

// #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitProperties.hlsl"

// TODO:
// Currently, Lit.hlsl and LitData.hlsl are included for every pass. Split Lit.hlsl in two:
// LitData.hlsl and LitShading.hlsl (merge into the existing LitData.hlsl).
// LitData.hlsl should be responsible for preparing shading parameters.
// LitShading.hlsl implements the light loop API.
// LitData.hlsl is included here, LitShading.hlsl is included below for shading passes only.

ENDHLSL

SubShader
{
	//Tags{  }
	Tags { "Queue" = "Transparent" "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDLitShader"}

	Pass
	{
		Name "BASE"
		//Tags { "LightMode" = "TransparentDepthPostpass" }
		Tags{ "LightMode" = "TransparentDepthPrepass" }
		Cull Back
		ZWrite On
		ColorMask 0
		//Blend Zero One
	}

	Pass
	{
		Name "Outline"
		Tags { "LightMode" = "Forward" }
		//Tags { "LightMode" = "TransparentBackface" }
		//Tags { "LightMode" = "TransparentDepthPostpass" }
		//Tags { "LightMode" = "GBuffer" }
		//Tags { "LightMode" = "SceneSelectionPass" } // <-- overrides the selection outline in the editor!!!
		Cull Front
		Blend One OneMinusDstColor // Soft Additive

		HLSLPROGRAM
		#pragma vertex UnlitContourVertex
		#pragma fragment UnlitContourFragment
		#include "OutlinePass.hlsl"
		ENDHLSL
	}
	/*
	Pass
	{
		Name "BASE"
		Cull Back
		Blend Zero One

		HLSLPROGRAM

//		#define SHADERPASS SHADERPASS_DEPTH_ONLY
//		#define SCENESELECTIONPASS // This will drive the output of the scene selection shader
//		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
//		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
//		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
//		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
//		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

		//#pragma vertex UnlitContourVertex
		//#pragma fragment UnlitContourFragment
		//#include "OutlinePass.hlsl"

		ENDHLSL
	}

	Pass
	{
		Name "OUTLINE"
		Tags { "LightMode" = "Always" }
		Cull Front
		
		// you can choose what kind of blending mode you want for the outline
		//Blend SrcAlpha OneMinusSrcAlpha // Normal
		//Blend One One // Additive
		Blend One OneMinusDstColor // Soft Additive
		//Blend DstColor Zero // Multiplicative
		//Blend DstColor SrcColor // 2x Multiplicative

		HLSLPROGRAM
		#pragma vertex UnlitContourVertex
		#pragma fragment UnlitContourFragment
		#include "OutlinePass.hlsl"
		ENDHLSL
	}
	*/
}

/*

SubShader
{
	// This tags allow to use the shader replacement features
	Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDLitShader" }

	Pass
	{
		Name "SceneSelectionPass"
		Tags { "LightMode" = "SceneSelectionPass" }

		Cull Off

		HLSLPROGRAM

		// Note: Require _ObjectId and _PassValue variables

		// We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
		#define SHADERPASS SHADERPASS_DEPTH_ONLY
		#define SCENESELECTIONPASS // This will drive the output of the scene selection shader
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		#pragma editor_sync_compilation

		ENDHLSL
	}

	// Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not bethe  meta pass.
	Pass
	{
		Name "GBuffer"
		Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

		Cull[_CullMode]
		ZTest[_ZTestGBuffer]

		Stencil
		{
			WriteMask[_StencilWriteMaskGBuffer]
			Ref[_StencilRefGBuffer]
			Comp Always
			Pass Replace
		}

		HLSLPROGRAM

		#pragma multi_compile _ DEBUG_DISPLAY
		#pragma multi_compile _ LIGHTMAP_ON
		#pragma multi_compile _ DIRLIGHTMAP_COMBINED
		#pragma multi_compile _ DYNAMICLIGHTMAP_ON
		#pragma multi_compile _ SHADOWS_SHADOWMASK
		// Setup DECALS_OFF so the shader stripper can remove variants
		#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
		#pragma multi_compile _ LIGHT_LAYERS

#ifndef DEBUG_DISPLAY
		// When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
		// Don't do it with debug display mode as it is possible there is no depth prepass in this case
		#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
#endif

		#define SHADERPASS SHADERPASS_GBUFFER
		#ifdef DEBUG_DISPLAY
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
		#endif
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"

		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	// Extracts information for lightmapping, GI (emission, albedo, ...)
	// This pass it not used during regular rendering.
	Pass
	{
		Name "META"
		Tags{ "LightMode" = "META" }

		Cull Off

		HLSLPROGRAM

		// Lightmap memo
		// DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
		// both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

		#define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "ShadowCaster"
		Tags{ "LightMode" = "ShadowCaster" }

		Cull[_CullMode]

		ZClip[_ZClip]
		ZWrite On
		ZTest LEqual

		ColorMask 0

		HLSLPROGRAM

		#define SHADERPASS SHADERPASS_SHADOWS
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "DepthOnly"
		Tags{ "LightMode" = "DepthOnly" }

		Cull[_CullMode]

		// To be able to tag stencil with disableSSR information for forward
		Stencil
		{
			WriteMask[_StencilWriteMaskDepth]
			Ref[_StencilRefDepth]
			Comp Always
			Pass Replace
		}

		ZWrite On

		HLSLPROGRAM

		// In deferred, depth only pass don't output anything.
		// In forward it output the normal buffer
		#pragma multi_compile _ WRITE_NORMAL_BUFFER
		#pragma multi_compile _ WRITE_MSAA_DEPTH

		#define SHADERPASS SHADERPASS_DEPTH_ONLY
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

		#ifdef WRITE_NORMAL_BUFFER // If enabled we need all regular interpolator
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
		#else
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
		#endif

		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "MotionVectors"
		Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

		// If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity
		Stencil
		{
			WriteMask[_StencilWriteMaskMV]
			Ref[_StencilRefMV]
			Comp Always
			Pass Replace
		}

		Cull[_CullMode]

		ZWrite On

		HLSLPROGRAM
		#pragma multi_compile _ WRITE_NORMAL_BUFFER
		#pragma multi_compile _ WRITE_MSAA_DEPTH

		#define SHADERPASS SHADERPASS_MOTION_VECTORS
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#ifdef WRITE_NORMAL_BUFFER // If enabled we need all regular interpolator
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
		#else
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitMotionVectorPass.hlsl"
		#endif
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "DistortionVectors"
		Tags { "LightMode" = "DistortionVectors" } // This will be only for transparent object based on the RenderQueue index

		Stencil
		{
			WriteMask[_StencilRefDistortionVec]
			Ref[_StencilRefDistortionVec]
			Comp Always
			Pass Replace
		}

		Blend[_DistortionSrcBlend][_DistortionDstBlend],[_DistortionBlurSrcBlend][_DistortionBlurDstBlend]
		BlendOp Add,[_DistortionBlurBlendOp]
		ZTest[_ZTestModeDistortion]
		ZWrite off
		Cull[_CullMode]

		HLSLPROGRAM

		#define SHADERPASS SHADERPASS_DISTORTION
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDistortionPass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "TransparentDepthPrepass"
		Tags{ "LightMode" = "TransparentDepthPrepass" }

		Cull[_CullMode]
		ZWrite On
		ColorMask 0

		HLSLPROGRAM

		#define SHADERPASS SHADERPASS_DEPTH_ONLY
		#define CUTOFF_TRANSPARENT_DEPTH_PREPASS
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	// Caution: Order is important: TransparentBackface, then Forward/ForwardOnly
	Pass
	{
		Name "TransparentBackface"
		Tags { "LightMode" = "TransparentBackface" }

		Blend[_SrcBlend][_DstBlend],[_AlphaSrcBlend][_AlphaDstBlend]
		ZWrite[_ZWrite]
		Cull Front
		ColorMask[_ColorMaskTransparentVel] 1
		ZTest[_ZTestTransparent]

		HLSLPROGRAM

		#pragma multi_compile _ DEBUG_DISPLAY
		#pragma multi_compile _ LIGHTMAP_ON
		#pragma multi_compile _ DIRLIGHTMAP_COMBINED
		#pragma multi_compile _ DYNAMICLIGHTMAP_ON
		#pragma multi_compile _ SHADOWS_SHADOWMASK
		// Setup DECALS_OFF so the shader stripper can remove variants
		#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT

		// Supported shadow modes per light type
		#pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH

		#define USE_CLUSTERED_LIGHTLIST // There is not FPTL lighting when using transparent

		#define SHADERPASS SHADERPASS_FORWARD
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

#ifdef DEBUG_DISPLAY
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif

		// The light loop (or lighting architecture) is in charge to:
		// - Define light list
		// - Define the light loop
		// - Setup the constant/data
		// - Do the reflection hierarchy
		// - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

		#define HAS_LIGHTLOOP

		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "Forward"
		Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

		Stencil
		{
			WriteMask[_StencilWriteMask]
			Ref[_StencilRef]
			Comp Always
			Pass Replace
		}

		Blend[_SrcBlend][_DstBlend],[_AlphaSrcBlend][_AlphaDstBlend]
		// In case of forward we want to have depth equal for opaque mesh
		ZTest[_ZTestDepthEqualForOpaque]
		ZWrite[_ZWrite]
		Cull[_CullModeForward]
		ColorMask[_ColorMaskTransparentVel] 1

		HLSLPROGRAM

		#pragma multi_compile _ DEBUG_DISPLAY
		#pragma multi_compile _ LIGHTMAP_ON
		#pragma multi_compile _ DIRLIGHTMAP_COMBINED
		#pragma multi_compile _ DYNAMICLIGHTMAP_ON
		#pragma multi_compile _ SHADOWS_SHADOWMASK
		// Setup DECALS_OFF so the shader stripper can remove variants
		#pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT

		// Supported shadow modes per light type
		#pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH

		#pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

		#define SHADERPASS SHADERPASS_FORWARD
		// In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
		// Don't do it with debug display mode as it is possible there is no depth prepass in this case
		#if !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(DEBUG_DISPLAY)
			#define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
		#endif
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

	#ifdef DEBUG_DISPLAY
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
	#endif

		// The light loop (or lighting architecture) is in charge to:
		// - Define light list
		// - Define the light loop
		// - Setup the constant/data
		// - Do the reflection hierarchy
		// - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

		#define HAS_LIGHTLOOP

		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}

	Pass
	{
		Name "TransparentDepthPostpass"
		Tags { "LightMode" = "TransparentDepthPostpass" }

		Cull[_CullMode]
		ZWrite On
		ColorMask 0

		HLSLPROGRAM
		#define SHADERPASS SHADERPASS_DEPTH_ONLY
		#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

		#pragma vertex Vert
		#pragma fragment Frag

		ENDHLSL
	}
}
*/

/*
SubShader
{
							Tags{ "RenderPipeline" = "HDRenderPipeline" }
							Pass
							{
								Name "IndirectDXR"
								Tags{ "LightMode" = "IndirectDXR" }

								HLSLPROGRAM

								#pragma raytracing test

								#pragma multi_compile _ DEBUG_DISPLAY
								#pragma multi_compile _ LIGHTMAP_ON
								#pragma multi_compile _ DIRLIGHTMAP_COMBINED
								#pragma multi_compile _ DYNAMICLIGHTMAP_ON

								#define SHADERPASS SHADERPASS_RAYTRACING_INDIRECT

							// multi compile that allows us to
							#pragma multi_compile _ DIFFUSE_LIGHTING_ONLY
							#pragma multi_compile _ MULTI_BOUNCE_INDIRECT

							// We use the low shadow maps for raytracing
							#define SHADOW_LOW

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
							#define HAS_LIGHTLOOP
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracingData.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl"

							ENDHLSL
						}

						Pass
						{
							Name "ForwardDXR"
							Tags{ "LightMode" = "ForwardDXR" }

							HLSLPROGRAM

							#pragma raytracing test

							#pragma multi_compile _ DEBUG_DISPLAY
							#pragma multi_compile _ LIGHTMAP_ON
							#pragma multi_compile _ DIRLIGHTMAP_COMBINED
							#pragma multi_compile _ DYNAMICLIGHTMAP_ON

							#define SHADERPASS SHADERPASS_RAYTRACING_FORWARD

							// We use the low shadow maps for raytracing
							#define SHADOW_LOW

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
							#define HAS_LIGHTLOOP
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracingData.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingForward.hlsl"

							ENDHLSL
						}

						Pass
						{
							Name "GBufferDXR"
							Tags{ "LightMode" = "GBufferDXR" }

							HLSLPROGRAM

							#pragma raytracing test

							#pragma multi_compile _ DEBUG_DISPLAY
							#pragma multi_compile _ LIGHTMAP_ON
							#pragma multi_compile _ DIRLIGHTMAP_COMBINED
							#pragma multi_compile _ DYNAMICLIGHTMAP_ON
							#pragma multi_compile _ DIFFUSE_LIGHTING_ONLY

							#define SHADERPASS SHADERPASS_RAYTRACING_GBUFFER

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracingData.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl"

							ENDHLSL
						}

						Pass
						{
							Name "VisibilityDXR"
							Tags{ "LightMode" = "VisibilityDXR" }

							HLSLPROGRAM

							#pragma raytracing test

							#define SHADERPASS SHADERPASS_RAYTRACING_VISIBILITY

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracingData.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingVisibility.hlsl"

							ENDHLSL
						}

						Pass
						{
							Name "PathTracingDXR"
							Tags{ "LightMode" = "PathTracingDXR" }

							HLSLPROGRAM

							#pragma raytracing test

							#pragma multi_compile _ DEBUG_DISPLAY

							#define SHADERPASS SHADERPASS_PATH_TRACING

							// This is just because it need to be defined, shadow maps are not used.
							#define SHADOW_LOW

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
							#define HAS_LIGHTLOOP
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracingData.hlsl"
							#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl"

							ENDHLSL
						}
						}
*/

	//CustomEditor "Rendering.HighDefinition.LitGUI"
}
