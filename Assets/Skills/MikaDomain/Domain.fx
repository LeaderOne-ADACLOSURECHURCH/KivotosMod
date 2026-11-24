float4x4 uMatrix;
float2 uSize;
float2 uAnchor;
float2 uPosition;
float2 uScale;
float2 uRotation; // cosine, sine, evaluated from the AE motion table
float2 uCanvas;
float2 uOffset;
float uOpacity;
float uMaskOpacity;
float2 uMaskScale;
float uTint;
float3 uBlack;
float3 uWhite;
float uMatteType; // 0 none, 1 alpha, 2 luma
float uAdditive;
float4 uAdjust; // hue in radians, saturation delta, brightness, contrast
float3 uGlow; // threshold, radius in source pixels, intensity
texture uSource;
texture uMask;
sampler SourceSampler : register(s0) = sampler_state { Texture=<uSource>; MinFilter=Linear; MagFilter=Linear; MipFilter=None; AddressU=Clamp; AddressV=Clamp; };
sampler MaskSampler : register(s1) = sampler_state { Texture=<uMask>; MinFilter=Linear; MagFilter=Linear; MipFilter=None; AddressU=Clamp; AddressV=Clamp; };
struct VOut { float4 Position:POSITION0; float2 UV:TEXCOORD0; float2 MaskUV:TEXCOORD1; };
VOut Vertex(float4 position:POSITION0, float2 uv:TEXCOORD0)
{
    VOut o;
    float2 p=(uv*uSize-uAnchor)*uScale;
    p=float2(p.x*uRotation.x-p.y*uRotation.y,p.x*uRotation.y+p.y*uRotation.x)+uPosition;
    o.Position=mul(float4(p+uOffset,0,1),uMatrix);
    o.UV=uv;
    o.MaskUV=p/uCanvas;
    return o;
}
float4 Pixel(VOut input):COLOR0
{
    float4 source=tex2D(SourceSampler,input.UV);
    float matte=1;
    if(uMatteType>0.5)
    {
        float2 maskUV=(input.MaskUV-.5)/uMaskScale+.5;
        float4 mask=tex2D(MaskSampler,maskUV);
        matte=uMatteType>1.5 ? dot(mask.rgb,float3(0.2126,0.7152,0.0722)) : mask.a;
        matte*=uMaskOpacity*step(0,maskUV.x)*step(maskUV.x,1)*step(0,maskUV.y)*step(maskUV.y,1);
    }
    float3 straight=source.rgb/max(source.a,0.00001);
    float gray=dot(straight,float3(0.2126,0.7152,0.0722));
    float3 rgb=lerp(straight,lerp(uBlack,uWhite,gray),uTint);
    // Brightness keeps black at black; contrast is centered on mid-grey.
    rgb=pow(max(rgb,0),exp2(-uAdjust.z));
    float contrast=(1+uAdjust.w)/max(.01,1-uAdjust.w);
    rgb=saturate((rgb-.5)*contrast+.5);
    float maximum=max(rgb.r,max(rgb.g,rgb.b));
    float minimum=min(rgb.r,min(rgb.g,rgb.b));
    float delta=maximum-minimum;
    float light=(maximum+minimum)*.5;
    float sat=delta/max(.00001,1-abs(2*light-1));
    float hue=0;
    if(delta>.00001) {
        if(maximum==rgb.r)hue=(rgb.g-rgb.b)/delta;
        else if(maximum==rgb.g)hue=2+(rgb.b-rgb.r)/delta;
        else hue=4+(rgb.r-rgb.g)/delta;
        hue=frac(hue/6);
    }
    hue=frac(hue+uAdjust.x/6.283185307);
    sat=uAdjust.y>=0 ? lerp(sat,1,saturate(uAdjust.y)) : sat*(1+saturate(-uAdjust.y)*-1);
    float chroma=(1-abs(2*light-1))*sat;
    float3 hueShape=saturate(abs(frac(hue+float3(0,2.0/3.0,1.0/3.0))*6-3)-1);
    rgb=(hueShape-.5)*chroma+light+uGlow;
    float alpha=source.a*uOpacity*saturate(matte);
    // PMA normal and linear-dodge layers share AlphaBlend; additive keeps RGB
    // but sets source alpha to zero, leaving destination color unattenuated.
    return float4(rgb*alpha,alpha*(1-uAdditive));
}
technique MikaEx { pass Compose { VertexShader=compile vs_3_0 Vertex(); PixelShader=compile ps_3_0 Pixel(); } }
