sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uShaderSpecificData;

float3 fract3(float3 f)
{
	return f - floor(f);
}

float3 multiplyMatrix3(float3 f, float3x3 m)
{
	float3 o;

	o.x = dot(f, m[0]);
	o.y = dot(f, m[1]);
	o.z = dot(f, m[2]);
	
	return o;
}

float4 WaterCaustic(float4 color : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Declare new color variable
	float4 k;
	float a = 1.0;
	float3x3 m = float3x3(-2.0,-1.0,2.0, 3.0,-2.0,1.0, 1.0,2.0,2.0);
	
	// Generate a clamped coords for pixelation effect
	float2 uv = round(coords * (uImageSize0 / 2.0)) / (uImageSize0 / 2.0); 
	
	// Apply time
	k.a = float(uTime * uShaderSpecificData.y);
	k.rg = uv * uShaderSpecificData.x; // + sin(uTime * 0.1);
		
	// Apply passes
	// 1
	k.rga = multiplyMatrix3(k.rga, m) * 0.3;
	a = min(a, length(0.5 - fract3(k.rga)));
	
	// 2
	k.rga = multiplyMatrix3(k.rga, m) * 0.3;
	a = min(a, length(0.5 - fract3(k.rga)));
	
	// 3
	k.rga = multiplyMatrix3(k.rga, m) * 0.3;
	a = min(a, length(0.5 - fract3(k.rga)));

	// Get the color of the texture based on the uv
	float4 texColor = tex2D(uImage0, uv * uImageSize0 / uImageSize0);
	
	// Overlay the water caustic over the texture color.
	color = texColor + float4(uColor.rgb, 1.0) * pow(a, uShaderSpecificData.z) * uShaderSpecificData.w * texColor.a;
	
	// Return a color with limited gradiants.
	color = round(color * 20.0) / 20.0;
	
	// Apply opacity
	color.rgba *= uOpacity;
	
	return color;
}

technique Technique1
{
    pass WaterCaustic
    {
        PixelShader = compile ps_2_0 WaterCaustic();
    }
}