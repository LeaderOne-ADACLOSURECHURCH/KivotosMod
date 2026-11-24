float4x4 uProjection;
float4x4 uPose;
float4x4 uMattePose;
float4x4 uInverseWarp;
float2 uSize;
float2 uCanvas;
float2 uWarpSize;
float4 uAdjust;
float uGlowStrength;
float2 uMatteFeather;
float4 uLayer; // opacity, matte mode, matte opacity, clear alpha for open additive
float2 uWarp; // rectangular-to-polar fraction and adjustment opacity
texture uSource;
texture uMatte;
texture uGlowTexture;

sampler Source : register(s0) = sampler_state
{
    Texture = <uSource>; MinFilter = Linear; MagFilter = Linear; MipFilter = None;
    AddressU = Clamp; AddressV = Clamp;
};
sampler Matte : register(s1) = sampler_state
{
    Texture = <uMatte>; MinFilter = Linear; MagFilter = Linear; MipFilter = None;
    AddressU = Clamp; AddressV = Clamp;
};
sampler GlowMap : register(s2) = sampler_state
{
    Texture = <uGlowTexture>; MinFilter = Linear; MagFilter = Linear; MipFilter = None;
    AddressU = Clamp; AddressV = Clamp;
};

struct Varyings
{
    float4 Position : POSITION0;
    float2 UV : TEXCOORD0;
    float2 Point : TEXCOORD1;
    float2 MaskUV : TEXCOORD2;
};

Varyings Vertex(float4 position : POSITION0, float2 uv : TEXCOORD0)
{
    Varyings output;
    float4 location = mul(float4(uv * uSize, 0, 1), uPose);
    output.Position = mul(location, uProjection);
    output.UV = uv;
    output.Point = location.xy;
    output.MaskUV = mul(location, uMattePose).xy;
    return output;
}

float InBounds(float2 uv)
{
    return step(0, uv.x) * step(0, uv.y) * step(uv.x, 1) * step(uv.y, 1);
}

float3 Adjust(float3 rgb)
{
    rgb = pow(max(rgb, 0), exp2(-uAdjust.z));
    rgb = (rgb - .5) * (1 + uAdjust.w) / max(.01, 1 - uAdjust.w) + .5;
    rgb = saturate(rgb);
    float hi = max(rgb.r, max(rgb.g, rgb.b));
    float lo = min(rgb.r, min(rgb.g, rgb.b));
    float delta = hi - lo;
    float light = (hi + lo) * .5;
    float sat = delta / max(.00001, 1 - abs(2 * light - 1));
    float hue = 0;
    if (delta > .00001)
    {
        if (hi == rgb.r) hue = (rgb.g - rgb.b) / delta;
        else if (hi == rgb.g) hue = 2 + (rgb.b - rgb.r) / delta;
        else hue = 4 + (rgb.r - rgb.g) / delta;
    }
    hue = frac(hue / 6 + uAdjust.x / 6.283185307);
    sat = uAdjust.y >= 0 ? lerp(sat, 1, saturate(uAdjust.y)) : sat * (1 + max(-1, uAdjust.y));
    float3 shape = saturate(abs(frac(hue + float3(0, 2.0/3.0, 1.0/3.0)) * 6 - 3) - 1);
    return (shape - .5) * (1 - abs(2 * light - 1)) * sat + light;
}

float4 Grade(float4 color)
{
    if (dot(abs(uAdjust), 1) < .00001) return color;
    float alpha = max(color.a, max(color.r, max(color.g, color.b)));
    return float4(Adjust(color.rgb / max(alpha, .00001)) * alpha, color.a);
}

float4 Layer(Varyings input) : COLOR0
{
    float4 color = Grade(tex2D(Source, input.UV));
    if (uGlowStrength > 0)
        color.rgb += Grade(tex2D(GlowMap, input.UV)).rgb * uGlowStrength;
    float coverage = 1;
    if (uLayer.y > .5)
    {
        float4 mask = tex2D(Matte, input.MaskUV);
        if (dot(uMatteFeather,uMatteFeather) > 0)
        {
            float2 t = uMatteFeather * .38;
            mask = mask * .28 +
                (tex2D(Matte,input.MaskUV+float2(t.x,t.y))+
                 tex2D(Matte,input.MaskUV+float2(-t.x,t.y))+
                 tex2D(Matte,input.MaskUV+float2(t.x,-t.y))+
                 tex2D(Matte,input.MaskUV-float2(t.x,t.y))) * .18;
        }
        coverage = uLayer.y > 2.5 ? dot(mask.rgb, float3(.2126, .7152, .0722)) : mask.a;
        coverage *= InBounds(input.MaskUV) * uLayer.z;
        if (uLayer.y == 2 || uLayer.y == 4) coverage = 1 - coverage;
    }
    color *= saturate(coverage) * uLayer.x;
    color.a *= 1 - uLayer.w;
    return color;
}

float4 Adjustment(Varyings input) : COLOR0
{
    // Inverse mapping samples a rectangular strip for each point of the disc.
    float2 xy = mul(float4(input.Point, 0, 1), uInverseWarp).xy;
    float2 uv = xy / uWarpSize;
    float2 delta = xy - uWarpSize * .5;
    float2 strip = float2(frac(atan2(delta.x, -delta.y) / 6.283185307),
                         length(delta) / (uWarpSize.y * .5));
    float2 samplePoint = lerp(uv, strip, uWarp.x) * uWarpSize;
    float2 sourceUV = mul(float4(samplePoint, 0, 1), uMattePose).xy / uCanvas;
    float4 base = tex2D(Source, input.UV);
    float4 changed = Grade(tex2D(Source, sourceUV)) * InBounds(sourceUV);
    changed *= lerp(1, InBounds(strip), step(.00001, uWarp.x));
    return lerp(base, changed, uWarp.y);
}

technique DrawLayer
{
    pass P0 { VertexShader = compile vs_3_0 Vertex(); PixelShader = compile ps_3_0 Layer(); }
}

technique ApplyAdjustment
{
    pass P0 { VertexShader = compile vs_3_0 Vertex(); PixelShader = compile ps_3_0 Adjustment(); }
}
