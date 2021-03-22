#ifndef _INJECT_IN_SHADERGRAPH_INCLUDED_
#define _INJECT_IN_SHADERGRAPH_INCLUDED_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

struct DrawVertex
{
    float3 positionWS;
    float2 uv;
};

struct DrawTriangle
{
    float3 normalOS;
    DrawVertex vertices[3];
};

StructuredBuffer<DrawTriangle> _DrawTriangles;

struct VertexShaderInput
{
    uint vertexID : SV_VertexID;
    //UNITY_VERTEX_INPUT_INSTANCE_ID
    uint instanceID : INSTANCEID_SEMANTIC;
};

// A replacement Vertex Shader used to extract data from the input ComputeBuffer
// and pass it to the original HDRP Vertex Shader.
PackedVaryingsMeshToPS GrassVert(VertexShaderInput input)
{
    // 1) EXTRACT info from the ComputeBuffer.
    DrawTriangle tri = _DrawTriangles[input.vertexID / 3];
    DrawVertex vertex = tri.vertices[input.vertexID % 3];

    float3 positionRWS = GetCameraRelativePositionWS(vertex.positionWS);
    float3 lightPosition = float3(1, 1, 1); // TMP
    float3 perpendicularAngle = float3(0, 0, 1);
    float3 faceNormal = cross(perpendicularAngle, tri.normalOS) * lightPosition;

    // 2) CALL the HDRP vertex shader with the extracted info.
    AttributesMesh inputMesh = (AttributesMesh)0;

    inputMesh.positionOS = TransformWorldToObject(positionRWS);

#if defined(ATTRIBUTES_NEED_NORMAL)
    inputMesh.normalOS = faceNormal;
#endif

#if defined(ATTRIBUTES_NEED_TANGENT)
    inputMesh.tangentOS = float4(0,0,1,1);
#endif

#if defined(ATTRIBUTES_NEED_TEXCOORD0)
    inputMesh.uv0 = float4(vertex.uv, 0, 0);
#endif

    UNITY_TRANSFER_INSTANCE_ID(input, inputMesh); // copy .instanceID from input to output

    VaryingsMeshToPS varyingsType = VertMesh(inputMesh);
    // VertMesh does:
    //UNITY_SETUP_INSTANCE_ID(input); // setup "unity_InstanceID" and "unity_StereoIndex" from input.instanceID
    //UNITY_TRANSFER_INSTANCE_ID(input, output); // copy .instanceID from input(AttributeMesh) to output(VaryingsMeshToPS)

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyingsType); // output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex

    // 3) PACK the result for the rest of the HDRP Pipeline.

    PackedVaryingsMeshToPS packed = PackVaryingsMeshToPS(varyingsType);

    //UNITY_TRANSFER_INSTANCE_ID(inputMesh, packed);
    //UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyingsType, packed);

    // Fragment shader does (1st line):
    // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput); // unity_StereoEyeIndex = input.stereoTargetEyeIndexAsRTArrayIdx;

    return packed;
}

//UNITY_SETUP_INSTANCE_ID(input); // setup "unity_InstanceID" and "unity_StereoIndex" from input.instanceID
//UNITY_TRANSFER_INSTANCE_ID(input, output); // copy .instanceID from input(AttributeMesh) to output(VaryingsMeshToPS)

//UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyingsType, packed);
//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyingsType); // output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex
//UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput); // unity_StereoEyeIndex = input.stereoTargetEyeIndexAsRTArrayIdx;


//#if UNITY_ANY_INSTANCING_ENABLED
//#if defined(UNITY_INSTANCING_ENABLED)
//#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
//#if defined(UNITY_STEREO_INSTANCING_ENABLED)


//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

/*
FORWARD / DEPTH_ONLY / FULLSCREEN_DEBUG
---------------------------------------

Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl
Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl
Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl
Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassFullScreenDebug.hlsl

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

META(light transport)
---------------------

--> has a much more complicated Vertex Shader.

MOTION_VECTORS
--------------

PackedVaryingsType Vert(AttributesMesh inputMesh,
                        AttributesPass inputPass)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);

    return MotionVectorVS(varyingsType, inputMesh, inputPass);
}

*/


//struct AttributesMesh
//{
//    float3 positionOS   : POSITION;
//#ifdef ATTRIBUTES_NEED_NORMAL
//    float3 normalOS     : NORMAL;
//#endif
//#ifdef ATTRIBUTES_NEED_TANGENT
//    float4 tangentOS    : TANGENT; // Store sign in w
//#endif
//#ifdef ATTRIBUTES_NEED_TEXCOORD0
//    float2 uv0          : TEXCOORD0;
//#endif
//#ifdef ATTRIBUTES_NEED_TEXCOORD1
//    float2 uv1          : TEXCOORD1;
//#endif
//#ifdef ATTRIBUTES_NEED_TEXCOORD2
//    float2 uv2          : TEXCOORD2;
//#endif
//#ifdef ATTRIBUTES_NEED_TEXCOORD3
//    float2 uv3          : TEXCOORD3;
//#endif
//#ifdef ATTRIBUTES_NEED_COLOR
//    float4 color        : COLOR;
//#endif
//
//    UNITY_VERTEX_INPUT_INSTANCE_ID
//};






















//--------------------------------------------------------
// DUMMY function to enable custom code injection
//--------------------------------------------------------

//void Dummy_float(out float3 PositionOS, out float3 NormalOS, out float3 TangentOS)
//{
//    float3 positionRWS = GetCameraRelativePositionWS(_DrawTriangles[0].vertices[0].positionWS);
//    PositionOS = TransformWorldToObject(positionRWS);
//    NormalOS = float3(0,1,0);
//    TangentOS = float3(0,0,1);
//}

#endif // _INJECT_IN_SHADERGRAPH_INCLUDED_
