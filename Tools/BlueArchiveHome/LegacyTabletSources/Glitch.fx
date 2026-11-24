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

float random(float seed)
{
	return 543.2543 * sin( dot( float2( seed, seed ), float2( 3525.46, -54.3415 ) ) );
}

float trunc(float f)
{
	if (f > 0.0)
	{
		return floor(f);
	}
	else
	{
		return ceil(f);
	}
}

float mix(float min, float max, float rate)
{
	return min + (max - min) * rate;
}

float4 Glitch(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float shake_speed = uShaderSpecificData.r;
	float shake_rate = uShaderSpecificData.g;
	float shake_power = uShaderSpecificData.b;
	float shake_block_size = uShaderSpecificData.a;
	float shake_color_rate = uSaturation;
	
	float r = random(trunc(uTime * shake_speed));
	float enable_shift = float((r - floor(r)) < shake_rate);
     
	float2 fixed_coords = coords;
	fixed_coords.x += (random((trunc(coords.y * shake_block_size) / shake_block_size)
		+ uTime) - 0.5) * shake_power * enable_shift;
	
	float4 pixel_color = tex2D(uImage0, fixed_coords);
	
	pixel_color.r = mix(pixel_color.r, tex2D(uImage0, fixed_coords + float2(shake_color_rate, 0.0)).r, enable_shift);
	pixel_color.b = mix(pixel_color.b, tex2D(uImage0, fixed_coords + float2(shake_color_rate, 0.0)).b, enable_shift);
	
	return pixel_color * uOpacity;
}

technique Technique1
{
    pass Glitch
    {
        PixelShader = compile ps_2_0 Glitch();
    }
}