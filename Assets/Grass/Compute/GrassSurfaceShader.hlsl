//#ifndef _VRTIST_GRASS_SURFACE_SHADER_INCLUDED_
//#define _VRTIST_GRASS_SURFACE_SHADER_INCLUDED_

SurfaceDescription GrassSurface(SurfaceDescriptionInputs IN)
{
    SurfaceDescription surface = (SurfaceDescription)0;
    surface.BaseColor = IN.VertexColor * lerp(_BottomColor, _TopColor, IN.uv0.y);
    surface.Emission = float3(0, 0, 0);
    surface.Alpha = 1;
    surface.BentNormal = IN.TangentSpaceNormal;
    surface.Smoothness = 0.5;
    surface.Occlusion = 1;
    surface.NormalTS = IN.TangentSpaceNormal;
    surface.Metallic = 0;
    return surface;
}

//#endif // _VRTIST_GRASS_SURFACE_SHADER_INCLUDED_
