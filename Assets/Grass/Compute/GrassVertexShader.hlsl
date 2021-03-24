#ifndef _GRASS_VERTEX_SHADER_INCLUDED_
#define _GRASS_VERTEX_SHADER_INCLUDED_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

struct GrassVertex
{
    float3 positionWS;
    float2 uv;
};

struct GrassTriangle
{
    float3 normalOS;
    float3 color;
    GrassVertex vertices[3];
};

StructuredBuffer<GrassTriangle> _GrassTriangles;

struct VertexShaderInput
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID // uint instanceID : INSTANCEID_SEMANTIC
};

// A replacement Vertex Shader used to extract data from the input ComputeBuffer
// and pass it to the original HDRP Vertex Shader.
PackedVaryingsMeshToPS GrassVert(VertexShaderInput input)
{
    // 1) EXTRACT info from the ComputeBuffer.
    GrassTriangle tri = _GrassTriangles[input.vertexID / 3];
    GrassVertex vertex = tri.vertices[input.vertexID % 3];

    // 2) CALL the HDRP vertex shader with the extracted info.
    AttributesMesh attribs = (AttributesMesh)0;

    //float3 positionRWS = GetCameraRelativePositionWS(vertex.positionWS); // IF compute outputs WorldSpace positions
    //attribs.positionOS = TransformWorldToObject(positionRWS); // IF compute outputs WorldSpace positions
    attribs.positionOS = vertex.positionWS;

#if defined(ATTRIBUTES_NEED_NORMAL)
    attribs.normalOS = tri.normalOS;
#endif

#if defined(ATTRIBUTES_NEED_TANGENT)
    attribs.tangentOS = float4(0,0,1,1);
#endif

#if defined(ATTRIBUTES_NEED_TEXCOORD0)
    attribs.uv0 = float4(vertex.uv, 0, 0);
#endif

#if defined(ATTRIBUTES_NEED_COLOR)
    attribs.color = float4(tri.color, 1);
#endif

    UNITY_TRANSFER_INSTANCE_ID(input, attribs); // copy .instanceID from input to output

    VaryingsMeshToPS v2f = VertMesh(attribs);

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v2f); // output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex

    // 3) PACK the result for the rest of the HDRP Pipeline.
    return PackVaryingsMeshToPS(v2f);
}

#endif // _GRASS_VERTEX_SHADER_INCLUDED_
