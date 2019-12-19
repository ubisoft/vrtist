#ifndef CUSTOM_OUTLINE_PASS_INCLUDED
#define CUSTOM_OUTLINE_PASS_INCLUDED

float _Outline;
float4 _OutlineColor;

struct Attributes 
{
	float4 positionOS : POSITION;
	float3 normalOS   : NORMAL;
};

struct Varyings 
{
	float4 color : COLOR;
	float4 positionCS : SV_POSITION;
};

Varyings UnlitContourVertex(Attributes v)
{
	Varyings output;

	output.positionCS = TransformObjectToHClip(v.positionOS);

	float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
	float2 offset = TransformWorldToHClipDir(normalWS).xy; // normal direction in screen space

	output.positionCS.xy += offset * output.positionCS.z * _Outline; // grow outline as the object goes farther away from camera (doesnt seem to work)
	output.color = _OutlineColor;

	return output;
}

float4 UnlitContourFragment(Varyings input) : SV_TARGET
{
	return input.color;
}

#endif // CUSTOM_OUTLINE_PASS_INCLUDED
