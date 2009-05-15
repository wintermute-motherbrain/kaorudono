#define NUM_SAMPLES 120

uniform float2		ScreenLightPos;
uniform half		Density;
uniform half		Weight;
uniform half		Decay;
uniform half		Exposure;

float2   ViewportSize;
float2   TextureSize     : register(c1);
float4x4 MatrixTransform : register(c2);

sampler				frameSampler : register(s0);

void LightScatterVS(inout float4 position : POSITION0,
		  				inout float4 color    : COLOR0,
						inout float2 texCoord : TEXCOORD0)
{
    // Apply the matrix transform.
    position = mul(position, transpose(MatrixTransform));
    
	// Half pixel offset for correct texel centering.
	position.xy -= 0.5;

	// Viewport adjustment.
	position.xy /= ViewportSize;
	position.xy *= float2(2, -2);
	position.xy -= float2(1, -1);

	// Compute the texture coordinate.
	texCoord /= TextureSize;
}



float4 LightScatterPS(float2 texCoord : TEXCOORD0) : COLOR0  
{
	// Calculate vector from pixel to light source in screen space.
	half2 deltaTexCoord = (texCoord - ScreenLightPos.xy);
	// Divide by number of samples and scale by control factor.
	deltaTexCoord *= 1.0f / NUM_SAMPLES * Density;
	// Store initial sample.
	half3 color = tex2D(frameSampler, texCoord);
	// Set up illumination decay factor.
	half illuminationDecay = 1.0f;
	// Evaluate summation from Equation 3 NUM_SAMPLES iterations.
	for (int i = 0; i < NUM_SAMPLES; i++)
	{
		// Step sample location along ray.
		texCoord -= deltaTexCoord;
		// Retrieve sample at new location.
		half3 sample = tex2D(frameSampler, texCoord);
		// Apply sample attenuation scale/decay factors.
		sample *= illuminationDecay * Weight;
		// Accumulate combined color.
		color += sample;
		// Update exponential decay factor.
		illuminationDecay *= Decay;
	}
	// Output final color with a further scale control factor.
	return float4( color * Exposure, 1);
}

technique LightScatter
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 LightScatterVS();
        PixelShader = compile ps_3_0 LightScatterPS();
    }
}
