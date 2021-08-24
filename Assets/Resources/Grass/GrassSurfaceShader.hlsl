//#ifndef _VRTIST_GRASS_SURFACE_SHADER_INCLUDED_
//#define _VRTIST_GRASS_SURFACE_SHADER_INCLUDED_

const float EPSILON = 1e-10;

float3 HUEtoRGB(in float hue)
{
    // Hue [0..1] to RGB [0..1]
    // See http://www.chilliant.com/rgb2hsv.html
    float3 rgb = abs(hue * 6. - float3(3, 2, 4)) * float3(1, -1, -1) + float3(-1, 2, 2);
    return clamp(rgb, 0., 1.);
}

float3 RGBtoHCV(in float3 rgb)
{
    // RGB [0..1] to Hue-Chroma-Value [0..1]
    // Based on work by Sam Hocevar and Emil Persson
    float4 p = (rgb.g < rgb.b) ? float4(rgb.bg, -1., 2. / 3.) : float4(rgb.gb, 0., -1. / 3.);
    float4 q = (rgb.r < p.x) ? float4(p.xyw, rgb.r) : float4(rgb.r, p.yzx);
    float c = q.x - min(q.w, q.y);
    float h = abs((q.w - q.y) / (6. * c + EPSILON) + q.z);
    return float3(h, c, q.x);
}

float3 HSVtoRGB(in float3 hsv)
{
    // Hue-Saturation-Value [0..1] to RGB [0..1]
    float3 rgb = HUEtoRGB(hsv.x);
    return ((rgb - 1.) * hsv.y + 1.) * hsv.z;
}

float3 HSLtoRGB(in float3 hsl)
{
    // Hue-Saturation-Lightness [0..1] to RGB [0..1]
    float3 rgb = HUEtoRGB(hsl.x);
    float c = (1. - abs(2. * hsl.z - 1.)) * hsl.y;
    return (rgb - 0.5) * c + hsl.z;
}

float3 RGBtoHSV(in float3 rgb)
{
    // RGB [0..1] to Hue-Saturation-Value [0..1]
    float3 hcv = RGBtoHCV(rgb);
    float s = hcv.y / (hcv.z + EPSILON);
    return float3(hcv.x, s, hcv.z);
}

float3 RGBtoHSL(in float3 rgb)
{
    // RGB [0..1] to Hue-Saturation-Lightness [0..1]
    float3 hcv = RGBtoHCV(rgb);
    float z = hcv.z - hcv.y * 0.5;
    float s = hcv.y / (1. - abs(z * 2. - 1.) + EPSILON);
    return float3(hcv.x, s, z);
}

SurfaceDescription GrassSurface(SurfaceDescriptionInputs IN)
{
    SurfaceDescription surface = (SurfaceDescription)0;

    // BASE = LERP from Bottom to Top color
    float3 lerpColor = lerp(_BottomColor, _TopColor, IN.uv0.y).rgb;

    // Convert to HSV to be able to modify the Hue, Saturation or Value
    float3 hsvLerpColor = RGBtoHSV(lerpColor);

    // Unpack the HSV modifier values from the packed VertexColor input.
    // H: rotate by a given percentage.
    // SV: [0....decrease....0.5....increase....1]
    float hueOverrideInput = IN.VertexColor.x;
    float saturationOverrideInput = IN.VertexColor.y;
    float valueOverrideInput = IN.VertexColor.z;
    float hueModifier = (hueOverrideInput >= 0.5) ? hueOverrideInput - 0.5 : hueOverrideInput + 0.5;
    float valueModifier = (valueOverrideInput >= 0.5) ? 1.0 + 2.0 * (valueOverrideInput - 0.5) :  (2.0 * valueOverrideInput);
    float saturationModifier = (saturationOverrideInput >= 0.5) ? 1.0 + 2.0 * (saturationOverrideInput - 0.5) : (2.0 * saturationOverrideInput);

    // Apply HSV modification on the base lerped color, and convert back to RGB.
    hsvLerpColor.x = fmod(hsvLerpColor.x + hueModifier, 1.0);
    hsvLerpColor.y = saturate(saturationModifier * hsvLerpColor.y); 
    hsvLerpColor.z = saturate(valueModifier * hsvLerpColor.z);
    float3 finalColor = HSVtoRGB(hsvLerpColor);

    surface.BaseColor = float4(finalColor, 1);
    surface.Emission = float3(0, 0, 0);
    surface.Alpha = 1;
    surface.BentNormal = IN.TangentSpaceNormal;
    surface.Smoothness = 0.8;
    surface.Occlusion = 1;
    surface.NormalTS = IN.TangentSpaceNormal;
    surface.Metallic = 0;
    return surface;
}

//#endif // _VRTIST_GRASS_SURFACE_SHADER_INCLUDED_
