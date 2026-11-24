float4x4 uProjection;
float2 uSize;
float2 uBodySize;
float2 uStep;
float uThreshold;
float uRadius;
texture uSource;

sampler Body : register(s0) = sampler_state
{
    Texture = <uSource>; MinFilter = Linear; MagFilter = Linear; MipFilter = None;
    AddressU = Clamp; AddressV = Clamp;
};

struct Varyings
{
    float4 Position : POSITION0;
    float2 UV : TEXCOORD0;
};

Varyings Vertex(float4 position : POSITION0, float2 uv : TEXCOORD0)
{
    Varyings output;
    output.Position = mul(float4(uv * uSize, 0, 1), uProjection);
    output.UV = uv;
    return output;
}

float3 Extract(float2 uv)
{
    float2 bodyUV = (uv - .5) * uSize / uBodySize + .5;
    float4 source = tex2D(Body, bodyUV);
    float inside = step(0, bodyUV.x) * step(0, bodyUV.y) * step(bodyUV.x, 1) * step(bodyUV.y, 1);
    return max(source.rgb - source.a * uThreshold, 0) * inside * 4;
}

float Weight(float offset)
{
    float ratio = offset / max(uRadius, .001);
    return exp(-4.5 * ratio * ratio);
}

float4 BrightPass(Varyings input) : COLOR0
{
    float3 glow = Extract(input.UV);
    return float4(glow, max(glow.r, max(glow.g, glow.b)));
}

float4 Blur(Varyings input) : COLOR0
{
    float3 glow = tex2D(Body, input.UV).rgb;
    float total = 1;
    // Use adjacent texels on the reduced texture, rather than disjoint samples
    // separated by the radius of the glow.
    [loop] for (int i = 1; i <= 25; i++)
    {
        if (i > ceil(uRadius)) break;
        float weight = Weight(i);
        float2 offset = uStep * i;
        glow += (tex2Dlod(Body, float4(input.UV + offset, 0, 0)).rgb +
                 tex2Dlod(Body, float4(input.UV - offset, 0, 0)).rgb) * weight;
        total += 2 * weight;
    }
    glow /= total;
    return float4(glow, max(glow.r, max(glow.g, glow.b)));
}

technique ExtractAndBlur
{
    pass ExtractPass { VertexShader = compile vs_3_0 Vertex(); PixelShader = compile ps_3_0 BrightPass(); }
    pass BlurPass { VertexShader = compile vs_3_0 Vertex(); PixelShader = compile ps_3_0 Blur(); }
}
