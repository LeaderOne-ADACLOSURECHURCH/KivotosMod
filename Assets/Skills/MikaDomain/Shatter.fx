float4x4 uProjection;
float uCaptureOpacity;
texture uScreenshot;
sampler Capture : register(s0) = sampler_state
{
    Texture = <uScreenshot>; MinFilter = Linear; MagFilter = Linear; MipFilter = None;
    AddressU = Clamp; AddressV = Clamp;
};
struct Varyings { float4 Position : POSITION0; float4 Color : COLOR0; float2 UV : TEXCOORD0; };
Varyings Vertex(float4 pos : POSITION0, float4 color : COLOR0, float2 uv : TEXCOORD0)
{
    Varyings result;
    result.Position=mul(pos,uProjection);
    result.Color=color;
    result.UV=uv;
    return result;
}
float4 Pixel(Varyings input) : COLOR0
{
    float3 photo=tex2D(Capture,input.UV).rgb;
    float3 glass=saturate(input.Color.rgb+photo*uCaptureOpacity);
    return float4(glass*input.Color.a,input.Color.a);
}
technique Shatter
{
    pass P0 { VertexShader=compile vs_3_0 Vertex(); PixelShader=compile ps_3_0 Pixel(); }
}
