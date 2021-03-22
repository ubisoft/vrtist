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

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(varyingsType); // output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex

    // 3) PACK the result for the rest of the HDRP Pipeline.
    return PackVaryingsMeshToPS(varyingsType);
}

#endif // _GRASS_VERTEX_SHADER_INCLUDED_
